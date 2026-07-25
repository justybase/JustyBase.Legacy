using DatabaseDataGridView.WinForms.Coloring;
using System.Data;

namespace DatabaseDataGridView.WinForms;

public interface ICustomDataGridView
{
    TabControl? ParentParent { get; }
    string AttachedSQL { get; set; }
    DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; }
    DataGridView InnerDataGridView { get; }
    DataTable CurrentDataTable { get; set; }
    string DateTimeFormat { get; set; }
    string DecimalFormat { get; set; }
    bool ForceDecimalFormat { get; set; }
    int GrifOffsetHeight { get; set; }
    Color GroupBackgroundActiveEnd { get; set; }
    Color GroupBackgroundActiveMiddle { get; set; }
    Color GroupBackgroundActiveStart { get; set; }
    Color GroupBackgroundEnd { get; set; }
    Color GroupBackgroundMiddle { get; set; }
    Color GroupBackgroundStart { get; set; }
    string IntegerFormat { get; set; }
    bool IsEmpty { get; set; }
    List<object[]> RowsList { get; set; }
    DataTable ShemaDataTable { set; }
    List<object[]> WorkingRowsList { get; }

    event MouseEventHandler DataGridMouseDown;
    event Action<string> WriteStats;

    void ClearDataGridView();
    void ClearFilters();
    void EnsureColumnList();
    void FinishColorize(IColorTheme colorTheme, bool useSpecialColoring);
    void HideFilters();
    void InitGrid(bool previewMode = false);
}