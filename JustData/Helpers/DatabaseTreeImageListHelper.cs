using System;
using AppBase.Common;
using System.Drawing;
using System.Windows.Forms;
using JustData.Properties;

namespace JustyBaseLegacy.UI.Helpers;

/// <summary>
/// Populates the database explorer TreeView ImageList.
/// ImageList.Images are cleared when ImageSize changes — repopulate after DPI scaling.
/// </summary>
internal static class DatabaseTreeImageListHelper
{
    public static void EnsurePopulated(ImageList list, int dpi)
    {
        ArgumentNullException.ThrowIfNull(list);

        Size targetSize = DpiScale.Scale(new Size(20, 20), dpi);
        if (list.ImageSize != targetSize)
        {
            list.ImageSize = targetSize;
        }

        if (list.Images.Count > 0)
        {
            return;
        }

        list.ColorDepth = ColorDepth.Depth32Bit;
        list.TransparentColor = Color.Transparent;

        list.Images.Add("database.png", Resources.database);
        list.Images.Add("database_table.png", Resources.database_table);
        list.Images.Add("application_view_columns.png", Resources.application_view_columns);
        list.Images.Add("bug.png", Resources.bug);
        list.Images.Add("table_key.png", Resources.table_key);
        list.Images.Add("bullet_white.png", Resources.bullet_white);
        list.Images.Add("weather_sun.png", Resources.weather_sun);
        list.Images.Add("Table.bmp", Resources.table);
        list.Images.Add("application_view_tile.png", Resources.application_view_tile);
        list.Images.Add("table_link.png", Resources.table_link);
        list.Images.Add("text_columns.png", Resources.text_columns);
        list.Images.Add("bullet_blue.png", Resources.bullet_blue);
        list.Images.Add("arrow_switch.png", Resources.arrow_switch);
        list.Images.Add("car.png", Resources.car);
        list.Images.Add("application_lightning.png", Resources.application_lightning);
        list.Images.Add("sum.png", Resources.sum);
        list.Images.Add("arrow_right.png", Resources.arrow_right);
        list.Images.Add("arrow_rotate_anticlockwise.png", Resources.arrow_rotate_anticlockwise);
        list.Images.Add("monitor_lightning.png", Resources.monitor_lightning);
        list.Images.Add("arrow_rotate_clockwise.png", Resources.arrow_rotate_clockwise);
        list.Images.Add("server_database.png", Resources.server_database);
        list.Images.Add("folder_user.png", Resources.folder_user);
        list.Images.Add("box.png", Resources.box);
        list.Images.Add("server_chart.png", Resources.server_chart);
        list.Images.Add("netezza_icon16.png", Resources.netezza_icon16);
        list.Images.Add("db2v2.png", Resources.db2v2);
        list.Images.Add("oracle.png", Resources.oracle);
        list.Images.Add("PostgreSQL.png", Resources.PostgreSQL);
        list.Images.Add("SQLite.png", Resources.SQLite);
        list.Images.Add("MySql.png", Resources.MySql);
        list.Images.Add("MSSQL16x16.png", Resources.MSSQL16x16);
        list.Images.Add("Folder.png", Resources.folder);
        list.Images.Add("Key.png", Resources.Key);
        list.Images.Add("folder_magnify.png", Resources.folder_magnify);
        list.Images.Add("server_connect.png", Resources.server_connect);
        list.Images.Add("table_column.png", Resources.table_column);
        list.Images.Add("msaccess_icon16x16.png", Resources.msaccess_icon16x16);
        list.Images.Add("hourglass.png", Resources.Hourglass);
    }
}
