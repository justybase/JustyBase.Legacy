using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

/// <summary>
/// Subtle dark-theme chrome: thin borders and ComboBox resize fixes.
/// </summary>
internal static class DarkChromeHelper
{
    private static readonly HashSet<Control> BorderedControls = [];
    private static readonly HashSet<Control> ChildBorderHosts = [];
    private static readonly HashSet<GroupBox> ThemedGroupBoxes = [];
    private static readonly HashSet<ComboBox> ThemedComboBoxes = [];
    private static readonly HashSet<ComboBox> OwnerDrawComboBoxes = [];
    private static readonly HashSet<TabControl> FlattenedTabControls = [];
    private static readonly ConditionalWeakTable<Control, BorderTheme> BorderThemes = [];
    private static readonly ConditionalWeakTable<GroupBox, GroupBoxTheme> GroupBoxThemes = [];

    private sealed class BorderTheme
    {
        public Color Border;
    }

    private sealed class GroupBoxTheme
    {
        public Color Back;
        public Color Title;
        public Color Border;
        public bool DrawChildFieldBorders;
    }

    public static Color SubtleBorder(Color back, Color fore)
    {
        static byte Blend(byte a, byte b) => (byte)((a * 0.55f) + (b * 0.45f));
        return Color.FromArgb(Blend(back.R, fore.R), Blend(back.G, fore.G), Blend(back.B, fore.B));
    }

    /// <summary>Muted edge color for dark surfaces — softer than blending against full ForeColor.</summary>
    public static Color SoftBorder(Color back, bool dark)
    {
        if (!dark)
        {
            return Color.FromArgb(222, 226, 230);
        }

        static byte Lift(byte channel) => (byte)(channel + (byte)((255 - channel) * 0.20f));
        return Color.FromArgb(Lift(back.R), Lift(back.G), Lift(back.B));
    }

    public static void ApplyGroupBox(
        GroupBox groupBox,
        Color back,
        Color titleFore,
        Color border,
        bool drawChildFieldBorders = false)
    {
        ArgumentNullException.ThrowIfNull(groupBox);

        groupBox.BackColor = back;
        // Keep title readable, but avoid the stock GroupBox border (which uses ForeColor).
        groupBox.ForeColor = titleFore;

        GroupBoxTheme theme = GroupBoxThemes.GetOrCreateValue(groupBox);
        theme.Back = back;
        theme.Title = titleFore;
        theme.Border = border;
        theme.DrawChildFieldBorders = drawChildFieldBorders;

        if (ThemedGroupBoxes.Add(groupBox))
        {
            groupBox.Paint += ThemedGroupBox_Paint;
            groupBox.Resize += (_, _) => groupBox.Invalidate();
        }

        if (drawChildFieldBorders)
        {
            ClearFieldBordersRecursive(groupBox);
        }

        groupBox.Invalidate();
    }

    private static void ClearFieldBordersRecursive(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is TextBoxBase textBox)
            {
                textBox.BorderStyle = BorderStyle.None;
            }
            else if (child.HasChildren)
            {
                ClearFieldBordersRecursive(child);
            }
        }
    }

    public static void ApplyComboBox(ComboBox comboBox, Color back, Color fore, bool ownerDrawItems)
    {
        ArgumentNullException.ThrowIfNull(comboBox);

        comboBox.BackColor = back;
        comboBox.ForeColor = fore;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DrawMode = ownerDrawItems ? DrawMode.OwnerDrawFixed : DrawMode.Normal;
        AttachComboBoxResizeFix(comboBox);
        comboBox.DropDown -= ComboBox_DropDownTheme;
        comboBox.DropDown += ComboBox_DropDownTheme;

        if (ownerDrawItems)
        {
            AttachOwnerDrawRenderer(comboBox);
        }

        comboBox.Invalidate(true);
    }

    public static void ApplyToolStripComboBox(ToolStripComboBox toolStripComboBox, Color back, Color fore, bool dark)
    {
        toolStripComboBox.BackColor = back;
        toolStripComboBox.ForeColor = fore;

        ComboBox comboBox = toolStripComboBox.ComboBox;
        if (comboBox is null)
        {
            return;
        }

        if (dark)
        {
            ApplyComboBox(comboBox, back, fore, ownerDrawItems: true);
            AttachOwnerDrawRenderer(comboBox);
        }
        else
        {
            comboBox.BackColor = back;
            comboBox.ForeColor = fore;
            comboBox.FlatStyle = FlatStyle.Standard;
            comboBox.DrawMode = DrawMode.Normal;
            GridThemingHelper.ApplyScrollbarTheme(comboBox, false);
        }

        comboBox.Invalidate(true);
        toolStripComboBox.Invalidate();
    }

    public static void ApplyTextBox(TextBox textBox, Color back, Color fore, Color border)
    {
        textBox.BackColor = back;
        textBox.ForeColor = fore;
        textBox.BorderStyle = BorderStyle.None;
        _ = border;
    }

    public static void ApplyPanel(Panel panel, Color back, Color fore, Color border)
    {
        panel.BackColor = back;
        panel.ForeColor = fore;
        panel.BorderStyle = BorderStyle.None;

        bool hasComboChildren = false;
        foreach (Control child in panel.Controls)
        {
            if (child is ComboBox)
            {
                hasComboChildren = true;
                break;
            }
        }

        if (hasComboChildren)
        {
            AttachChildBorders(panel, border);
        }
        else
        {
            AttachSubtleBorder(panel, border);
        }
    }

    public static void ApplySplitContainer(SplitContainer splitContainer, Color back, Color fore, Color border)
    {
        splitContainer.BackColor = border;
        splitContainer.ForeColor = fore;
        splitContainer.BorderStyle = BorderStyle.None;
        splitContainer.Panel1.BackColor = back;
        splitContainer.Panel2.BackColor = back;
        splitContainer.Panel1.ForeColor = fore;
        splitContainer.Panel2.ForeColor = fore;
    }

    public static void ApplyStatusPanelChildBorders(Panel panel, Color border)
    {
        AttachChildBorders(panel, border);
    }

    public static void FlattenTabControl(TabControl tabControl)
    {
        ArgumentNullException.ThrowIfNull(tabControl);

        if (!FlattenedTabControls.Add(tabControl))
        {
            if (tabControl.IsHandleCreated)
            {
                RemoveClientEdge(tabControl);
            }

            return;
        }

        tabControl.HandleCreated += (_, _) => RemoveClientEdge(tabControl);
        if (tabControl.IsHandleCreated)
        {
            RemoveClientEdge(tabControl);
        }
    }

    public static void AttachChildBorders(Control host, Color border)
    {
        ArgumentNullException.ThrowIfNull(host);

        BorderThemes.GetOrCreateValue(host).Border = border;

        if (!ChildBorderHosts.Add(host))
        {
            host.Invalidate();
            return;
        }

        host.Paint += ChildBorderHost_Paint;
        host.Resize += (_, _) => host.Invalidate();
        host.ControlAdded += (_, _) => host.Invalidate();
        host.ControlRemoved += (_, _) => host.Invalidate();
        host.Invalidate();
    }

    private static void ChildBorderHost_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control host || !BorderThemes.TryGetValue(host, out BorderTheme? theme))
        {
            return;
        }

        DrawChildBorders(e.Graphics, host, theme.Border);
    }

    private static void DrawChildBorders(Graphics graphics, Control host, Color border)
    {
        using var pen = new Pen(border);
        DrawFieldBordersRecursive(graphics, host, host, pen);
    }

    private static void DrawFieldBordersRecursive(Graphics graphics, Control root, Control current, Pen pen)
    {
        foreach (Control child in current.Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            if (child is TextBoxBase or ComboBox or NumericUpDown or ListBox or DateTimePicker)
            {
                Point location = child.Parent is null
                    ? child.Location
                    : root.PointToClient(child.Parent.PointToScreen(child.Location));
                var rect = new Rectangle(location.X - 1, location.Y - 1, child.Width + 1, child.Height + 1);
                graphics.DrawRectangle(pen, rect);
                continue;
            }

            if (child.HasChildren && child is not GroupBox)
            {
                DrawFieldBordersRecursive(graphics, root, child, pen);
            }
        }
    }

    public static void AttachSubtleBorder(Control control, Color border)
    {
        ArgumentNullException.ThrowIfNull(control);

        BorderThemes.GetOrCreateValue(control).Border = border;

        if (!BorderedControls.Add(control))
        {
            control.Invalidate();
            return;
        }

        control.Paint += SubtleBorder_Paint;
        control.Resize += (_, _) => control.Invalidate();
        control.Invalidate();
    }

    private static void SubtleBorder_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control control || !BorderThemes.TryGetValue(control, out BorderTheme? theme))
        {
            return;
        }

        using var pen = new Pen(theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
    }

    private static void ThemedGroupBox_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not GroupBox groupBox || !GroupBoxThemes.TryGetValue(groupBox, out GroupBoxTheme? theme))
        {
            return;
        }

        Graphics g = e.Graphics;
        Size textSize = TextRenderer.MeasureText(
            g,
            groupBox.Text,
            groupBox.Font,
            Size.Empty,
            TextFormatFlags.NoPadding);
        int textHeight = Math.Max(textSize.Height, groupBox.Font.Height);
        int midY = textHeight / 2;
        Rectangle bounds = groupBox.ClientRectangle;

        // Cover the stock high-contrast GroupBox frame, then redraw a soft one.
        using (var coverPen = new Pen(theme.Back, 3f))
        {
            g.DrawRectangle(coverPen, 1, midY, bounds.Width - 3, bounds.Height - midY - 2);
        }

        using (var borderPen = new Pen(theme.Border))
        {
            const int textPadding = 6;
            int textLeft = 10;
            int textRight = textLeft + textSize.Width + 2;

            g.DrawLine(borderPen, 0, midY, Math.Max(0, textLeft - textPadding), midY);
            g.DrawLine(borderPen, Math.Min(bounds.Width - 1, textRight + textPadding), midY, bounds.Width - 1, midY);
            g.DrawLine(borderPen, 0, midY, 0, bounds.Height - 1);
            g.DrawLine(borderPen, 0, bounds.Height - 1, bounds.Width - 1, bounds.Height - 1);
            g.DrawLine(borderPen, bounds.Width - 1, midY, bounds.Width - 1, bounds.Height - 1);
        }

        Rectangle textBounds = new(10, 0, textSize.Width + 4, textHeight);
        using (var backBrush = new SolidBrush(theme.Back))
        {
            g.FillRectangle(backBrush, textBounds);
        }

        TextRenderer.DrawText(
            g,
            groupBox.Text,
            groupBox.Font,
            textBounds,
            theme.Title,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        if (theme.DrawChildFieldBorders)
        {
            DrawChildBorders(g, groupBox, theme.Border);
        }
    }

    private static void RemoveClientEdge(Control control)
    {
        if (!control.IsHandleCreated || control.IsDisposed)
        {
            return;
        }

        const int gwlExStyle = -20;
        const int wsExClientEdge = 0x200;
        const int wsExStaticEdge = 0x20000;
        IntPtr style = GetWindowLongPtr(control.Handle, gwlExStyle);
        long updated = style.ToInt64() & ~wsExClientEdge & ~wsExStaticEdge;
        SetWindowLongPtr(control.Handle, gwlExStyle, new IntPtr(updated));
        SetWindowPos(
            control.Handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        control.Invalidate(true);
    }

    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;
    private const int SWP_FRAMECHANGED = 0x0020;

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        int uFlags);

    private static void AttachComboBoxResizeFix(ComboBox comboBox)
    {
        if (!ThemedComboBoxes.Add(comboBox))
        {
            return;
        }

        comboBox.SizeChanged += (_, _) =>
        {
            if (!comboBox.IsHandleCreated || comboBox.IsDisposed)
            {
                return;
            }

            comboBox.BeginInvoke(() =>
            {
                if (comboBox.IsDisposed)
                {
                    return;
                }

                comboBox.Invalidate(true);
                comboBox.Update();
                comboBox.Parent?.Invalidate(true);
            });
        };
    }

    private static void ComboBox_DropDownTheme(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            GridThemingHelper.ApplyScrollbarTheme(comboBox, true);
        }
    }

    private static void AttachOwnerDrawRenderer(ComboBox comboBox)
    {
        if (OwnerDrawComboBoxes.Add(comboBox))
        {
            comboBox.DrawItem += DarkComboBox_DrawItem;
        }
    }

    private static void DarkComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        // Closed DropDownList uses Index == -1 / ComboBoxEdit — must still paint the selected text.
        string text;
        if (e.Index < 0)
        {
            text = comboBox.SelectedIndex >= 0 && comboBox.SelectedIndex < comboBox.Items.Count
                ? comboBox.Items[comboBox.SelectedIndex]?.ToString() ?? string.Empty
                : comboBox.Text ?? string.Empty;
        }
        else if (e.Index >= comboBox.Items.Count)
        {
            return;
        }
        else
        {
            text = comboBox.Items[e.Index]?.ToString() ?? string.Empty;
        }

        bool isEditPortion = e.Index < 0 || (e.State & DrawItemState.ComboBoxEdit) != 0;
        bool selected = !isEditPortion && (e.State & DrawItemState.Selected) != 0;
        Color backColor = selected
            ? ControlPaint.Light(comboBox.BackColor, 0.16f)
            : comboBox.BackColor;
        Color foreColor = comboBox.ForeColor.IsEmpty || comboBox.ForeColor == Color.Transparent
            ? SystemColors.ControlText
            : comboBox.ForeColor;

        using var backgroundBrush = new SolidBrush(backColor);
        using var textBrush = new SolidBrush(foreColor);
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

        var textBounds = new RectangleF(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        e.Graphics.DrawString(text, e.Font ?? comboBox.Font, textBrush, textBounds, format);

        if (!isEditPortion && (e.State & DrawItemState.Focus) != 0)
        {
            e.DrawFocusRectangle();
        }
    }
}
