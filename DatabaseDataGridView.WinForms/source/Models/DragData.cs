namespace DatabaseDataGridView.WinForms.Models;

internal sealed class DragData(string cellvalue, string dgvType)
{
    public string Cellvalue { get; set; } = cellvalue;
    public string DgvType { get; set; } = dgvType;
}
