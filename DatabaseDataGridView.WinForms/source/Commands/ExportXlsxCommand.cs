using DatabaseDataGridView.WinForms.Interfaces;
using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ExportXlsxCommand : IExportCommand
{
    private readonly string _path;
    private readonly DataTable _headerDataTable;
    private readonly List<object[]> _rowList;
    private readonly string _attachedSQL;
    private readonly IExportMakes _importExportTasks;

    public ExportXlsxCommand(string path, DataTable headerDataTable, List<object[]> rowList, string attachedSQL, IExportMakes importExportTasks)
    {
        _path = path;
        _headerDataTable = headerDataTable;
        _rowList = rowList;
        _attachedSQL = attachedSQL;
        _importExportTasks = importExportTasks;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() => _importExportTasks.SaveAsXlsx(_path, _headerDataTable, _rowList, _attachedSQL));
    }
}

