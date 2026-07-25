namespace DatabaseDataGridView.WinForms;

public interface IUiHelperService
{
    void DoubleBufDateGridView(DataGridView dataGridView);
    void ColorComboBox_DrawItem(object sender, DrawItemEventArgs e, bool useSpecialColoring, Brush generalBrush);
}