using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>
/// TabControl whose selected page fills the client area with no content-frame border.
/// Used when tab headers are hidden (FlatButtons + tiny ItemSize).
/// </summary>
internal sealed class BorderlessTabControl : TabControl
{
    private const int TcmAdjustRect = 0x1328;

    protected override void WndProc(ref Message m)
    {
        // Ignore TCM_ADJUSTRECT so Windows does not inset the page for the default frame.
        if (m.Msg == TcmAdjustRect && !DesignMode)
        {
            return;
        }

        base.WndProc(ref m);
    }
}
