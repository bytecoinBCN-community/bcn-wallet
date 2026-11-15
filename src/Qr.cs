using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace BcnWallet
{
    // Small QR Code encoder (ISO/IEC 18004): byte mode, error correction level M, versions 1..15.
    // Enough for a Bytecoin address with a "bytecoin:" URI. Follows the structure of Project Nayuki's reference code.
    public static class Qr
    {
        // ECC codewords per block and number of blocks for level M, index = version
        static readonly int[] EccPerBlock = { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24 };
        static readonly int[] Blocks = { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10 };

        public static bool[,] Encode(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            int ver = 1;
            for (; ver <= 15; ver++)
            {
                int cap = DataCodewords(ver) * 8;
                int need = 4 + (ver < 10 ? 8 : 16) + data.Length * 8;
                if (need <= cap) break;
            }
            if (ver > 15) throw new ArgumentException("text too long for QR");
            // bit stream
            var bits = new List<bool>();
            Append(bits, 4, 4);
            Append(bits, data.Length, ver < 10 ? 8 : 16);
            foreach (var b in data) Append(bits, b, 8);
            int capBits = DataCodewords(ver) * 8;
            Append(bits, 0, Math.Min(4, capBits - bits.Count));
            Append(bits, 0, (8 - bits.Count % 8) % 8);
            for (int pad = 0xEC; bits.Count < capBits; pad ^= 0xEC ^ 0x11) Append(bits, pad, 8);
            var cw = new byte[bits.Count / 8];
            for (int i = 0; i < bits.Count; i++) if (bits[i]) cw[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            var all = AddEcc(cw, ver);

            int size = ver * 4 + 17;
            var m = new bool[size, size];
            var fn = new bool[size, size];
            DrawFunction(m, fn, ver);
            DrawCodewords(m, fn, all);
            int best = 0; long bestPen = long.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                ApplyMask(m, fn, mask);
                DrawFormat(m, fn, mask);
                long p = Penalty(m);
                if (p < bestPen) { bestPen = p; best = mask; }
                ApplyMask(m, fn, mask);
            }
            ApplyMask(m, fn, best);
            DrawFormat(m, fn, best);
            return m;
        }

        static void Append(List<bool> bits, int val, int len) { for (int i = len - 1; i >= 0; i--) bits.Add(((val >> i) & 1) != 0); }

        static int RawModules(int ver)
        {
            int r = (16 * ver + 128) * ver + 64;
            if (ver >= 2)
            {
                int na = ver / 7 + 2;
                r -= (25 * na - 10) * na - 55;
                if (ver >= 7) r -= 36;
            }
            return r;
        }

        static int DataCodewords(int ver) { return RawModules(ver) / 8 - EccPerBlock[ver] * Blocks[ver]; }

        static byte[] AddEcc(byte[] data, int ver)
        {
            int nb = Blocks[ver], ecl = EccPerBlock[ver];
            int raw = RawModules(ver) / 8;
            int nShort = nb - raw % nb, shortLen = raw / nb;
            var gen = RsDivisor(ecl);
            var blocks = new List<byte[]>();
            for (int i = 0, k = 0; i < nb; i++)
            {
                int dlen = shortLen - ecl + (i < nShort ? 0 : 1);
                var dat = new byte[dlen];
                Array.Copy(data, k, dat, 0, dlen);
                k += dlen;
                var ecc = RsRemainder(dat, gen);
                var blk = new byte[shortLen + 1];
                Array.Copy(dat, blk, dlen);
                if (i < nShort) { Array.Copy(ecc, 0, blk, dlen + 1, ecl); } else Array.Copy(ecc, 0, blk, dlen, ecl);
                blocks.Add(blk);
            }
            var res = new List<byte>();
            for (int i = 0; i < shortLen + 1; i++)
                for (int j = 0; j < nb; j++)
                    if (i != shortLen - ecl || j >= nShort) res.Add(blocks[j][i]);
            return res.ToArray();
        }

        static byte[] RsDivisor(int degree)
        {
            var r = new byte[degree];
            r[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < r.Length; j++)
                {
                    r[j] = Mul(r[j], root);
                    if (j + 1 < r.Length) r[j] ^= r[j + 1];
                }
                root = Mul(root, 0x02);
            }
            return r;
        }

        static byte[] RsRemainder(byte[] data, byte[] div)
        {
            var r = new byte[div.Length];
            foreach (var b in data)
            {
                int f = b ^ r[0];
                Array.Copy(r, 1, r, 0, r.Length - 1);
                r[r.Length - 1] = 0;
                for (int i = 0; i < r.Length; i++) r[i] ^= Mul(div[i], f);
            }
            return r;
        }

        static byte Mul(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return (byte)z;
        }

        static void Set(bool[,] m, bool[,] fn, int x, int y, bool v) { m[y, x] = v; fn[y, x] = true; }

        static void DrawFunction(bool[,] m, bool[,] fn, int ver)
        {
            int size = m.GetLength(0);
            for (int i = 0; i < size; i++) { Set(m, fn, 6, i, i % 2 == 0); Set(m, fn, i, 6, i % 2 == 0); }
            Finder(m, fn, 3, 3); Finder(m, fn, size - 4, 3); Finder(m, fn, 3, size - 4);
            var pos = AlignPos(ver);
            int n = pos.Length;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (!(i == 0 && j == 0 || i == 0 && j == n - 1 || i == n - 1 && j == 0)) Align(m, fn, pos[i], pos[j]);
            DrawFormat(m, fn, 0);
            if (ver >= 7)
            {
                int rem = ver;
                for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
                long bits = ((long)ver << 12) | (uint)rem;
                for (int i = 0; i < 18; i++)
                {
                    bool bit = ((bits >> i) & 1) != 0;
                    int a = size - 11 + i % 3, b = i / 3;
                    Set(m, fn, a, b, bit); Set(m, fn, b, a, bit);
                }
            }
        }

        static int[] AlignPos(int ver)
        {
            if (ver == 1) return new int[0];
            int n = ver / 7 + 2;
            int step = (ver * 8 + n * 3 + 5) / (n * 4 - 4) * 2;
            var r = new int[n];
            r[0] = 6;
            for (int i = n - 1, pos = ver * 4 + 10; i >= 1; i--, pos -= step) r[i] = pos;
            return r;
        }

        static void Finder(bool[,] m, bool[,] fn, int cx, int cy)
        {
            int size = m.GetLength(0);
            for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int d = Math.Max(Math.Abs(dx), Math.Abs(dy)), x = cx + dx, y = cy + dy;
                    if (x >= 0 && x < size && y >= 0 && y < size) Set(m, fn, x, y, d != 2 && d != 4);
                }
        }

        static void Align(bool[,] m, bool[,] fn, int cx, int cy)
        {
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++) Set(m, fn, cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
        }

        static void DrawFormat(bool[,] m, bool[,] fn, int mask)
        {
            int size = m.GetLength(0);
            int data = 0 << 3 | mask; // level M = 0b00
            int rem = data;
            for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            int bits = (data << 10 | rem) ^ 0x5412;
            for (int i = 0; i <= 5; i++) Set(m, fn, 8, i, ((bits >> i) & 1) != 0);
            Set(m, fn, 8, 7, ((bits >> 6) & 1) != 0);
            Set(m, fn, 8, 8, ((bits >> 7) & 1) != 0);
            Set(m, fn, 7, 8, ((bits >> 8) & 1) != 0);
            for (int i = 9; i < 15; i++) Set(m, fn, 14 - i, 8, ((bits >> i) & 1) != 0);
            for (int i = 0; i < 8; i++) Set(m, fn, size - 1 - i, 8, ((bits >> i) & 1) != 0);
            for (int i = 8; i < 15; i++) Set(m, fn, 8, size - 15 + i, ((bits >> i) & 1) != 0);
            Set(m, fn, 8, size - 8, true);
        }

        static void DrawCodewords(bool[,] m, bool[,] fn, byte[] data)
        {
            int size = m.GetLength(0), i = 0;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < size; vert++)
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool up = ((right + 1) & 2) == 0;
                        int y = up ? size - 1 - vert : vert;
                        if (!fn[y, x] && i < data.Length * 8)
                        {
                            m[y, x] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                            i++;
                        }
                    }
            }
        }

        static void ApplyMask(bool[,] m, bool[,] fn, int mask)
        {
            int size = m.GetLength(0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (fn[y, x]) continue;
                    bool inv;
                    switch (mask)
                    {
                        case 0: inv = (x + y) % 2 == 0; break;
                        case 1: inv = y % 2 == 0; break;
                        case 2: inv = x % 3 == 0; break;
                        case 3: inv = (x + y) % 3 == 0; break;
                        case 4: inv = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: inv = x * y % 2 + x * y % 3 == 0; break;
                        case 6: inv = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        default: inv = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    if (inv) m[y, x] = !m[y, x];
                }
        }

        // Simplified penalty (runs + 2x2 blocks + balance); any mask yields a valid symbol.
        static long Penalty(bool[,] m)
        {
            int size = m.GetLength(0);
            long p = 0; int dark = 0;
            for (int y = 0; y < size; y++)
            {
                int runX = 1, runY = 1;
                for (int x = 0; x < size; x++)
                {
                    if (m[y, x]) dark++;
                    if (x > 0)
                    {
                        if (m[y, x] == m[y, x - 1]) { runX++; if (runX == 5) p += 3; else if (runX > 5) p++; } else runX = 1;
                        if (m[x, y] == m[x - 1, y]) { runY++; if (runY == 5) p += 3; else if (runY > 5) p++; } else runY = 1;
                    }
                    if (x > 0 && y > 0 && m[y, x] == m[y, x - 1] && m[y, x] == m[y - 1, x] && m[y, x] == m[y - 1, x - 1]) p += 3;
                }
            }
            int total = size * size;
            int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            return p + Math.Max(0, k) * 10;
        }

        // Dark modules on a white quiet zone, as a frozen DrawingImage.
        public static ImageSource Image(string text)
        {
            var m = Encode(text);
            int size = m.GetLength(0);
            var g = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (m[y, x]) g.Children.Add(new RectangleGeometry(new Rect(x + 4, y + 4, 1.02, 1.02)));
            var dg = new DrawingGroup();
            dg.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, size + 8, size + 8))));
            dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0x10, 0x1B, 0x28)), null, g));
            var img = new DrawingImage(dg);
            img.Freeze();
            return img;
        }
    }
}
