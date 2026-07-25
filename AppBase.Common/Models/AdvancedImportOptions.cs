using AppBase.Common.Enums;

namespace AppBase.Common.Models;

public sealed class AdvancedImportOptions
{
    public ImportOptions? Type { get; set; }
    public string? ConnectionString { get; set; }
    public string? Destination { get; set; }
    public string? ImportType { get; set; }
    public bool TableExists { get; set; } = false;
    public Action<int>? ProgressFunction { get; set; }
    public Action<string>? MessageFunction { get; set; }
}
