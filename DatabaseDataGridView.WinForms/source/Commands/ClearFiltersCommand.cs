using DatabaseDataGridView.WinForms;
using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ClearFiltersCommand : IFilterCommand
{
    private readonly DataGridView _dataGridView;
    private readonly Dictionary<int, (object? filterValue, FilterType filterType)> _standardFilterDict;
    private readonly CueTextBox _tbSearch;
    private readonly Action _reloadOriginalRows;
    private readonly Label _lbCnt;
    private readonly Func<List<object[]>> _getWorkingRowsList;

    public ClearFiltersCommand(
        DataGridView dataGridView,
        Dictionary<int, (object? filterValue, FilterType filterType)> standardFilterDict,
        CueTextBox tbSearch,
        Action reloadOriginalRows,
        Label lbCnt,
        Func<List<object[]>> getWorkingRowsList)
    {
        _dataGridView = dataGridView;
        _standardFilterDict = standardFilterDict;
        _tbSearch = tbSearch;
        _reloadOriginalRows = reloadOriginalRows;
        _lbCnt = lbCnt;
        _getWorkingRowsList = getWorkingRowsList;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() =>
        {
            _dataGridView.Invoke(() =>
            {
                _dataGridView.RowCount = 0;
                (_standardFilterDict).Clear();
                _tbSearch.Text = "";
                _reloadOriginalRows();
                var workingRowsList = _getWorkingRowsList();
                _lbCnt.Text = workingRowsList.Count.ToString("N0");
                _dataGridView.RowCount = workingRowsList.Count;
                _dataGridView.Invalidate();
            });
        });
    }
}
