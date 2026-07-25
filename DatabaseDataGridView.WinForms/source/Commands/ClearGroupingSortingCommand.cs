using DatabaseDataGridView.WinForms.Models;
using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ClearGroupingSortingCommand : IFilterCommand
{
    private readonly List<int> _groupByColumnNums;
    private readonly BindingSource _source;
    private readonly List<(int index, SortInfo sortInfo)> _sortInfoList;
    private readonly int _isTechColNameIndex;
    private readonly Action _removeGroupColumns;
    private readonly List<(string filter, int level)> _expandedGroups;
    private readonly DataGridView _dataGridView;
    private readonly Func<string, List<object[]>> _filterWorkingList;
    private readonly Action<List<object[]>> _setWorkingRowsList;
    private readonly CueTextBox _tbSearch;

    public ClearGroupingSortingCommand(
        List<int> groupByColumnNums,
        BindingSource source,
        List<(int index, SortInfo sortInfo)> sortInfoList,
        int isTechColNameIndex,
        Action removeGroupColumns,
        List<(string filter, int level)> expandedGroups,
        DataGridView dataGridView,
        Func<string, List<object[]>> filterWorkingList,
        Action<List<object[]>> setWorkingRowsList,
        CueTextBox tbSearch)
    {
        _groupByColumnNums = groupByColumnNums;
        _source = source;
        _sortInfoList = sortInfoList;
        _isTechColNameIndex = isTechColNameIndex;
        _removeGroupColumns = removeGroupColumns;
        _expandedGroups = expandedGroups;
        _dataGridView = dataGridView;
        _filterWorkingList = filterWorkingList;
        _setWorkingRowsList = setWorkingRowsList;
        _tbSearch = tbSearch;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() =>
        {
            _groupByColumnNums.Clear();
            if (_source.IsSorted)
            {
                _source.RemoveSort();
            }
            _sortInfoList.Clear();

            if (_isTechColNameIndex != -1)
            {
                _removeGroupColumns();
                _expandedGroups.Clear();

                _dataGridView.Invoke(() =>
                {
                    _dataGridView.RowCount = 0;
                    var filteredRows = _filterWorkingList(_tbSearch.Text);
                    _setWorkingRowsList(filteredRows);
                    _tbSearch.Enabled = true;
                    _dataGridView.RowCount = filteredRows.Count;
                });
            }
        });
    }
}