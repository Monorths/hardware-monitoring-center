using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconGenerator
{
    private static int Main()
    {
        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
        List<byte[]> images = new List<byte[]>();
        foreach (int size in sizes)
        {
            using (Bitmap bitmap = Draw(size))
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                images.Add(stream.ToArray());
            }
        }

        string output = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(Environment.CurrentDirectory, "app.ico");
        using (FileStream stream = File.Create(output))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] image in images)
                writer.Write(image);
        }
        return 0;
    }

    private static Bitmap Draw(int size)
    {
        Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            float pad = Math.Max(1f, size * 0.035f);
            RectangleF outer = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);
            float radius = size * 0.22f;

            using (GraphicsPath path = RoundRect(outer, radius))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(10, 17, 24)))
            using (Pen border = new Pen(Color.FromArgb(45, 210, 236), Math.Max(1f, size * 0.045f)))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);
            }

            using (Pen stem = new Pen(Color.FromArgb(235, 245, 250), Math.Max(1.5f, size * 0.095f)))
            using (SolidBrush bulb = new SolidBrush(Color.FromArgb(255, 160, 92)))
            using (SolidBrush shine = new SolidBrush(Color.FromArgb(255, 236, 204)))
            {
                stem.StartCap = LineCap.Round;
                stem.EndCap = LineCap.Round;
                PointF top = new PointF(size * 0.53f, size * 0.24f);
                PointF bottom = new PointF(size * 0.53f, size * 0.67f);
                graphics.DrawLine(stem, top, bottom);
                graphics.FillEllipse(bulb, size * 0.33f, size * 0.60f, size * 0.40f, size * 0.40f);
                graphics.FillEllipse(shine, size * 0.46f, size * 0.70f, size * 0.14f, size * 0.14f);
            }

            using (Pen tick = new Pen(Color.FromArgb(45, 210, 236), Math.Max(1f, size * 0.035f)))
            {
                tick.StartCap = LineCap.Round;
                tick.EndCap = LineCap.Round;
                graphics.DrawLine(tick, size * 0.68f, size * 0.34f, size * 0.78f, size * 0.34f);
                graphics.DrawLine(tick, size * 0.68f, size * 0.48f, size * 0.76f, size * 0.48f);
            }
        }
        return bitmap;
    }

    private static GraphicsPath RoundRect(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        GraphicsPath path = new GraphicsPath();
        RectangleF arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
