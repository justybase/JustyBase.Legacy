using DatabaseDataGridView.WinForms.Interfaces;


namespace DatabaseDataGridView.WinForms.Coloring;

public sealed class MyRendererToolStrinRenderer : ToolStripProfessionalRenderer
{
    public static Brush SelectedMenuBrush { get; set; } =  Brushes.DarkGray;

    private readonly IColorConfig _config;
    private readonly IColorTheme _colorTheme;

    public MyRendererToolStrinRenderer(IColorConfig config, IColorTheme colorTheme) : base(new MyColors(config))
    {
        _config = config;
        _colorTheme = colorTheme;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip.IsDropDown)
            base.OnRenderToolStripBackground(e);
        else if (_config.UseSpecialColoring)
            e.Graphics.Clear(_colorTheme.MainBack);
        else
            e.Graphics.Clear(MyColors.Color1);
    }


    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            base.OnRenderButtonBackground(e);
        }
        else
        {
            Rectangle rectangle = new Rectangle(0, 0, e.Item.Size.Width - 1, e.Item.Size.Height - 1);
            e.Graphics.FillRectangle(SelectedMenuBrush, rectangle);
            e.Graphics.DrawRectangle(Pens.Black, rectangle);
        }
    }

    //Pen blackPen = new Pen(Color.Black, 1);
    protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            Rectangle rectangle = new Rectangle(0, 0, e.Item.Size.Width - 11, e.Item.Size.Height - 1);
            e.Graphics.FillRectangle(SelectedMenuBrush, rectangle);
            e.Graphics.DrawRectangle(Pens.Black, rectangle);
            Rectangle rectangle2 = new Rectangle(e.Item.Size.Width - 11, 0, 10, e.Item.Size.Height - 1);
            e.Graphics.FillRectangle(SelectedMenuBrush, rectangle2);
            e.Graphics.DrawRectangle(Pens.Black, rectangle2);

            // Create points that define polygon.
            PointF point1 = new PointF(rectangle2.X + 3, rectangle2.Y + 10);
            PointF point2 = new PointF(rectangle2.X + 7, rectangle2.Y + 10);
            PointF point3 = new PointF(rectangle2.X + 5, rectangle2.Y + 12);
            PointF[] curvePoints =
                {
             point1,
             point2,
             point3
         };

            // Draw polygon curve to screen.
            e.Graphics.FillPolygon(SelectedMenuBrush, curvePoints);
            e.Graphics.DrawPolygon(Pens.Black, curvePoints);
        }
        else
        {
            base.OnRenderSplitButtonBackground(e);
        }
    }

    //https://stackoverflow.com/questions/1918247/how-to-disable-the-line-under-tool-strip-in-winform-c
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        //base.OnRenderToolStripBorder(e);
    }

}
