using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
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
    IDatabaseRuntimeCatalogWriter,
    INetezzaCompletionContext,
    INetezzaCompletionRuntimeContext,
    INetezzaSchemaTableCatalog,
    INetezzaSchemaTableCatalogWriter
{
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private readonly object _catalogSync = new();

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

    private readonly Dictionary<string, Dictionary<int, DatabaseInfo>> _databaseDictionary = [];

    private readonly Dictionary<string, List<NetezzaColumnInfoRow>> _columnTablesDictionary = [];

    private readonly Dictionary<string, Dictionary<int, List<int>>> _baseTableConnections = [];

    private readonly Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> _databaseSchemaLookup = [];

    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _databaseOwners = [];

    private readonly Dictionary<string, Dictionary<string, Dictionary<int, string>>> _databaseTableDescriptions = [];

    /// <summary>
    /// Process catalog of Netezza table metadata keyed by connection.
    /// Owned here so DI consumers of <see cref="INetezzaSchemaTableCatalog"/>
    /// do not share a static dictionary.
    /// </summary>
    private readonly Dictionary<string, Dictionary<int, NetezzaTableInfo>> _tablesByConnection = [];

    public IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary
    {
        get
        {
            lock (_catalogSync)
                return Snapshot(_databaseDictionary);
        }
    }

    public IReadOnlyDictionary<string, List<NetezzaColumnInfoRow>> ColumnTablesDictionary
    {
        get
        {
            lock (_catalogSync)
                return SnapshotLists(_columnTablesDictionary);
        }
    }

    public IReadOnlyDictionary<string, Dictionary<int, List<int>>> BaseTableConnections
    {
        get
        {
            lock (_catalogSync)
                return SnapshotLists(_baseTableConnections);
        }
    }

    public IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> DatabaseSchemaLookup
    {
        get
        {
            lock (_catalogSync)
                return SnapshotTree(_databaseSchemaLookup);
        }
    }

    public IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> DatabaseOwners
    {
        get
        {
            lock (_catalogSync)
                return SnapshotTree(_databaseOwners);
        }
    }

    public IReadOnlyDictionary<string, Dictionary<string, Dictionary<int, string>>> DatabaseTableDescriptions
    {
        get
        {
            lock (_catalogSync)
                return SnapshotIntTree(_databaseTableDescriptions);
        }
    }

    public IReadOnlyDictionary<string, Dictionary<int, NetezzaTableInfo>> TablesByConnection
    {
        get
        {
            lock (_catalogSync)
                return Snapshot(_tablesByConnection);
        }
    }

    public void ReplaceDatabaseDictionary(Dictionary<string, Dictionary<int, DatabaseInfo>> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        lock (_catalogSync)
        {
            _databaseDictionary.Clear();
            foreach ((string connection, Dictionary<int, DatabaseInfo> databases) in value)
                _databaseDictionary[connection] = new Dictionary<int, DatabaseInfo>(databases);
        }
    }

    public void ClearDatabaseDictionary()
    {
        lock (_catalogSync)
            _databaseDictionary.Clear();
    }

    public void ClearDatabaseConnection(string connectionName)
    {
        lock (_catalogSync)
        {
            _databaseDictionary[connectionName] = [];
            _baseTableConnections[connectionName] = [];
        }
    }

    public void SetDatabase(string connectionName, int databaseId, DatabaseInfo database)
    {
        lock (_catalogSync)
        {
            if (!_databaseDictionary.TryGetValue(connectionName, out var databases))
                _databaseDictionary[connectionName] = databases = [];
            databases[databaseId] = database;
        }
    }

    public void EnsureBaseTableConnection(string connectionName, int databaseId)
    {
        lock (_catalogSync)
        {
            if (!_baseTableConnections.TryGetValue(connectionName, out var databases))
                _baseTableConnections[connectionName] = databases = [];
            databases.TryAdd(databaseId, []);
        }
    }

    public void ClearBaseTableConnection(string connectionName)
    {
        lock (_catalogSync)
        {
            if (_baseTableConnections.TryGetValue(connectionName, out var databases))
                databases.Clear();
        }
    }

    public void AddBaseTable(string connectionName, int databaseId, int tableId)
    {
        lock (_catalogSync)
        {
            EnsureBaseTableConnection(connectionName, databaseId);
            _baseTableConnections[connectionName][databaseId].Add(tableId);
        }
    }

    public void SetColumnTable(string connectionName, List<NetezzaColumnInfoRow> columns)
    {
        lock (_catalogSync)
            _columnTablesDictionary[connectionName] = new List<NetezzaColumnInfoRow>(columns);
    }

    public void SetColumnTableValue(string connectionName, int columnId, NetezzaColumnInfoRow column)
    {
        lock (_catalogSync)
        {
            if (!_columnTablesDictionary.TryGetValue(connectionName, out var columns))
                _columnTablesDictionary[connectionName] = columns = [];
            if (columnId >= 0 && columnId < columns.Count)
                columns[columnId] = column;
        }
    }

    public void SetSchemaLookup(string connectionName, Dictionary<string, Dictionary<string, (string owner, int tableId)>> lookup)
    {
        lock (_catalogSync)
            _databaseSchemaLookup[connectionName] = CloneNested(lookup);
    }

    public void SetOwners(string connectionName, Dictionary<string, Dictionary<string, string>> owners)
    {
        lock (_catalogSync)
            _databaseOwners[connectionName] = CloneNested(owners);
    }

    public void SetTableDescription(string connectionName, string databaseName, int tableId, string? description)
    {
        lock (_catalogSync)
        {
            if (!_databaseTableDescriptions.TryGetValue(connectionName, out var databases))
                _databaseTableDescriptions[connectionName] = databases = [];
            if (!databases.TryGetValue(databaseName, out var tables))
                databases[databaseName] = tables = [];
            tables[tableId] = description ?? string.Empty;
        }
    }

    public IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> GetDatabaseSnapshot() => DatabaseDictionary;

    public void ClearConnection(string connectionName)
    {
        lock (_catalogSync)
            _tablesByConnection.Remove(connectionName);
    }

    public void ReplaceConnection(string connectionName, Dictionary<int, NetezzaTableInfo> tables)
    {
        lock (_catalogSync)
            _tablesByConnection[connectionName] = new Dictionary<int, NetezzaTableInfo>(tables);
    }

    public void SetTable(string connectionName, int tableId, NetezzaTableInfo table)
    {
        lock (_catalogSync)
        {
            if (!_tablesByConnection.TryGetValue(connectionName, out var tables))
                _tablesByConnection[connectionName] = tables = [];
            tables[tableId] = table;
        }
    }

    public void ClearSchemaLookup(string connectionName)
    {
        lock (_catalogSync)
        {
            if (_databaseSchemaLookup.TryGetValue(connectionName, out var value))
                value.Clear();
        }
    }

    public void ClearDatabaseOwners(string connectionName)
    {
        lock (_catalogSync)
        {
            if (_databaseOwners.TryGetValue(connectionName, out var value))
                value.Clear();
        }
    }

    private static Dictionary<TKey, Dictionary<TInnerKey, TValue>> Snapshot<TKey, TInnerKey, TValue>(
        Dictionary<TKey, Dictionary<TInnerKey, TValue>> source)
        where TKey : notnull
        where TInnerKey : notnull
    {
        var copy = new Dictionary<TKey, Dictionary<TInnerKey, TValue>>(source.Comparer);
        foreach ((TKey key, Dictionary<TInnerKey, TValue> value) in source)
            copy[key] = new Dictionary<TInnerKey, TValue>(value, value.Comparer);

        return copy;
    }

    private static Dictionary<TKey, List<TValue>> SnapshotLists<TKey, TValue>(
        Dictionary<TKey, List<TValue>> source)
        where TKey : notnull
    {
        Dictionary<TKey, List<TValue>> copy = [];
        foreach ((TKey key, List<TValue> value) in source)
            copy[key] = new List<TValue>(value);
        return copy;
    }

    private static Dictionary<TKey, Dictionary<TInnerKey, List<TValue>>> SnapshotLists<TKey, TInnerKey, TValue>(
        Dictionary<TKey, Dictionary<TInnerKey, List<TValue>>> source)
        where TKey : notnull
        where TInnerKey : notnull
    {
        Dictionary<TKey, Dictionary<TInnerKey, List<TValue>>> copy = [];
        foreach ((TKey key, Dictionary<TInnerKey, List<TValue>> value) in source)
            copy[key] = SnapshotLists(value);
        return copy;
    }

    private static Dictionary<string, Dictionary<string, Dictionary<string, TValue>>> SnapshotTree<TValue>(
        Dictionary<string, Dictionary<string, Dictionary<string, TValue>>> source)
    {
        var copy = new Dictionary<string, Dictionary<string, Dictionary<string, TValue>>>(source.Comparer);
        foreach ((string key, Dictionary<string, Dictionary<string, TValue>> value) in source)
            copy[key] = Snapshot(value);
        return copy;
    }

    private static Dictionary<string, Dictionary<string, TValue>> CloneNested<TValue>(
        Dictionary<string, Dictionary<string, TValue>> source)
    {
        var copy = new Dictionary<string, Dictionary<string, TValue>>(source.Comparer);
        foreach ((string key, Dictionary<string, TValue> value) in source)
            copy[key] = new Dictionary<string, TValue>(value, value.Comparer);
        return copy;
    }

    private static Dictionary<string, Dictionary<string, Dictionary<int, string>>> SnapshotIntTree(
        Dictionary<string, Dictionary<string, Dictionary<int, string>>> source)
    {
        var copy = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(source.Comparer);
        foreach ((string key, Dictionary<string, Dictionary<int, string>> value) in source)
            copy[key] = Snapshot(value);
        return copy;
    }

    [GeneratedRegex(@"(___expCsv|___expXlsx): (?<sql>.*)\s+->\s+(?<filePath>(.*\.(xlsx|xlsb|justData|[a-z]{3})|nul))", RegexOptions.IgnoreCase | RegexOptions.Singleline, "pl-PL")]
    private static partial Regex ExportRegex();
}
