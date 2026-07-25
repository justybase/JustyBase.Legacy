using DatabaseDataGridView.WinForms.Interfaces;

namespace DatabaseDataGridView.WinForms.Coloring;

public sealed class MyColors : ProfessionalColorTable
{
    public static Color LogErrorStdColor { get; set; } = Color.Pink;
    public static Brush Log1Brush { get; set; } = Brushes.LightYellow;
    public static Brush Log2Brush { get; set; } = Brushes.Pink;
    public static Brush Log3Brush { get; set; } = Brushes.LightGreen;
    public static Color Color1 { get; set; } = SystemColors.Control;
    public static Color Color2 { get; set; } = SystemColors.ControlDark;

    private readonly IColorConfig _config;

    public MyColors(IColorConfig config)
    {
        _config = config;
        base.UseSystemColors = false;
    }

    public override Color MenuItemSelectedGradientBegin =>
        _config.UseSpecialColoring ?
        Color.FromArgb(_config.MenuItemSelectedGradientBegin[0], _config.MenuItemSelectedGradientBegin[1], _config.MenuItemSelectedGradientBegin[2])
        : Color1;
    public override Color MenuItemSelectedGradientEnd =>
        _config.UseSpecialColoring ?
        Color.FromArgb(_config.MenuItemSelectedGradientEnd[0], _config.MenuItemSelectedGradientEnd[1], _config.MenuItemSelectedGradientEnd[2])
        : Color2;

    public override Color MenuItemBorder =>
        Color.FromArgb(_config.MenuItemBorder[0], _config.MenuItemBorder[1], _config.MenuItemBorder[2]);
    public override Color MenuItemPressedGradientBegin =>
        Color.FromArgb(_config.MenuItemPressedGradientBegin[0], _config.MenuItemPressedGradientBegin[1], _config.MenuItemPressedGradientBegin[2]);
    public override Color MenuItemPressedGradientMiddle =>
        Color.FromArgb(_config.MenuItemPressedGradientMiddle[0], _config.MenuItemPressedGradientMiddle[1], _config.MenuItemPressedGradientMiddle[2]);
    public override Color MenuItemPressedGradientEnd =>
        Color.FromArgb(_config.MenuItemPressedGradientEnd[0], _config.MenuItemPressedGradientEnd[1], _config.MenuItemPressedGradientEnd[2]);
    public override Color ButtonSelectedHighlightBorder =>
        Color.FromArgb(_config.ButtonSelectedHighlightBorder[0], _config.ButtonSelectedHighlightBorder[1], _config.ButtonSelectedHighlightBorder[2]);
    public override Color MenuItemSelected =>
        Color.FromArgb(_config.MenuItemSelected[0], _config.MenuItemSelected[1], _config.MenuItemSelected[2]);
}
