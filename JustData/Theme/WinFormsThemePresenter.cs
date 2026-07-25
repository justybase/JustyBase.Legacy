using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBaseLegacy.UI.Helpers;

namespace JustyBaseLegacy.UI.Theme;

/// <summary>Applies document-level WinForms theme state without owning the shell form.</summary>
public sealed class WinFormsThemePresenter
{
    public void RefreshAutocompletePopups(
        IEnumerable<FastColoredTextBox> editors,
        IColorTheme colorTheme,
        bool dark)
    {
        ArgumentNullException.ThrowIfNull(editors);
        ArgumentNullException.ThrowIfNull(colorTheme);

        foreach (FastColoredTextBox editor in editors)
        {
            try
            {
                if (editor.IsDisposed || editor.Tag is not TbInfo { PopupMenu: { } popup })
                    continue;

                popup.ApplyAppearance(
                    colorTheme.CurrentFctbColors.FctbBackColor,
                    colorTheme.CurrentFctbColors.FctbForeColor,
                    colorTheme.CurrentFctbColors.FctbPopupMenuSelected);
                GridThemingHelper.ApplyScrollbarTheme(popup.ListViewHost, dark);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine($"Applying popup menu theme failed: {exception.GetType().Name}");
            }
        }
    }
}
