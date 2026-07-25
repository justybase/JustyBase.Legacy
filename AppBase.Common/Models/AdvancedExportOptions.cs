using AppBase.Common.Enums;
using System.Text;

namespace AppBase.Common.Models;
public sealed class AdvancedExportOptions
{
    public ExportOptions? Type { get; set; }
    public string? Path { get; set; }
    public string? Delimiter { get; set; }
    public string? Linedelimiter { get; set; }
    public string? NullValue { get; set; }
    public bool Header { get; set; }
    public Encoding? Encod { get; set; }
    public string? CompressionString { get; set; }
    public string? TabName { get; set; }
    public string? PivotTableTabName { get; set; }
    public string? PivotTableName { get; set; }
    public bool PrintHeaders { get; set; }
    public string? StartCell { get; set; }
    public bool ForceRefresh { get; set; }
    public bool Clear { get; set; }

}

