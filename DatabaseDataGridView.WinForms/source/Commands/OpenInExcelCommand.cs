using DatabaseDataGridView.WinForms.Interfaces;
using System.Data;
using System.Diagnostics;

namespace DatabaseDataGridView.WinForms.Commands;

public class OpenInExcelCommand : IExportCommand
{
    private readonly IExportMakes _importExportTasks;
    private readonly DataTable _dataTable;
    private readonly List<object[]> _originalDataList;
    private readonly string _attachedSQL;
    private readonly Action<string> _doMessageAction;

    public OpenInExcelCommand(IExportMakes importExportTasks, DataTable dataTable, List<object[]> originalDataList, string attachedSQL, Action<string> doMessageAction)
    {
        _importExportTasks = importExportTasks;
        _dataTable = dataTable;
        _originalDataList = originalDataList;
        _attachedSQL = attachedSQL;
        _doMessageAction = doMessageAction;
    }

    public async Task ExecuteAsync()
    {
        string path = await ExcelStep1();
        Process fileopener = new Process();
        fileopener.StartInfo.FileName = "explorer";
        fileopener.StartInfo.Arguments = $"\"" + path + "\"";
        fileopener.Start();
        _doMessageAction?.Invoke("Opened");
    }

    private async Task<string> ExcelStep1(string? proposedPath = null)
    {
        string tmp = Path.GetTempPath();
        string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string path;
        if (proposedPath is null)
        {
            path = tmp + name + ".xlsb";
        }
        else
        {
            path = proposedPath;
        }

        if (path.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var exportCommand = new ExportXlsxCommand(path, _dataTable, _originalDataList, _attachedSQL, _importExportTasks);
            await exportCommand.ExecuteAsync();
        }
        else
        {
            await _importExportTasks.ExportCSVReaderFromDt(System.Text.Encoding.UTF8, _dataTable, path, _originalDataList);
        }

        return path;
    }
}
