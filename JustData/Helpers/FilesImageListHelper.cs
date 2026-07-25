using AppBase.Common;
using System.Drawing.Drawing2D;

namespace JustyBaseLegacy.UI.Helpers;

internal static class FilesImageListHelper
{
    public static void EnsurePopulated(ImageList list, int dpi, Color foreground)
    {
        Size target = DpiScale.Scale(new Size(18, 18), dpi);
        list.ColorDepth = ColorDepth.Depth32Bit;
        list.TransparentColor = Color.Transparent;
        list.ImageSize = target;
        list.Images.Clear();
        list.Images.Add(CreateFolder(target, foreground));
        list.Images.Add(CreateFile(target, foreground));
    }

    private static Bitmap CreateFolder(Size size, Color color)
    {
        var bitmap = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(bitmap);
        Configure(graphics);
        using var brush = new SolidBrush(Color.FromArgb(70, color));
        using var pen = new Pen(color, Math.Max(1f, size.Width / 14f));
        graphics.FillPath(brush, FolderPath(size));
        graphics.DrawPath(pen, FolderPath(size));
        return bitmap;
    }

    private static GraphicsPath FolderPath(Size size)
    {
        var path = new GraphicsPath();
        float x = size.Width * .08f;
        float y = size.Height * .22f;
        float w = size.Width * .84f;
        float h = size.Height * .64f;
        path.AddPolygon([
            new PointF(x, y + h * .18f),
            new PointF(x + w * .34f, y + h * .18f),
            new PointF(x + w * .45f, y),
            new PointF(x + w * .68f, y),
            new PointF(x + w * .78f, y + h * .18f),
            new PointF(x + w, y + h * .18f),
            new PointF(x + w * .92f, y + h),
            new PointF(x, y + h)
        ]);
        return path;
    }

    private static Bitmap CreateFile(Size size, Color color)
    {
        var bitmap = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(bitmap);
        Configure(graphics);
        using var brush = new SolidBrush(Color.FromArgb(55, color));
        using var pen = new Pen(color, Math.Max(1f, size.Width / 14f));
        float margin = size.Width * .16f;
        var path = new GraphicsPath();
        path.AddPolygon([
            new PointF(margin, margin),
            new PointF(size.Width * .62f, margin),
            new PointF(size.Width - margin, size.Height * .38f),
            new PointF(size.Width - margin, size.Height - margin),
            new PointF(margin, size.Height - margin)
        ]);
        graphics.FillPath(brush, path);
        graphics.DrawPath(pen, path);
        graphics.DrawLine(pen, size.Width * .62f, margin, size.Width * .62f, size.Height * .38f);
        graphics.DrawLine(pen, size.Width * .62f, size.Height * .38f, size.Width - margin, size.Height * .38f);
        return bitmap;
    }

    private static void Configure(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }
}
