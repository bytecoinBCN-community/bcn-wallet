using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace BcnWallet
{
    // .amwbackup files (docs/amwbackup-format.md of Amethyst Web Wallet): the recovery phrase, creation date and
    // address count, plus (version 2) the wallet state at a block 10 below the top, so a phone or the web wallet
    // restores in seconds instead of scanning the chain from the creation date.
    //   "AMWBACKUP1" | salt[16] | nonce[12] | AES-256-GCM(UTF-8 JSON) | tag[16]
    //   key = PBKDF2-HMAC-SHA256(password, salt, 600000), associated data = the first 38 bytes.
    public static class Backup
    {
        public const string Magic = "AMWBACKUP1";
        public const string Format = "amethyst-web-wallet-backup";
        public const string Extension = ".amwbackup";
        public const int Iterations = 600000;
        public const int MinPassword = 10;
        public const int SnapshotDepth = 10;
        public const int MaxPayload = 16 * 1024 * 1024;
        const int Header = 38;

        static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 256 }; }

        public static string ToJson(object o) { return Json().Serialize(o); }

        // ------------------------------------------------------------------ container

        // CNG's native PBKDF2 takes a fraction of a second; the managed Rfc2898DeriveBytes about six.
        static byte[] Key(string password, byte[] salt)
        {
            var pw = Encoding.UTF8.GetBytes(password);
            try { return Gcm.Pbkdf2(pw, salt, Iterations, 32); }
            catch (Exception) { }
            finally { Array.Clear(pw, 0, pw.Length); }
            using (var kdf = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(32);
        }

        public static byte[] Encrypt(string json, string password, byte[] salt = null, byte[] nonce = null)
        {
            if (salt == null || nonce == null)
            {
                salt = new byte[16]; nonce = new byte[12];
                using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); rng.GetBytes(nonce); }
            }
            var head = new byte[Header];
            Encoding.ASCII.GetBytes(Magic).CopyTo(head, 0);
            salt.CopyTo(head, 10);
            nonce.CopyTo(head, 26);
            var key = Key(password, salt);
            var plain = Encoding.UTF8.GetBytes(json);
            try
            {
                var tag = new byte[16];
                var cipher = Gcm.Run(true, key, nonce, head, plain, tag);
                var file = new byte[Header + cipher.Length + 16];
                head.CopyTo(file, 0);
                cipher.CopyTo(file, Header);
                tag.CopyTo(file, Header + cipher.Length);
                return file;
            }
            finally { Array.Clear(key, 0, key.Length); Array.Clear(plain, 0, plain.Length); }
        }

        public static bool LooksLikeBackup(byte[] file)
        {
            return file != null && file.Length >= Header + 16 && Encoding.ASCII.GetString(file, 0, 10) == Magic;
        }

        // Throws with a translated message: not a backup, or wrong password / damaged file (one message for both).
        public static string Decrypt(byte[] file, string password)
        {
            if (!LooksLikeBackup(file)) throw new Exception(L.T("bk.not_backup"));
            var head = new byte[Header];
            Array.Copy(file, head, Header);
            var salt = new byte[16]; Array.Copy(file, 10, salt, 0, 16);
            var nonce = new byte[12]; Array.Copy(file, 26, nonce, 0, 12);
            var cipher = new byte[file.Length - Header - 16]; Array.Copy(file, Header, cipher, 0, cipher.Length);
            var tag = new byte[16]; Array.Copy(file, file.Length - 16, tag, 0, 16);
            var key = Key(password, salt);
            try
            {
                var plain = Gcm.Run(false, key, nonce, head, cipher, tag);
                if (plain == null) throw new Exception(L.T("bk.wrong_password"));
                try { return new UTF8Encoding(false, true).GetString(plain); }
                finally { Array.Clear(plain, 0, plain.Length); }
            }
            finally { Array.Clear(key, 0, key.Length); }
        }

        // ------------------------------------------------------------------ payload

        public class Contents
        {
            public string Mnemonic, MnemonicPassword = "", FirstAddress = "", CreatedBy = "", Reason = "";
            public int AddressCount = 1, Version;
            public long CreationTs, SavedAt, SnapshotHeight, ScanFrom;   // ScanFrom: unix time to start scanning (a day early), 0 = whole chain
        }

        public static Contents Parse(string json)
        {
            Dictionary<string, object> p;
            try { p = Json().DeserializeObject(json) as Dictionary<string, object>; } catch { p = null; }
            if (p == null || J.Str(p, "format") != Format) throw new Exception(L.T("bk.unknown_format"));
            var c = new Contents { Version = (int)J.Long(p, "version"), CreatedBy = J.Str(p, "created_by"), SavedAt = J.Long(p, "saved_at") };
            Dictionary<string, object> w;
            if (c.Version == 1) w = p;
            else if (c.Version == 2) w = J.Obj(p, "wallet");
            else throw new Exception(L.T("bk.newer_format"));
            if (w == null || J.Str(w, "mnemonic").Length == 0) throw new Exception(L.T("bk.unknown_format"));
            var net = J.Str(p, "net");
            if (net.Length > 0 && net != "main") throw new Exception(L.F("bk.other_net", net));
            if (c.Version == 2 && J.Str(w, "type") != "amethyst") throw new Exception(L.T("bk.not_amethyst"));
            c.Mnemonic = J.Str(w, "mnemonic");
            c.MnemonicPassword = J.Str(w, "mnemonic_password");
            c.FirstAddress = J.Str(w, "first_address");
            c.AddressCount = Math.Max(1, (int)J.Long(w, "address_count"));
            c.CreationTs = J.Long(w, "creation_timestamp");
            if (c.SavedAt == 0) c.SavedAt = J.Long(w, "saved_at");
            // Where scanning starts (format, section 3): first payment, else the snapshot block (nothing before it),
            // else the creation date; one day earlier for time zones and clock skew. This wallet does not load the
            // snapshot outputs, so these rows are the ones that apply, and they are always correct.
            long from = c.CreationTs;
            var s = J.Obj(p, "snapshot");
            if (s != null)
            {
                c.SnapshotHeight = J.Long(s, "height");
                if (J.Long(s, "first_activity_timestamp") > 0) from = J.Long(s, "first_activity_timestamp");
                else if (J.Long(s, "block_timestamp") > 0) from = J.Long(s, "block_timestamp");
            }
            c.ScanFrom = from > 86400 ? from - 86400 : 0;
            return c;
        }

        public static string FileName(string firstAddress)
        {
            var id = firstAddress != null && firstAddress.Length >= 11 ? firstAddress.Substring(3, 8) : "wallet";
            return "bcn-" + id + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Extension;
        }

        // ------------------------------------------------------------------ AES-256-GCM through Windows CNG
        // .NET Framework 4.8 has no AesGcm class; bcrypt.dll has had GCM since Windows Vista.
        static class Gcm
        {
            [StructLayout(LayoutKind.Sequential)]
            struct AuthInfo
            {
                public int cbSize, dwInfoVersion;
                public IntPtr pbNonce; public int cbNonce;
                public IntPtr pbAuthData; public int cbAuthData;
                public IntPtr pbTag; public int cbTag;
                public IntPtr pbMacContext; public int cbMacContext;
                public int cbAAD; public long cbData; public int dwFlags;
            }

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] static extern int BCryptOpenAlgorithmProvider(out IntPtr alg, string id, string impl, int flags);
            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] static extern int BCryptSetProperty(IntPtr h, string prop, byte[] input, int size, int flags);
            [DllImport("bcrypt.dll")] static extern int BCryptGenerateSymmetricKey(IntPtr alg, out IntPtr key, IntPtr obj, int objSize, byte[] secret, int secretSize, int flags);
            [DllImport("bcrypt.dll")] static extern int BCryptEncrypt(IntPtr key, byte[] input, int inSize, ref AuthInfo info, byte[] iv, int ivSize, byte[] output, int outSize, out int result, int flags);
            [DllImport("bcrypt.dll")] static extern int BCryptDecrypt(IntPtr key, byte[] input, int inSize, ref AuthInfo info, byte[] iv, int ivSize, byte[] output, int outSize, out int result, int flags);
            [DllImport("bcrypt.dll")] static extern int BCryptDestroyKey(IntPtr key);
            [DllImport("bcrypt.dll")] static extern int BCryptCloseAlgorithmProvider(IntPtr alg, int flags);

            [DllImport("bcrypt.dll")] static extern int BCryptDeriveKeyPBKDF2(IntPtr prf, byte[] pass, int passLen, byte[] salt, int saltLen, ulong iterations, byte[] key, int keyLen, int flags);

            const int AuthTagMismatch = unchecked((int)0xC000A002);
            const int HmacFlag = 8;  // BCRYPT_ALG_HANDLE_HMAC_FLAG

            public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, int length)
            {
                IntPtr prf;
                Check(BCryptOpenAlgorithmProvider(out prf, "SHA256", null, HmacFlag));
                try
                {
                    var key = new byte[length];
                    Check(BCryptDeriveKeyPBKDF2(prf, password, password.Length, salt, salt.Length, (ulong)iterations, key, length, 0));
                    return key;
                }
                finally { BCryptCloseAlgorithmProvider(prf, 0); }
            }

            // Encrypt: fills tag, returns ciphertext. Decrypt: checks tag, returns plaintext or null on mismatch.
            public static byte[] Run(bool encrypt, byte[] key, byte[] nonce, byte[] aad, byte[] input, byte[] tag)
            {
                IntPtr alg, hKey = IntPtr.Zero;
                Check(BCryptOpenAlgorithmProvider(out alg, "AES", null, 0));
                var hn = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                var ha = GCHandle.Alloc(aad, GCHandleType.Pinned);
                var ht = GCHandle.Alloc(tag, GCHandleType.Pinned);
                try
                {
                    var mode = Encoding.Unicode.GetBytes("ChainingModeGCM\0");
                    Check(BCryptSetProperty(alg, "ChainingMode", mode, mode.Length, 0));
                    Check(BCryptGenerateSymmetricKey(alg, out hKey, IntPtr.Zero, 0, key, key.Length, 0));
                    var info = new AuthInfo
                    {
                        cbSize = Marshal.SizeOf(typeof(AuthInfo)), dwInfoVersion = 1,
                        pbNonce = hn.AddrOfPinnedObject(), cbNonce = nonce.Length,
                        pbAuthData = ha.AddrOfPinnedObject(), cbAuthData = aad.Length,
                        pbTag = ht.AddrOfPinnedObject(), cbTag = tag.Length
                    };
                    var output = new byte[input.Length];
                    int n;
                    int st = encrypt
                        ? BCryptEncrypt(hKey, input, input.Length, ref info, null, 0, output, output.Length, out n, 0)
                        : BCryptDecrypt(hKey, input, input.Length, ref info, null, 0, output, output.Length, out n, 0);
                    if (!encrypt && st == AuthTagMismatch) return null;
                    Check(st);
                    return output;
                }
                finally
                {
                    hn.Free(); ha.Free(); ht.Free();
                    if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                    BCryptCloseAlgorithmProvider(alg, 0);
                }
            }

            static void Check(int status)
            {
                if (status != 0) throw new CryptographicException("CNG error 0x" + status.ToString("X8", CultureInfo.InvariantCulture));
            }
        }
    }
}
