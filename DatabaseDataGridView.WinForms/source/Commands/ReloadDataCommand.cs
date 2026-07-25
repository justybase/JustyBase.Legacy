using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ReloadDataCommand : IFilterCommand
{
    private readonly List<object[]> _workingRowsList;
    private readonly List<object[]> _originalDataList;

    public ReloadDataCommand(List<object[]> workingRowsList, List<object[]> originalDataList)
    {
        _workingRowsList = workingRowsList;
        _originalDataList = originalDataList;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() =>
        {
            _workingRowsList.Clear();
            foreach (var item in _originalDataList)
            {
                _workingRowsList.Add(item);
            }
        });
    }
}