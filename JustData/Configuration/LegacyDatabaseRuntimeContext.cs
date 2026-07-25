using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using System.Drawing;
using System.Text.RegularExpressions;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Shared database/catalog state used by providers and autocomplete.
/// Keeping this state outside the main window makes the provider boundary
/// explicit and allows schema services to be tested without constructing a Form.
/// </summary>
public sealed partial class LegacyDatabaseRuntimeContext :
    IDatabaseRuntimeContext,
    INetezzaCompletionContext,
    INetezzaCompletionRuntimeContext
{
    private readonly IApplicationSettingsContext _applicationSettingsContext;

    public Color LogErrorStdColor { get; set; } = Color.Empty;

    public Regex RxExportCsvXlsx { get; } = ExportRegex();

    public LegacyDatabaseRuntimeContext(IApplicationSettingsContext applicationSettingsContext)
    {
        _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
    }

    public IApplicationConfig Config => _applicationSettingsContext.Config;

    public string ConfigDirectory => _applicationSettingsContext.ConfigDirectory;

    public bool SchemaRefreshed { get; set; } = true;

    public string SelectedConnectionName { get; set; } = string.Empty;

    public string SelectedDatabase { get; set; } = string.Empty;

    public Dictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary { get; set; } = [];

    public Dictionary<string, List<NetezzaColumnInfoRow>> ColumnTablesDictionary { get; } = [];

    public Dictionary<string, Dictionary<int, List<int>>> BaseTableConnections { get; } = [];

    public Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> DatabaseSchemaLookup { get; } = [];

    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> DatabaseOwners { get; } = [];

    public Dictionary<string, Dictionary<string, Dictionary<int, string>>> DatabaseTableDescriptions { get; } = [];

    [GeneratedRegex(@"(___expCsv|___expXlsx): (?<sql>.*)\s+->\s+(?<filePath>(.*\.(xlsx|xlsb|justData|[a-z]{3})|nul))", RegexOptions.IgnoreCase | RegexOptions.Singleline, "pl-PL")]
    private static partial Regex ExportRegex();
}
