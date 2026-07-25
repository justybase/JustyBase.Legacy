using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace JustyBaseLegacy.UI.Helpers;

internal static class SqlToolbarIconFactory
{
    private static readonly Color RunColor = Color.FromArgb(0, 122, 204);
    private static readonly Color StopColor = Color.FromArgb(241, 76, 76);

    public static Bitmap CreatePlay(Size size)
    {
        return Create(size, graphics =>
        {
            using var path = new GraphicsPath();
            path.AddPolygon(new[]
            {
                new PointF(size.Width * 0.30f, size.Height * 0.18f),
                new PointF(size.Width * 0.30f, size.Height * 0.82f),
                new PointF(size.Width * 0.78f, size.Height * 0.50f)
            });

            using var brush = new SolidBrush(RunColor);
            graphics.FillPath(brush, path);
        });
    }

    public static Bitmap CreateStop(Size size)
    {
        return Create(size, graphics =>
        {
            float inset = size.Width * 0.22f;
            using var brush = new SolidBrush(StopColor);
            graphics.FillRectangle(brush, inset, inset, size.Width - (inset * 2), size.Height - (inset * 2));
        });
    }

    public static Bitmap CreateImport(Size size)
    {
        return Create(size, graphics =>
        {
            using var arrowPen = new Pen(Color.FromArgb(0, 122, 204), Math.Max(1.5f, size.Width * 0.11f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            float centerX = size.Width * 0.50f;
            float arrowTop = size.Height * 0.16f;
            float arrowBottom = size.Height * 0.66f;
            float arrowHead = size.Width * 0.20f;
            graphics.DrawLine(arrowPen, centerX, arrowTop, centerX, arrowBottom);
            graphics.DrawLine(arrowPen, centerX - arrowHead, arrowBottom - arrowHead, centerX, arrowBottom);
            graphics.DrawLine(arrowPen, centerX, arrowBottom, centerX + arrowHead, arrowBottom - arrowHead);

            using var trayPen = new Pen(Color.FromArgb(83, 95, 107), Math.Max(1.5f, size.Width * 0.09f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            graphics.DrawLine(trayPen, size.Width * 0.20f, size.Height * 0.72f, size.Width * 0.28f, size.Height * 0.86f);
            graphics.DrawLine(trayPen, size.Width * 0.28f, size.Height * 0.86f, size.Width * 0.72f, size.Height * 0.86f);
            graphics.DrawLine(trayPen, size.Width * 0.72f, size.Height * 0.86f, size.Width * 0.80f, size.Height * 0.72f);
        });
    }

    public static Bitmap CreateKeepConnection(Size size)
    {
        return Create(size, graphics =>
        {
            using var linkPen = new Pen(Color.FromArgb(83, 95, 107), Math.Max(1.5f, size.Width * 0.10f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            using var activePen = new Pen(Color.FromArgb(40, 167, 69), Math.Max(1.5f, size.Width * 0.10f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            graphics.DrawArc(linkPen,
                new RectangleF(size.Width * 0.10f, size.Height * 0.28f, size.Width * 0.52f, size.Height * 0.44f),
                135,
                210);
            graphics.DrawArc(activePen,
                new RectangleF(size.Width * 0.38f, size.Height * 0.28f, size.Width * 0.52f, size.Height * 0.44f),
                -45,
                210);
        });
    }

    public static Bitmap CreateComment(Size size, bool uncomment)
    {
        return Create(size, graphics =>
        {
            float left = size.Width * 0.18f;
            float top = size.Height * 0.18f;
            float width = size.Width * 0.64f;
            float height = size.Height * 0.54f;
            float radius = Math.Max(1f, size.Width * 0.10f);

            using var bubble = new GraphicsPath();
            bubble.AddArc(left, top, radius, radius, 180, 90);
            bubble.AddArc(left + width - radius, top, radius, radius, 270, 90);
            bubble.AddArc(left + width - radius, top + height - radius, radius, radius, 0, 90);
            bubble.AddLine(left + width - radius, top + height, left + width * 0.54f, top + height);
            bubble.AddLine(left + width * 0.54f, top + height, left + width * 0.40f, top + height + size.Height * 0.16f);
            bubble.AddLine(left + width * 0.40f, top + height + size.Height * 0.16f, left + width * 0.38f, top + height);
            bubble.AddLine(left + width * 0.38f, top + height, left + width * 0.10f, top + height);
            bubble.AddArc(left, top + height - radius, radius, radius, 90, 90);
            bubble.CloseFigure();

            using var fill = new SolidBrush(Color.FromArgb(83, 95, 107));
            using var outline = new Pen(Color.FromArgb(62, 72, 82), Math.Max(1f, size.Width * 0.06f));
            graphics.FillPath(fill, bubble);
            graphics.DrawPath(outline, bubble);

            using var textPen = new Pen(Color.White, Math.Max(1f, size.Width * 0.07f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            float lineLeft = left + size.Width * 0.18f;
            float lineRight = left + width - size.Width * 0.18f;
            graphics.DrawLine(textPen, lineLeft, top + height * 0.34f, lineRight, top + height * 0.34f);
            graphics.DrawLine(textPen, lineLeft, top + height * 0.57f, lineRight, top + height * 0.57f);

            if (uncomment)
            {
                using var slashPen = new Pen(Color.FromArgb(220, 53, 69), Math.Max(1.5f, size.Width * 0.10f))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                graphics.DrawLine(slashPen,
                    size.Width * 0.22f,
                    size.Height * 0.78f,
                    size.Width * 0.80f,
                    size.Height * 0.22f);
            }
        });
    }

    public static Bitmap CreateFormat(Size size)
    {
        return Create(size, graphics =>
        {
            float cx = size.Width * 0.50f;
            float cy = size.Height * 0.50f;
            float r = Math.Min(size.Width, size.Height) * 0.38f;

            // Draw a lightning bolt (format/action symbol).
            using var boltPen = new Pen(Color.FromArgb(255, 185, 0), Math.Max(1.5f, size.Width * 0.08f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var boltFill = new SolidBrush(Color.FromArgb(255, 200, 30));

            // Zig-zag lightning shape.
            float s = size.Width * 0.22f;
            float m = size.Width * 0.50f;
            var boltPoints = new[]
            {
                new PointF(cx - s * 0.6f, cy - r * 0.7f),
                new PointF(cx + s * 0.2f, cy - r * 0.1f),
                new PointF(cx - s * 0.1f, cy + r * 0.1f),
                new PointF(cx + s * 0.6f, cy + r * 0.7f),
                new PointF(cx + s * 0.2f, cy + r * 0.1f),
                new PointF(cx - s * 0.4f, cy - r * 0.1f),
            };

            using var boltPath = new GraphicsPath();
            boltPath.AddLines(boltPoints);
            boltPath.CloseFigure();
            graphics.FillPath(boltFill, boltPath);
            graphics.DrawPath(boltPen, boltPath);

            // Small sparkle dots to suggest "format/clean".
            using var dotBrush = new SolidBrush(Color.FromArgb(255, 200, 30));
            float dotR = Math.Max(1.5f, size.Width * 0.035f);
            graphics.FillEllipse(dotBrush,
                cx - r * 0.9f - dotR, cy - r * 0.5f - dotR,
                dotR * 2, dotR * 2);
            graphics.FillEllipse(dotBrush,
                cx + r * 0.7f - dotR, cy + r * 0.5f - dotR,
                dotR * 2, dotR * 2);
        });
    }

    private static Bitmap Create(Size size, Action<Graphics> draw)
    {
        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        draw(graphics);
        return bitmap;
    }
}
