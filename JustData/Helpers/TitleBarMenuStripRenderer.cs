using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

/// <summary>
/// Flat title-bar menu with explicit colors — no system-color bleed-through.
/// </summary>
internal sealed class TitleBarMenuStripRenderer : ToolStripProfessionalRenderer
{
    private readonly Color _back;
    private readonly Color _fore;
    private readonly Color _hoverBack;
    private readonly Color _hoverFore;

    public TitleBarMenuStripRenderer(Color back, Color fore, Color hoverBack, Color hoverFore)
        : base(new TitleBarColorTable(back, hoverBack))
    {
        _back = back;
        _fore = fore;
        _hoverBack = hoverBack;
        _hoverFore = hoverFore;
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip)
        {
            return;
        }

        base.OnRenderToolStripBackground(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip && !e.Item.IsOnDropDown)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                using var brush = new SolidBrush(_hoverBack);
                e.Graphics.FillRectangle(brush, bounds);
            }

            return;
        }

        base.OnRenderMenuItemBackground(e);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip && !e.Item.IsOnDropDown)
        {
            Color textColor = e.Item.Selected || e.Item.Pressed ? _hoverFore : _fore;
            using var brush = new SolidBrush(textColor);
            var format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip,
                HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None,
            };
            e.Graphics.DrawString(e.Text, e.TextFont, brush, e.TextRectangle, format);
            return;
        }

        base.OnRenderItemText(e);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip && !e.Item.IsOnDropDown)
        {
            return;
        }

        base.OnRenderButtonBackground(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip)
        {
            return;
        }

        base.OnRenderSeparator(e);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }

    private sealed class TitleBarColorTable : ProfessionalColorTable
    {
        private readonly Color _back;
        private readonly Color _hover;

        public TitleBarColorTable(Color back, Color hover)
        {
            _back = back;
            _hover = hover;
            UseSystemColors = false;
        }

        public override Color ToolStripGradientBegin => _back;
        public override Color ToolStripGradientMiddle => _back;
        public override Color ToolStripGradientEnd => _back;
        public override Color MenuStripGradientBegin => _back;
        public override Color MenuStripGradientEnd => _back;
        public override Color MenuItemSelected => _hover;
        public override Color MenuItemSelectedGradientBegin => _hover;
        public override Color MenuItemSelectedGradientEnd => _hover;
        public override Color MenuItemBorder => _hover;
        public override Color MenuItemPressedGradientBegin => _hover;
        public override Color MenuItemPressedGradientEnd => _hover;
        public override Color CheckBackground => _hover;
        public override Color CheckSelectedBackground => _hover;
        public override Color CheckPressedBackground => _hover;
        public override Color ImageMarginGradientBegin => _back;
        public override Color ImageMarginGradientMiddle => _back;
        public override Color ImageMarginGradientEnd => _back;
        public override Color SeparatorDark => _back;
        public override Color SeparatorLight => _back;
    }
}
