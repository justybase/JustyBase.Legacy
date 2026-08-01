using AppBase.Common.Enums;
using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustData.Application.Login;
using JustData.Application.Schema;
using System.Collections.Concurrent;

namespace JustyBaseLegacy.UI.Schema;

/// <summary>
/// Transitional adapter over the current provider sessions. It normalizes the
/// Netezza, DB2, Oracle, PostgreSQL, SQL Server, SQLite and other existing providers
/// through the same provider-neutral repository contract.
/// </summary>
public sealed class LegacySchemaRepository : ISchemaRepository, IOutlineRepository
{
    private readonly IGeneralDbService _generalDbService;
    private readonly AppBase.Common.Interfaces.IDatabaseRuntimeContext _databaseRuntimeContext;
    private readonly INetezzaCompletionRuntimeContext _completionRuntime;
    private readonly IConnectionSessionRegistry _connectionSessions;
    private readonly INetezzaSchemaTableCatalogWriter _schemaTables;
    private readonly IConnectionProfileCatalog _profiles;
    private readonly ConcurrentDictionary<string, Task> _refreshes = new(StringComparer.OrdinalIgnoreCase);

    public LegacySchemaRepository(
        IGeneralDbService generalDbService,
        AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext,
        INetezzaCompletionRuntimeContext completionRuntime,
        IConnectionSessionRegistry connectionSessions,
        INetezzaSchemaTableCatalogWriter schemaTables,
        IConnectionProfileCatalog profiles)
    {
        _generalDbService = generalDbService ?? throw new ArgumentNullException(nameof(generalDbService));
        _databaseRuntimeContext = databaseRuntimeContext ?? throw new ArgumentNullException(nameof(databaseRuntimeContext));
        _completionRuntime = completionRuntime ?? throw new ArgumentNullException(nameof(completionRuntime));
        _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }

    public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<string> names = _profiles.ConnectionNames;
        if (string.IsNullOrWhiteSpace(connectionName))
            names = names.Concat(_connectionSessions.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        else
            names = names.Append(connectionName).Distinct(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<SchemaNode> roots = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new SchemaNode(name, name, SchemaNodeKind.Connection, new(name), true))
            .ToArray();
        return Task.FromResult(roots);
    }

    public async Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(SchemaNode parent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_connectionSessions.TryGetValue(parent.Path.Connection, out var database))
        {
            if (parent.Kind == SchemaNodeKind.Connection
                && _profiles.TryGetProfile(parent.Path.Connection, out ConnectionProfile profile))
            {
                return [
                    new SchemaNode($"{parent.Id}/{profile.Database}", profile.Database, SchemaNodeKind.Database,
                        new(parent.Path.Connection, profile.Database), true)];
            }

            return [];
        }

        // Netezza keeps the catalog in shared in-memory dictionaries. Filtering a
        // large dictionary is CPU work, not a UI operation; do it away from the
        // WinForms synchronization context before the view model renders batches.
        if (database is INetezza)
        {
            return await Task.Run<IReadOnlyList<SchemaNode>>(() => parent.Kind switch
            {
                SchemaNodeKind.Connection => MapDatabases(parent, database),
                SchemaNodeKind.Database => MapNetezzaCategories(parent),
                SchemaNodeKind.Schema => MapNetezzaObjects(parent),
                SchemaNodeKind.Table or SchemaNodeKind.View or SchemaNodeKind.Synonym => MapNetezzaColumns(parent),
                _ => []
            }, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<SchemaNode> result = parent.Kind switch
        {
            SchemaNodeKind.Connection => MapDatabases(parent, database),
            SchemaNodeKind.Database => MapSchemas(parent, database),
            SchemaNodeKind.Schema => MapObjects(parent, database),
            SchemaNodeKind.Table or SchemaNodeKind.View or SchemaNodeKind.Synonym => MapColumns(parent, database),
            _ => []
        };
        return result;
    }

    public async Task<SchemaSearchResult> SearchAsync(SchemaSearchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string query = request.Query?.Trim() ?? string.Empty;
        if (query.Length == 0) return new SchemaSearchResult([]);

        List<SchemaNode> matches = [];
        foreach (string connection in _profiles.ConnectionNames.Concat(_connectionSessions.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(request.Connection) && !connection.Equals(request.Connection, StringComparison.OrdinalIgnoreCase)) continue;
            if (!_connectionSessions.TryGetValue(connection, out var database)) continue;
            if (database is INetezza)
            {
                SchemaSearchResult netezzaResult = await Task.Run(
                    () => SearchNetezzaCatalog(
                        connection,
                        query,
                        request.IncludeColumns,
                        request.MaxResults,
                        _schemaTables.TablesByConnection.TryGetValue(connection, out var tables) ? tables : null,
                        _databaseRuntimeContext.DatabaseDictionary.TryGetValue(connection, out var databases) ? databases : null,
                        _databaseRuntimeContext.ColumnTablesDictionary.TryGetValue(connection, out var columns) ? columns : null),
                    cancellationToken).ConfigureAwait(false);
                matches.AddRange(netezzaResult.Nodes);
                if (netezzaResult.IsTruncated || matches.Count >= request.MaxResults)
                    return new SchemaSearchResult(matches.Take(request.MaxResults).ToArray(), true);
                continue;
            }

            foreach (var schema in database.objectInSchema)
            {
                foreach (var item in schema.Value)
                {
                    if (!item.Key.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                    matches.Add(new SchemaNode(
                        $"{connection}/{schema.Key}/{item.Key}", item.Key,
                        LegacySchemaTypeMapper.Map(item.Value), new(connection, database.DefaultDatabaseName, schema.Key, item.Key), false,
                        ProviderKind: item.Value.ToString()));
                    if (matches.Count >= request.MaxResults)
                        return new SchemaSearchResult(matches.ToArray(), true);
                }
            }
        }

        return new SchemaSearchResult(matches.ToArray());
    }

    internal static SchemaSearchResult SearchNetezzaCatalog(
        string connection,
        string query,
        bool includeColumns,
        int maxResults,
        IReadOnlyDictionary<int, NetezzaTableInfo>? tables,
        IReadOnlyDictionary<int, DatabaseInfo>? databases,
        IReadOnlyList<NetezzaColumnInfoRow>? columns)
    {
        if (tables is null || databases is null || maxResults <= 0)
            return new SchemaSearchResult([]);

        var matches = new List<SchemaNode>(Math.Min(maxResults, tables.Count));
        bool truncated = false;
        foreach ((int objectId, NetezzaTableInfo table) in tables.OrderBy(pair => pair.Value.TABLE_NAME, StringComparer.OrdinalIgnoreCase))
        {
            bool matched = table.TABLE_NAME.Contains(query, StringComparison.OrdinalIgnoreCase)
                || table.TABLE_DESC?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
            if (!matched && includeColumns && columns is not null)
            {
                int lastColumn = table.FIRST_COLUMN_ID + table.COLUMN_COUNT;
                for (int columnId = table.FIRST_COLUMN_ID; columnId < lastColumn && columnId < columns.Count; columnId++)
                {
                    if (columnId < 0 || columns[columnId].TABLE_ID != objectId)
                        continue;
                    NetezzaColumnInfoRow column = columns[columnId];
                    if (column.COLUMN_NAME.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || column.COLUMN_DESCRIPTION?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        matched = true;
                        break;
                    }
                }
            }

            if (!matched || !databases.TryGetValue(table.DATABASE_ID, out DatabaseInfo? database))
                continue;
            if (matches.Count == maxResults)
            {
                truncated = true;
                break;
            }

            string category = GetNetezzaCategory(table.TABLE_KIND);
            matches.Add(new SchemaNode(
                $"{connection}/{database.DatabaseName}/{category}/{objectId}",
                table.TABLE_NAME,
                LegacySchemaTypeMapper.Map(table.TABLE_KIND),
                new SchemaPath(connection, database.DatabaseName, category, table.TABLE_NAME),
                table.TABLE_KIND is TypeInDatabase.table or TypeInDatabase.view or TypeInDatabase.synonym,
                LegacyObjectId: objectId,
                ProviderKind: table.TABLE_KIND.ToString(),
                DisplayName: table.TABLE_NAME,
                Description: table.TABLE_DESC,
                Owner: table.TABLE_OWNER));
        }

        return new SchemaSearchResult(matches, truncated);
    }

    private static string GetNetezzaCategory(TypeInDatabase kind) => kind switch
    {
        TypeInDatabase.table => "Tables",
        TypeInDatabase.thisExternal => "External Tables",
        TypeInDatabase.view => "Views",
        TypeInDatabase.procedure => "Procedures",
        TypeInDatabase.sequence => "Sequences",
        TypeInDatabase.function => "Functions",
        TypeInDatabase.synonym => "Synonyms",
        TypeInDatabase.thisAggregate => "Aggregate",
        _ => kind.ToString()
    };

    public Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(LegacySqlReferenceParser.Parse(sql));
    }

    public Task<SqlOutline> GetOutlineAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SqlOutlineParser.Parse(sql));
    }

    public async Task RefreshAsync(
        string? connectionName = null,
        CancellationToken cancellationToken = default,
        SchemaRefreshRequest? request = null)
    {
        SchemaRefreshRequest effective = request ?? new SchemaRefreshRequest();
        IEnumerable<string> names = string.IsNullOrWhiteSpace(connectionName)
            ? _connectionSessions.Keys
            : [connectionName];
        foreach (string name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_connectionSessions.TryGetValue(name, out var database)) continue;
            cancellationToken.ThrowIfCancellationRequested();

            // Single-flight per connection. The in-flight work must NOT be tied to a
            // caller's CT: rapid UI refresh cancels the waiter, and a shared cancelled
            // task was poisoning later waiters with TaskCanceledException (app crash)
            // while finally{TryRemove} allowed overlapping DownloadSchemaNetezza runs
            // ("Collection was modified").
            Task refresh = _refreshes.GetOrAdd(name, _ => RunRefreshAndClearAsync(name, database, effective));
            try
            {
                await refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
    }

    public async Task<bool> AttachDatabaseAsync(
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("A connection name is required.", nameof(connectionName));
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("A database name is required.", nameof(databaseName));
        if (!_connectionSessions.TryGetValue(connectionName, out var database) || database is not INetezza netezza)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        bool success = await netezza.DownloadOneDb(connectionName, databaseName)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!success)
            return false;

        ClearConnectionSchemaCaches(connectionName);
        string? userName = _profiles.TryGetProfile(connectionName, out ConnectionProfile profile)
            ? profile.UserName
            : null;
        NetezzaHelpers.InitializeConnectionSchemaData(
            _databaseRuntimeContext,
            _connectionSessions,
            _schemaTables,
            userName,
            connectionName);
        return true;
    }

    private async Task RunRefreshAndClearAsync(string connectionName, IGeneralDb database, SchemaRefreshRequest request)
    {
        try
        {
            await RefreshDatabaseAsync(connectionName, database, request, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _refreshes.TryRemove(connectionName, out _);
        }
    }

    private async Task RefreshDatabaseAsync(
        string connectionName,
        IGeneralDb database,
        SchemaRefreshRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Run(database.InitDb, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // Netezza keeps its object catalog in the provider-side dictionaries and
        // fills those dictionaries through DownloadSchemaNetezza rather than
        // InitDb. Run that provider operation from the repository refresh so the
        // MVVM tree can load schemas/tables without relying on a hidden TreeView.
        if (database is INetezza netezza)
        {
            NetezzaRefreshMode mode = MapMode(request.Mode);
            List<string>? dbsToRefresh = request.DatabasesToRefresh is { Count: > 0 }
                ? request.DatabasesToRefresh.ToList()
                : null;

            bool downloaded = await netezza.DownloadSchemaNetezza(
                connectionName,
                mode,
                dbsToRefresh,
                loadSources: request.LoadSources,
                showInUiExtra: null).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!downloaded)
                throw new InvalidOperationException($"Failed to refresh schema for connection '{connectionName}'.");

            ClearConnectionSchemaCaches(connectionName);
            string? userName = _profiles.TryGetProfile(connectionName, out ConnectionProfile profile)
                ? profile.UserName
                : null;
            NetezzaHelpers.InitializeConnectionSchemaData(
                _databaseRuntimeContext,
                _connectionSessions,
                _schemaTables,
                userName,
                connectionName);
        }
    }

    private void ClearConnectionSchemaCaches(string connectionName)
    {
        _schemaTables.ClearConnection(connectionName);
        _completionRuntime.ClearSchemaLookup(connectionName);
        _completionRuntime.ClearDatabaseOwners(connectionName);
    }

    private static NetezzaRefreshMode MapMode(SchemaRefreshMode mode) => mode switch
    {
        SchemaRefreshMode.Full => NetezzaRefreshMode.full,
        SchemaRefreshMode.PartialOnlyTables => NetezzaRefreshMode.partialOnlyTables,
        _ => NetezzaRefreshMode.partial
    };

    private IReadOnlyList<SchemaNode> MapDatabases(SchemaNode parent, IGeneralDb database)
    {
        if (database is INetezza
            && _databaseRuntimeContext.DatabaseDictionary.TryGetValue(parent.Path.Connection, out var providerDatabases)
            && providerDatabases.Count > 0)
        {
            // Snapshot Values before LINQ to avoid "Collection was modified" when
            // Netezza.DownloadSchemaNetezza replaces or populates the dictionary
            // from a background thread (Task.Run).
            DatabaseInfo[] dbValues = providerDatabases.Values.ToArray();
            return dbValues
                .Select(info => info.DatabaseName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new SchemaNode($"{parent.Id}/{name}", name, SchemaNodeKind.Database,
                    new(parent.Path.Connection, name), true))
                .ToArray();
        }

        IEnumerable<string> names = (database.DatabaseList ?? [])
            .Append(database.DefaultDatabaseName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        // Some providers do not populate DatabaseList/DefaultDatabaseName until
        // their first schema refresh. Keep the logged-in profile database visible
        // while that refresh is still in progress instead of rendering a dead root.
        if (!names.Any()
            && _profiles.TryGetProfile(parent.Path.Connection, out ConnectionProfile profile)
            && !string.IsNullOrWhiteSpace(profile.Database))
        {
            names = [profile.Database];
        }

        return names.Select(name => new SchemaNode($"{parent.Id}/{name}", name, SchemaNodeKind.Database,
            new(parent.Path.Connection, name), true)).ToArray();
    }

    private static IReadOnlyList<SchemaNode> MapSchemas(SchemaNode parent, IGeneralDb database)
    {
        return database.objectInSchema.Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new SchemaNode($"{parent.Id}/{name}", name, SchemaNodeKind.Schema,
                new(parent.Path.Connection, parent.Path.Database, name), true)).ToArray();
    }

    private static IReadOnlyList<SchemaNode> MapObjects(SchemaNode parent, IGeneralDb database)
    {
        if (!database.objectInSchema.TryGetValue(parent.Name, out var objects)) return [];
        return objects.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new SchemaNode($"{parent.Id}/{item.Key}", item.Key, LegacySchemaTypeMapper.Map(item.Value),
                new(parent.Path.Connection, parent.Path.Database, parent.Path.Schema, item.Key),
                item.Value is TypeInDatabase.table or TypeInDatabase.view or TypeInDatabase.synonym,
            ProviderKind: item.Value.ToString())).ToArray();
    }

    private IReadOnlyList<SchemaNode> MapNetezzaCategories(SchemaNode parent)
    {
        string[] categories = ["Tables", "External Tables", "Views", "Procedures", "Sequences", "Functions", "Synonyms", "Aggregate"];
        return categories.Select(category => new SchemaNode(
            $"{parent.Id}/{category}", category, SchemaNodeKind.Schema,
            new(parent.Path.Connection, parent.Path.Database, category), true)).ToArray();
    }

    private IReadOnlyList<SchemaNode> MapNetezzaObjects(SchemaNode parent)
    {
        if (!_schemaTables.TablesByConnection.TryGetValue(parent.Path.Connection, out var tables)
            || !TryGetNetezzaDatabaseId(parent.Path.Connection, parent.Path.Database, out int databaseId))
            return [];

        string category = parent.Name;
        return tables
            .Where(pair => pair.Value.DATABASE_ID == databaseId && MatchesNetezzaCategory(category, pair.Value.TABLE_KIND))
            .OrderBy(pair => pair.Value.TABLE_NAME, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new SchemaNode(
                $"{parent.Id}/{pair.Value.TABLE_NAME}",
                pair.Value.TABLE_NAME,
                LegacySchemaTypeMapper.Map(pair.Value.TABLE_KIND),
                new(parent.Path.Connection, parent.Path.Database, category, pair.Value.TABLE_NAME),
                pair.Value.TABLE_KIND is TypeInDatabase.table or TypeInDatabase.view or TypeInDatabase.thisExternal,
                LegacyObjectId: pair.Key,
                ProviderKind: pair.Value.TABLE_KIND.ToString(),
                DisplayName: _databaseRuntimeContext.Config.DontShowOwner
                    ? pair.Value.TABLE_NAME
                    : $"{pair.Value.TABLE_OWNER}.{pair.Value.TABLE_NAME}"))
            .ToArray();
    }

    private IReadOnlyList<SchemaNode> MapNetezzaColumns(SchemaNode parent)
    {
        if (parent.LegacyObjectId is not int tableId
            || !_schemaTables.TablesByConnection.TryGetValue(parent.Path.Connection, out var tables)
            || !tables.TryGetValue(tableId, out var table)
            || !_databaseRuntimeContext.ColumnTablesDictionary.TryGetValue(parent.Path.Connection, out var columns))
            return [];

        int firstColumnId = table.FIRST_COLUMN_ID;
        int lastExclusive = firstColumnId + table.COLUMN_COUNT;
        var result = new List<SchemaNode>(table.COLUMN_COUNT);
        for (int columnId = firstColumnId; columnId < lastExclusive; columnId++)
        {
            if (columnId < 0 || columnId >= columns.Count)
                continue;

            var column = columns[columnId];
            // The dictionary is indexed by the legacy column id. Guard against a
            // partially refreshed catalog instead of showing a fake child node.
            if (column.TABLE_ID != tableId)
                continue;

            string nullable = column.IS_NULLABLE ? string.Empty : " NOT NULL";
            result.Add(new SchemaNode(
                $"{parent.Id}/{column.COLUMN_NAME}",
                column.COLUMN_NAME,
                SchemaNodeKind.Column,
                new(parent.Path.Connection, parent.Path.Database, parent.Path.Schema, parent.Name),
                false,
                LegacyObjectId: columnId,
                ProviderKind: column.DATA_TYPE,
                DisplayName: $"{column.COLUMN_NAME} - {column.DATA_TYPE}{nullable}"));
        }
        return result;
    }

    private bool TryGetNetezzaDatabaseId(string connectionName, string? databaseName, out int databaseId)
    {
        databaseId = default;
        if (string.IsNullOrWhiteSpace(databaseName)
            || !_databaseRuntimeContext.DatabaseDictionary.TryGetValue(connectionName, out var databases))
            return false;

        // Snapshot to avoid "Collection was modified" during background schema download
        foreach (var pair in databases.ToArray())
        {
            if (pair.Value.DatabaseName.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
            {
                databaseId = pair.Key;
                return true;
            }
        }
        return false;
    }

    private static bool MatchesNetezzaCategory(string category, TypeInDatabase kind) => category switch
    {
        "Tables" => kind == TypeInDatabase.table,
        "External Tables" => kind == TypeInDatabase.thisExternal,
        "Views" => kind == TypeInDatabase.view,
        "Procedures" => kind == TypeInDatabase.procedure,
        "Sequences" => kind == TypeInDatabase.sequence,
        "Functions" => kind == TypeInDatabase.function,
        "Synonyms" => kind == TypeInDatabase.synonym,
        "Aggregate" => kind == TypeInDatabase.thisAggregate,
        _ => false
    };

    private static IReadOnlyList<SchemaNode> MapColumns(SchemaNode parent, IGeneralDb database)
    {
        string[] columns = database.GetColumns(parent.Path.Database ?? database.DefaultDatabaseName, parent.Path.Schema ?? string.Empty, parent.Name);
        return columns.Select(column => new SchemaNode($"{parent.Id}/{column}", column, SchemaNodeKind.Column,
            new(parent.Path.Connection, parent.Path.Database, parent.Path.Schema, parent.Name), false)).ToArray();
    }
}
