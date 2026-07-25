using DatabaseDataGridView.WinForms.Interfaces;
using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class DownloadCommand : IExportCommand
{
    private readonly IExportMakes _exportService;
    private readonly DataTable _dataTable;
    private readonly List<object[]> _originalDataList;
    private readonly string _attachedSQL;
    private readonly Action<string> _doMessageAction;
    private readonly Func<SaveFileDialog> _saveFileDialogFactory;

    public DownloadCommand(IExportMakes exportService, DataTable dataTable, List<object[]> originalDataList, string attachedSQL, Action<string> doMessageAction, Func<SaveFileDialog> saveFileDialogFactory)
    {
        _exportService = exportService;
        _dataTable = dataTable;
        _originalDataList = originalDataList;
        _attachedSQL = attachedSQL;
        _doMessageAction = doMessageAction;
        _saveFileDialogFactory = saveFileDialogFactory;
    }

    public async Task ExecuteAsync()
    {
        var saveFileDialog = _saveFileDialogFactory();
        saveFileDialog.Filter = "xlsb Files(*.xlsb)|*.xlsb|csv Files(*.csv)|*.csv";

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        string filePath = saveFileDialog.FileName;
        await ExcelStep1(filePath);
        _doMessageAction?.Invoke("Saved");
    }

    private async Task ExcelStep1(string proposedPath)
    {
        if (proposedPath.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase) || proposedPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            await _exportService.SaveAsXlsx(proposedPath, _dataTable, _originalDataList, _attachedSQL);
        }
        else
        {
            await _exportService.ExportCSVReaderFromDt(System.Text.Encoding.UTF8, _dataTable, proposedPath, _originalDataList);
        }
    }
}
