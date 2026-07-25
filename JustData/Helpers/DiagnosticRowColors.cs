using System.Drawing;
using System.Windows.Forms;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI.Helpers;

internal static class DiagnosticRowColors
{
    public static void Apply(DataGridViewRow row, LintSeverity severity, bool isDarkTheme)
    {
        var style = row.DefaultCellStyle;
        var (back, fore) = Get(severity, isDarkTheme);
        style.BackColor = back;
        style.ForeColor = fore;
    }

    public static (Color Back, Color Fore) Get(LintSeverity severity, bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            return severity switch
            {
                LintSeverity.Error => (Color.FromArgb(60, 30, 30), Color.FromArgb(255, 160, 160)),
                LintSeverity.Warning => (Color.FromArgb(55, 48, 20), Color.FromArgb(255, 210, 100)),
                LintSeverity.Information => (Color.FromArgb(25, 40, 55), Color.FromArgb(150, 200, 255)),
                LintSeverity.Hint => (Color.FromArgb(35, 35, 35), Color.FromArgb(160, 160, 160)),
                _ => (Color.FromArgb(30, 30, 30), Color.FromArgb(241, 241, 241))
            };
        }

        return severity switch
        {
            LintSeverity.Error => (MyColors.LogErrorStdColor, Color.FromArgb(140, 0, 0)),
            LintSeverity.Warning => (Color.FromArgb(255, 248, 210), Color.FromArgb(110, 75, 0)),
            LintSeverity.Information => (Color.FromArgb(225, 240, 255), Color.FromArgb(0, 70, 130)),
            LintSeverity.Hint => (Color.FromArgb(245, 245, 245), Color.FromArgb(70, 70, 70)),
            _ => (SystemColors.Window, SystemColors.WindowText)
        };
    }
}
