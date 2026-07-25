using AppBase.Common;
using FastColoredTextBoxNS;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

internal static class DocumentMapLayoutHelper
{
    private const string HostTag = "DocMapHost";

    public static int GetPreferredWidth(int dpi) => DpiScale.Scale(120, dpi);

    public static void Show(FastColoredTextBox editor, DocumentMap map)
    {
        Panel host = EnsureHost(editor);
        if (!host.Controls.Contains(map))
        {
            map.Dock = DockStyle.Right;
            map.Width = GetPreferredWidth(editor.DeviceDpi);
            host.Controls.Add(map);
        }

        map.Visible = true;
        RestoreEditorChrome(editor);
        host.PerformLayout();
    }

    public static void Hide(DocumentMap map, FastColoredTextBox editor)
    {
        map.Visible = false;
        RestoreEditorChrome(editor);
        editor.Parent?.PerformLayout();
    }

    public static void ConfigureMapColors(DocumentMap map, bool darkTheme, IList<byte>? backRgb, IList<byte>? foreRgb)
    {
        if (darkTheme && backRgb is { Count: 3 } && foreRgb is { Count: 3 })
        {
            map.BackColor = Color.FromArgb(backRgb[0], backRgb[1], backRgb[2]);
            map.ForeColor = Color.FromArgb(foreRgb[0], foreRgb[1], foreRgb[2]);
        }
    }

    private static Panel EnsureHost(FastColoredTextBox editor)
    {
        if (editor.Parent is Panel host && HostTag.Equals(host.Tag))
        {
            return host;
        }

        Control parent = editor.Parent
            ?? throw new InvalidOperationException("SQL editor has no parent control.");

        host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Tag = HostTag,
        };

        if (parent is TableLayoutPanel layout)
        {
            int row = layout.GetRow(editor);
            int column = layout.GetColumn(editor);
            layout.Controls.Remove(editor);
            layout.Controls.Add(host, column, row);
        }
        else
        {
            int index = parent.Controls.GetChildIndex(editor);
            DockStyle dock = editor.Dock;
            AnchorStyles anchor = editor.Anchor;
            Rectangle bounds = editor.Bounds;

            parent.Controls.Remove(editor);
            host.Dock = dock;
            host.Anchor = anchor;
            if (dock == DockStyle.None)
            {
                host.Bounds = bounds;
            }

            parent.Controls.Add(host);
            parent.Controls.SetChildIndex(host, index);
        }

        editor.Dock = DockStyle.Fill;
        editor.Margin = Padding.Empty;
        host.Controls.Add(editor);
        return host;
    }

    private static void RestoreEditorChrome(FastColoredTextBox editor)
    {
        editor.ShowScrollBars = true;
        editor.PerformLayout();
        editor.Invalidate(true);
        editor.Update();
    }
}
