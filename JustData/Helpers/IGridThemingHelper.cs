using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

public interface IGridThemingHelper
{
    void ApplyScrollbarTheme(Control control, bool dark);
    void ApplyScrollbarThemeRecursive(Control root, bool dark);
    void RecreateThemedDataGridHandlesRecursive(Control root);
    void EnableDarkScrollbars(Control control, bool enable);
}
