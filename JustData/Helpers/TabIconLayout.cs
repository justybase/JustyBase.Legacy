using System;
using System.Drawing;
using System.Windows.Forms;
using AppBase.Common;

namespace JustyBaseLegacy.UI.Helpers;

internal static class TabIconLayout
{
    public static int IconSize(int dpi) => DpiScale.Scale(16, dpi);

    public static int HitInflate(int dpi) => DpiScale.Scale(2, dpi);

    public static Rectangle CloseIconRect(Rectangle tabRect, int dpi)
    {
        int size = IconSize(dpi);
        int margin = DpiScale.Scale(5, dpi);
        return new Rectangle(
            tabRect.Right - margin - size,
            tabRect.Y + Math.Max(0, (tabRect.Height - size) / 2),
            size,
            size);
    }

    public static Rectangle PinIconRect(Rectangle tabRect, int dpi)
    {
        int size = IconSize(dpi);
        Rectangle closeRect = CloseIconRect(tabRect, dpi);
        int gap = DpiScale.Scale(3, dpi);
        return new Rectangle(
            closeRect.X - gap - size,
            closeRect.Y,
            size,
            size);
    }

    public static Rectangle HitRect(Rectangle iconRect, int dpi)
    {
        Rectangle hit = iconRect;
        hit.Inflate(HitInflate(dpi), HitInflate(dpi));
        return hit;
    }

    private static int FontHeight(Font font, int dpi) =>
        (int)Math.Ceiling(font.GetHeight(dpi));

    public static int TabHeight(Font font, int dpi) =>
        Math.Max(
            FontHeight(font, dpi) + DpiScale.Scale(6, dpi),
            IconSize(dpi) + DpiScale.Scale(6, dpi));

    public static int ResultsTabHeight(Font font, int dpi) =>
        Math.Max(
            FontHeight(font, dpi) + DpiScale.Scale(6, dpi),
            IconSize(dpi) + DpiScale.Scale(8, dpi));

    public static Point TabPadding(int dpi) =>
        new(DpiScale.Scale(18, dpi), DpiScale.Scale(2, dpi));

    /// <summary>
    /// Extra horizontal padding so auto-sized tab width covers pin/close icon chrome.
    /// </summary>
    public static Point ResultsTabPadding(int dpi) =>
        new(DpiScale.Scale(26, dpi), DpiScale.Scale(3, dpi));

    /// <summary>
    /// For TabAlignment.Left/Right, ItemSize.Width is the extent of each tab along the bar
    /// and ItemSize.Height is the thickness of the tab strip.
    /// </summary>
    public static Size LeftTabItemSize(TabControl leftTabs, Font font, int dpi)
    {
        int fontHeight = FontHeight(font, dpi);
        int stripWidth = fontHeight + DpiScale.Scale(8, dpi);
        int maxChars = 4;
        if (leftTabs != null)
        {
            foreach (TabPage tab in leftTabs.TabPages)
            {
                maxChars = Math.Max(maxChars, tab.Text?.Length ?? 0);
            }
        }

        int charStep = (int)Math.Ceiling(fontHeight * 0.58f);
        int tabSegmentHeight = DpiScale.Scale(2, dpi) + maxChars * charStep;
        tabSegmentHeight = Math.Max(tabSegmentHeight, DpiScale.Scale(28, dpi));
        return new Size(tabSegmentHeight, stripWidth);
    }
}
