using DatabaseDataGridView.WinForms;

namespace JustyBaseLegacy.UI.Helpers;

internal static class DataGridDpiHelper
{
    public static void Apply(CustomDataGridView grid)
    {
        grid?.ApplyDpiMetrics();
    }
}
