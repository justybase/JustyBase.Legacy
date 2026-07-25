using System;
using System.Drawing;
using System.Windows.Forms;
using AppBase.Common;

namespace JustyBaseLegacy.UI.Helpers;

internal static class GridDpiMetrics
{
    public static int RowHeight(Font font, int dpi, int paddingLogical = 10) =>
        (int)Math.Ceiling(font.GetHeight()) + DpiScale.Scale(paddingLogical, dpi);

    public static void Apply(DataGridView grid, int dpi, int paddingLogical = 10, bool updateExistingRows = true)
    {
        if (grid == null)
        {
            return;
        }

        int rowHeight = RowHeight(grid.Font, dpi, paddingLogical);
        int cellPadding = DpiScale.Scale(4, dpi);

        grid.RowTemplate.Height = rowHeight;
        grid.DefaultCellStyle.Padding = new Padding(cellPadding, DpiScale.Scale(2, dpi), cellPadding, DpiScale.Scale(2, dpi));

        if (grid.ColumnHeadersVisible)
        {
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = rowHeight + DpiScale.Scale(4, dpi);
        }

        if (updateExistingRows)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.Height = rowHeight;
            }
        }
    }
}
