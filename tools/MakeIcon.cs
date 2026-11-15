// Renders the Bytecoin coin logo (design canvas vector) with the wallet role badge (a lock) into a multi-size .ico.
// Usage: MakeIcon.exe out.ico
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

static class MakeIcon
{
    const string B = "F0 M34 25H59C69 25 74 31 74 38C74 43 71.5 46.5 67.5 48.5C73.5 50.5 77 55.5 77 62C77 71 70.5 76 60.5 76H34ZM44.5 34V44H57C60.5 44 62.5 42 62.5 39C62.5 36 60.5 34 57 34ZM44.5 54.5V67H59C63 67 65.5 64.5 65.5 60.75C65.5 57 63 54.5 59 54.5Z";

    static byte[] Render(int size)
    {
        var coral = new SolidColorBrush(Color.FromRgb(0xFF, 0x6A, 0x55));
        var navy = new SolidColorBrush(Color.FromRgb(0x10, 0x1B, 0x28));
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            double k = size / 100.0;
            dc.PushTransform(new ScaleTransform(k, k));
            dc.DrawEllipse(coral, null, new Point(50, 50), 48, 48);
            if (size >= 32) dc.DrawEllipse(null, new Pen(navy, 3.5), new Point(50, 50), 40.5, 40.5);
            dc.DrawGeometry(navy, null, Geometry.Parse(B));
            dc.DrawRectangle(navy, null, new Rect(20, 46.5, 20, 7.5));
            if (size >= 24)
            {
                // role badge from the style guide: navy disc, coral ring, light lock
                var light = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF4));
                dc.DrawEllipse(navy, new Pen(coral, 4), new Point(77, 77), 21, 21);
                dc.DrawRoundedRectangle(light, null, new Rect(67, 75, 20, 14), 2.5, 2.5);
                dc.DrawGeometry(null, new Pen(light, 3.6), Geometry.Parse("M71 75.5V71.5a6 6 0 0 1 12 0V75.5"));
            }            dc.Pop();
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using (var ms = new MemoryStream()) { enc.Save(ms); return ms.ToArray(); }
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 1) { Console.Error.WriteLine("usage: MakeIcon out.ico [preview.png]"); return 2; }
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
        var frames = new List<byte[]>();
        foreach (var s in sizes) frames.Add(Render(s));
        using (var f = new BinaryWriter(File.Create(args[0])))
        {
            f.Write((short)0); f.Write((short)1); f.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                f.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                f.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                f.Write((byte)0); f.Write((byte)0);
                f.Write((short)1); f.Write((short)32);
                f.Write(frames[i].Length); f.Write(offset);
                offset += frames[i].Length;
            }
            foreach (var fr in frames) f.Write(fr);
        }
        if (args.Length > 1) File.WriteAllBytes(args[1], frames[sizes.Length - 1]);
        return 0;
    }
}
