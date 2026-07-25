using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ApplySearchFilterCommand : IFilterCommand
{
    private readonly string _searchText;
    private readonly DataGridView _dataGridView;
    private readonly Func<string, List<object[]>> _filterWorkingList;
    private readonly Action<List<object[]>> _setWorkingRowsList;
    private readonly Label _lbCnt;
    private readonly Action _resetSummaries;

    public ApplySearchFilterCommand(
        string searchText,
        DataGridView dataGridView,
        Func<string, List<object[]>> filterWorkingList,
        Action<List<object[]>> setWorkingRowsList,
        Label lbCnt,
        Action resetSummaries)
    {
        _searchText = searchText;
        _dataGridView = dataGridView;
        _filterWorkingList = filterWorkingList;
        _setWorkingRowsList = setWorkingRowsList;
        _lbCnt = lbCnt;
        _resetSummaries = resetSummaries;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() =>
        {
            var filteredRows = _filterWorkingList(_searchText);
            
            _dataGridView.Invoke(() =>
            {
                _dataGridView.RowCount = 0;
                _setWorkingRowsList(filteredRows);
                _lbCnt.Text = filteredRows.Count.ToString("N0");
                _dataGridView.RowCount = filteredRows.Count;
                _dataGridView.Invalidate();
                _resetSummaries();
            });
        });
    }
}