using System.Drawing;
using System.Runtime.CompilerServices;

namespace DatabaseDataGridView.WinForms;

public class UiHelperService : IUiHelperService
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_DoubleBuffered")]
    private static extern void SetDoubleBuffered(Control control, bool value);

    public void DoubleBufDateGridView(DataGridView dataGridView)
    {
        if (SystemInformation.TerminalServerSession)
        {
            return;
        }
        SetDoubleBuffered(dataGridView, true);
    }

    public void ColorComboBox_DrawItem(object sender, DrawItemEventArgs e, bool useSpecialColoring, Brush generalBrush)
    {
        if (!useSpecialColoring || sender is not ComboBox comboBox || e.Index < 0)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color back = selected ? ControlPaint.Light(comboBox.BackColor, 0.12f) : comboBox.BackColor;
        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        string? text = comboBox.Items[e.Index]?.ToString();
        if (!string.IsNullOrEmpty(text))
        {
            using var textBrush = new SolidBrush(comboBox.ForeColor);
            e.Graphics.DrawString(text, e.Font ?? comboBox.Font, textBrush, e.Bounds, StringFormat.GenericDefault);
        }
    }

}
