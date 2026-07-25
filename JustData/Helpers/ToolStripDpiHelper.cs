using System.Drawing;
using System.Windows.Forms;
using AppBase.Common;

namespace JustyBaseLegacy.UI.Helpers;

internal static class ToolStripDpiHelper
{
    public static Size MenuIconSize(int dpi) => DpiScale.Scale(new Size(16, 16), dpi);

    public static void ApplyMenuStrip(MenuStrip menuStrip, Font menuFont, int dpi)
    {
        Size iconSize = MenuIconSize(dpi);
        menuStrip.Font = menuFont;
        menuStrip.ImageScalingSize = iconSize;
        ApplyDropDownMenus(menuStrip.Items, menuFont, iconSize);
    }

    public static void ApplyContextMenu(ContextMenuStrip menu, Font menuFont, int dpi)
    {
        Size iconSize = MenuIconSize(dpi);
        menu.Font = menuFont;
        menu.ImageScalingSize = iconSize;
        ApplyMenuItems(menu.Items, menuFont, iconSize);
    }

    private static void ApplyDropDownMenus(ToolStripItemCollection items, Font menuFont, Size iconSize)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                menuItem.DropDown.Font = menuFont;
                menuItem.DropDown.ImageScalingSize = iconSize;
                ApplyMenuItems(menuItem.DropDownItems, menuFont, iconSize);
            }
        }
    }

    private static void ApplyMenuItems(ToolStripItemCollection items, Font menuFont, Size iconSize)
    {
        foreach (ToolStripItem item in items)
        {
            if (item.Image != null)
            {
                item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            }

            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                menuItem.DropDown.Font = menuFont;
                menuItem.DropDown.ImageScalingSize = iconSize;
                ApplyMenuItems(menuItem.DropDownItems, menuFont, iconSize);
            }
        }
    }
}
