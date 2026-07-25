using AppBase.Data.Completion;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

/// <summary>
/// Creates the small, DPI-aware icon set used by the SQL completion popup.
/// The glyphs are drawn as vectors into 32-bit bitmaps, so no blurry legacy
/// bitmap scaling or runtime SVG dependency is needed.
/// </summary>
internal static class SqlCompletionIconFactory
{
    private static readonly Color TableColor = Color.FromArgb(78, 170, 220);
    private static readonly Color ViewColor = Color.FromArgb(76, 190, 128);
    private static readonly Color ColumnColor = Color.FromArgb(172, 118, 220);
    private static readonly Color DatabaseColor = Color.FromArgb(230, 180, 72);
    private static readonly Color SchemaColor = Color.FromArgb(216, 155, 74);
    private static readonly Color FunctionColor = Color.FromArgb(205, 112, 190);
    private static readonly Color CteColor = Color.FromArgb(69, 183, 178);
    private static readonly Color AliasColor = Color.FromArgb(145, 157, 174);
    private static readonly Color KeywordColor = Color.FromArgb(220, 108, 130);
    private static readonly Color SnippetColor = Color.FromArgb(93, 180, 117);
    private static readonly Color DataTypeColor = Color.FromArgb(224, 143, 82);
    private static readonly Color VariableColor = Color.FromArgb(215, 125, 90);
    private static readonly Color ReferenceColor = Color.FromArgb(126, 151, 190);

    public static ImageList Create(int dpi)
    {
        int size = Math.Max(12, (int)Math.Round(16 * dpi / 96f));
        var imageList = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(size, size),
            TransparentColor = Color.Transparent
        };

        for (int i = 0; i <= (int)CompletionIconKind.Reference; i++)
            imageList.Images.Add(CreateIcon((CompletionIconKind)i, size));

        return imageList;
    }

    private static Bitmap CreateIcon(CompletionIconKind kind, int size)
    {
        return Create(size, graphics =>
        {
            switch (kind)
            {
                case CompletionIconKind.Table:
                    DrawTable(graphics, size, TableColor);
                    break;
                case CompletionIconKind.View:
                    DrawView(graphics, size, ViewColor);
                    break;
                case CompletionIconKind.Column:
                    DrawColumns(graphics, size, ColumnColor);
                    break;
                case CompletionIconKind.Database:
                    DrawDatabase(graphics, size, DatabaseColor);
                    break;
                case CompletionIconKind.Schema:
                    DrawFolder(graphics, size, SchemaColor);
                    break;
                case CompletionIconKind.Function:
                    DrawTextGlyph(graphics, size, "ƒ", FunctionColor);
                    break;
                case CompletionIconKind.Cte:
                    DrawCards(graphics, size, CteColor);
                    break;
                case CompletionIconKind.Alias:
                    DrawLink(graphics, size, AliasColor);
                    break;
                case CompletionIconKind.Keyword:
                    DrawTextGlyph(graphics, size, "<>", KeywordColor);
                    break;
                case CompletionIconKind.Snippet:
                    DrawTextGlyph(graphics, size, "{}", SnippetColor);
                    break;
                case CompletionIconKind.DataType:
                    DrawTextGlyph(graphics, size, "123", DataTypeColor);
                    break;
                case CompletionIconKind.Variable:
                    DrawTextGlyph(graphics, size, "$", VariableColor);
                    break;
                case CompletionIconKind.Reference:
                    DrawReference(graphics, size, ReferenceColor);
                    break;
            }
        });
    }

    private static void DrawTable(Graphics graphics, int size, Color color)
    {
        float left = size * .18f;
        float top = size * .20f;
        float width = size * .64f;
        float height = size * .60f;
        using var pen = CreatePen(color, size * .09f);
        graphics.DrawRectangle(pen, left, top, width, height);
        graphics.DrawLine(pen, left, top + height * .34f, left + width, top + height * .34f);
        graphics.DrawLine(pen, left + width * .34f, top, left + width * .34f, top + height);
        graphics.DrawLine(pen, left + width * .68f, top, left + width * .68f, top + height);
    }

    private static void DrawView(Graphics graphics, int size, Color color)
    {
        using var pen = CreatePen(color, size * .09f);
        float left = size * .12f;
        float top = size * .28f;
        float width = size * .76f;
        float height = size * .44f;
        graphics.DrawArc(pen, left, top, width * .50f, height, 180, 180);
        graphics.DrawArc(pen, left + width * .50f, top, width * .50f, height, 180, 180);
        graphics.DrawArc(pen, left, top, width * .50f, height, 0, 180);
        graphics.DrawArc(pen, left + width * .50f, top, width * .50f, height, 0, 180);
        using var brush = new SolidBrush(color);
        float radius = size * .13f;
        graphics.FillEllipse(brush, size * .50f - radius, size * .50f - radius, radius * 2, radius * 2);
    }

    private static void DrawColumns(Graphics graphics, int size, Color color)
    {
        using var brush = new SolidBrush(color);
        float width = size * .14f;
        float gap = size * .09f;
        float left = size * .24f;
        graphics.FillRectangle(brush, left, size * .18f, width, size * .64f);
        graphics.FillRectangle(brush, left + width + gap, size * .30f, width, size * .52f);
        graphics.FillRectangle(brush, left + (width + gap) * 2, size * .40f, width, size * .42f);
    }

    private static void DrawDatabase(Graphics graphics, int size, Color color)
    {
        using var pen = CreatePen(color, size * .09f);
        float left = size * .22f;
        float top = size * .22f;
        float width = size * .56f;
        float height = size * .56f;
        float ellipseHeight = size * .20f;
        graphics.DrawEllipse(pen, left, top, width, ellipseHeight);
        graphics.DrawLine(pen, left, top + ellipseHeight / 2, left, top + height);
        graphics.DrawLine(pen, left + width, top + ellipseHeight / 2, left + width, top + height);
        graphics.DrawArc(pen, left, top + height - ellipseHeight, width, ellipseHeight, 0, 180);
        graphics.DrawArc(pen, left, top + height - ellipseHeight, width, ellipseHeight, 180, 180);
    }

    private static void DrawFolder(Graphics graphics, int size, Color color)
    {
        using var path = new GraphicsPath();
        path.AddPolygon(new[]
        {
            new PointF(size * .16f, size * .30f),
            new PointF(size * .43f, size * .30f),
            new PointF(size * .51f, size * .42f),
            new PointF(size * .84f, size * .42f),
            new PointF(size * .78f, size * .78f),
            new PointF(size * .16f, size * .78f)
        });
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    private static void DrawCards(Graphics graphics, int size, Color color)
    {
        using var pen = CreatePen(color, size * .08f);
        graphics.DrawRectangle(pen, size * .18f, size * .30f, size * .56f, size * .48f);
        graphics.DrawRectangle(pen, size * .30f, size * .20f, size * .56f, size * .48f);
    }

    private static void DrawLink(Graphics graphics, int size, Color color)
    {
        using var pen = CreatePen(color, size * .10f);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        graphics.DrawArc(pen, size * .10f, size * .28f, size * .48f, size * .36f, 135, 180);
        graphics.DrawArc(pen, size * .42f, size * .36f, size * .48f, size * .36f, -45, 180);
        graphics.DrawLine(pen, size * .38f, size * .50f, size * .62f, size * .50f);
    }

    private static void DrawReference(Graphics graphics, int size, Color color)
    {
        using var pen = CreatePen(color, size * .09f);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        graphics.DrawLine(pen, size * .20f, size * .70f, size * .76f, size * .28f);
        graphics.DrawLine(pen, size * .58f, size * .28f, size * .76f, size * .28f);
        graphics.DrawLine(pen, size * .76f, size * .28f, size * .76f, size * .46f);
    }

    private static void DrawTextGlyph(Graphics graphics, int size, string text, Color color)
    {
        using var brush = new SolidBrush(color);
        using var font = new Font("Segoe UI Symbol", Math.Max(6f, size * (text.Length > 2 ? .34f : .68f)), FontStyle.Bold, GraphicsUnit.Pixel);
        var bounds = new RectangleF(0, size * .13f, size, size * .72f);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoClip
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static Pen CreatePen(Color color, float width)
    {
        return new Pen(color, Math.Max(1.1f, width))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }

    private static Bitmap Create(int size, Action<Graphics> draw)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        draw(graphics);
        return bitmap;
    }
}
