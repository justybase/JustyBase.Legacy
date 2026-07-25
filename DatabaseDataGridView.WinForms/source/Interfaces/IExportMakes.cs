using System.Data;

namespace DatabaseDataGridView.WinForms.Interfaces;

public interface IExportMakes
{
    Task<long> ExportCSVReaderFromDt(System.Text.Encoding enc, DataTable dt, string csvPath, List<object[]> rows);
    Task SaveAsXlsx(string xlsxPath, DataTable? dtExp = null, List<object[]>? rowsList = null, string? sql = null);
    Task ExportExcelAllTabsAsync(string xlsxPath, IEnumerable<(string title, DataTable dt, List<object[]> rows, string sql)> items);
}