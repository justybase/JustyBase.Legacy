using AppBase.Common.WindowManagement;
using DatabaseDataGridView.WinForms;
using System;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

public sealed class GridThemingHelper : IGridThemingHelper
{
    public static readonly GridThemingHelper Default = new();

    public static void ApplyScrollbarTheme(Control control, bool dark)
        => Default.DoApplyScrollbarTheme(control, dark);

    public static void ApplyScrollbarThemeRecursive(Control root, bool dark)
        => Default.DoApplyScrollbarThemeRecursive(root, dark);

    public static void RecreateThemedDataGridHandlesRecursive(Control root)
        => Default.DoRecreateThemedDataGridHandlesRecursive(root);

    public static void EnableDarkScrollbars(Control control, bool enable)
        => Default.DoApplyScrollbarTheme(control, enable);

    public void DoApplyScrollbarTheme(Control control, bool dark)
    {
        if (control is null)
        {
            return;
        }

        string theme = dark ? "DarkMode_Explorer" : "Explorer";

        void Apply()
        {
            if (!control.IsHandleCreated)
            {
                return;
            }

            ApplyThemeToHandle(control.Handle, theme);
            WindowNativeMethods.EnumChildWindows(
                control.Handle,
                (hwnd, _) =>
                {
                    ApplyThemeToHandle(hwnd, theme);
                    return true;
                },
                IntPtr.Zero);
        }

        if (control.IsHandleCreated)
        {
            Apply();
        }
        else
        {
            control.HandleCreated += (_, _) => Apply();
        }
    }

    public void DoApplyScrollbarThemeRecursive(Control root, bool dark)
    {
        if (root is null || root.IsDisposed)
        {
            return;
        }

        if (root is TreeView or DataGridView or ComboBox or ListBox or ListView or TextBox
            || (root is ScrollableControl scrollable && scrollable.AutoScroll))
        {
            DoApplyScrollbarTheme(root, dark);
        }

        foreach (Control child in root.Controls)
        {
            DoApplyScrollbarThemeRecursive(child, dark);
        }
    }

    public void DoRecreateThemedDataGridHandlesRecursive(Control root)
    {
        if (root is null || root.IsDisposed)
        {
            return;
        }

        if (root is ThemedDataGridView themedGrid)
        {
            themedGrid.RecreateForThemeChange();
        }

        foreach (Control child in root.Controls)
        {
            DoRecreateThemedDataGridHandlesRecursive(child);
        }
    }

    void IGridThemingHelper.ApplyScrollbarTheme(Control control, bool dark)
        => DoApplyScrollbarTheme(control, dark);

    void IGridThemingHelper.ApplyScrollbarThemeRecursive(Control root, bool dark)
        => DoApplyScrollbarThemeRecursive(root, dark);

    void IGridThemingHelper.RecreateThemedDataGridHandlesRecursive(Control root)
        => DoRecreateThemedDataGridHandlesRecursive(root);

    void IGridThemingHelper.EnableDarkScrollbars(Control control, bool enable)
        => DoApplyScrollbarTheme(control, enable);

    private static void ApplyThemeToHandle(IntPtr hwnd, string theme)
    {
        if (hwnd != IntPtr.Zero)
        {
            WindowNativeMethods.SetWindowTheme(hwnd, theme, null);
        }
    }
}
