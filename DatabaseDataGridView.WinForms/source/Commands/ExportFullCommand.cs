
using DatabaseDataGridView.WinForms.Interfaces;
using DatabaseDataGridView.WinForms.Extensions;
using System.Collections.Specialized;
using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class ExportFullCommand : IExportCommand
{
    private readonly DataTable _headerDataTable;
    private readonly List<object[]> _rowList;
    private readonly ICustomDataGridView _parentControl;
    private readonly IExportMakes _exportService;

    public ExportFullCommand(DataTable headerDataTable, List<object[]> rowList, ICustomDataGridView parentControl, IExportMakes exportService)
    {
        _headerDataTable = headerDataTable;
        _rowList = rowList;
        _parentControl = parentControl;
        _exportService = exportService;
    }


    public async Task ExecuteAsync()
    {
        await ExportExcelFull(_headerDataTable, _rowList, _parentControl);
    }

    private async Task ExportExcelFull(DataTable? dt = null, List<object[]>? list = null, ICustomDataGridView? dtV = null)
    {
        try
        {
            string name = StringExtensions.RandomName("Exported_");
            string tmp = Path.GetTempPath() + "Exported\\";
            if (!Directory.Exists(tmp))
                Directory.CreateDirectory(tmp);

            string path = tmp + name + ".xlsx";

            StringCollection paths = new StringCollection();

            var gn = new GetNameFromUser()
            {
                StartPosition = FormStartPosition.Manual
            };
            var p = Control.MousePosition;
            p.Offset(-100, -200);
            gn.Location = p;
            if (gn.ShowDialog() == DialogResult.OK)
            {
                name = gn.GetName();
                path = tmp + name + ".xlsb";

                if (dtV is null)
                {
                    throw new InvalidOperationException("The source grid is unavailable.");
                }

                if (gn.IsAllTabls())
                {

                    List<(string title, DataTable dt, List<object[]> rows, string sql)> listTabs = new();

                    var tc = dtV.ParentParent;
                    if (tc is not TabControl)
                    {
                        throw new Exception("Parent control is not a TabControl.");
                    }
                    foreach (TabPage tp in tc.TabPages)
                    {
                        var myGrids = tp.Controls.OfType<CustomDataGridView>().ToArray();
                        if (myGrids == null || myGrids.Length == 0)
                        {
                            continue;
                        }
                        var myGrid = myGrids[0];
                        if (myGrid is not null)
                        {
                            listTabs.Add((tp.Text,myGrid.CurrentDataTable, myGrid.RowsList, myGrid.AttachedSQL));
                        }
                    }

                    await _exportService.ExportExcelAllTabsAsync(path, listTabs);
                }
                else
                {
                    await _exportService.SaveAsXlsx(path, dt, list, dtV.AttachedSQL);
                }
            }
            paths.Add(path);
            Clipboard.SetFileDropList(paths);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
