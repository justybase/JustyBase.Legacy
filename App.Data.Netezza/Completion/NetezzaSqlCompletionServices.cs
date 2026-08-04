using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Common.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Caching;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Dialects;
using JustyBase.NetezzaSqlParser.Ast;
using JustyBase.NetezzaSqlParser.Visitor;

namespace AppBase.Data.Completion;

/// <summary>
/// Shared SQL completion infrastructure (schema, parse cache) for all SQL editor tabs.
/// Each editor tab gets its own <see cref="NzCompletionEngine"/> with a unique document URI.
/// </summary>
public sealed class NetezzaSqlCompletionServices
{
    private readonly INetezzaSchemaTableCatalog _schemaTables;
    private string _schemaSyncedForConnection;

    public InMemorySchemaProvider SchemaProvider { get; } = new();
    public NetezzaSchemaSnapshot MetadataSnapshot { get; private set; } = NetezzaSchemaSnapshot.Empty;
    public DocumentParsingCoordinator ParsingCoordinator { get; } = new();
    public event Action? SchemaInvalidated;

    public NetezzaSqlCompletionServices(INetezzaSchemaTableCatalog schemaTables)
    {
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
    }

    public NzCompletionEngine CreateEngine(string documentUri, SqlDialect dialect = SqlDialect.Netezza)
    {
        var engine = new NzCompletionEngine(
            SchemaProvider,
            ParsingCoordinator,
            DialectRuntime.AuthoringCatalog(dialect),
            dialect);
        engine.SetDocumentUri(documentUri);
        return engine;
    }

    public void EnsureSchemaForConnection(INetezzaCompletionContext completionContext, string connectionName)
    {
        if (completionContext is null || string.IsNullOrEmpty(connectionName))
            return;

        if (string.Equals(_schemaSyncedForConnection, connectionName, StringComparison.OrdinalIgnoreCase)
            && SchemaProvider.HasTables())
            return;

        MetadataSnapshot = LegacySchemaSync.SyncConnection(SchemaProvider, completionContext, _schemaTables, connectionName);
        _schemaSyncedForConnection = connectionName;
    }

    /// <summary>
    /// Projects the already-loaded DB2 catalog into the same parser provider
    /// used by Netezza. Columns are part of the provider snapshot so the shared
    /// CompletionAliasResolver can resolve A. for both schema.table and
    /// database.schema.table references without depending on a host workaround.
    /// </summary>
    public void EnsureDb2Schema(IGeneralDb database, string connectionName, string databaseName)
    {
        if (database is null || string.IsNullOrWhiteSpace(connectionName))
            return;

        string syncKey = $"DB2:{connectionName}:{databaseName}";
        if (string.Equals(_schemaSyncedForConnection, syncKey, StringComparison.OrdinalIgnoreCase)
            && SchemaProvider.HasTables())
            return;

        SchemaProvider.Clear();
        foreach ((string schema, Dictionary<string, TypeInDatabase> objects) in database.objectInSchema)
        {
            foreach ((string name, TypeInDatabase kind) in objects)
            {
                if (kind is not (TypeInDatabase.table or TypeInDatabase.view
                    or TypeInDatabase.synonym or TypeInDatabase.db2alias or TypeInDatabase.db2nickname))
                    continue;

                ColumnInfo[] columns;
                try
                {
                    columns = database.GetColumns(databaseName, schema, name)
                        .Where(column => !string.IsNullOrWhiteSpace(column))
                        .Select(column => new ColumnInfo(column))
                        .ToArray();
                }
                catch
                {
                    // Keep the object visible when one object cannot expose
                    // columns. The live fallback can still serve that object.
                    columns = [];
                }

                SchemaProvider.AddTable(new TableInfo(
                    name,
                    schema,
                    databaseName,
                    Columns: columns,
                    IsView: kind == TypeInDatabase.view));
            }
        }

        SchemaProvider.BumpMetadataEpoch();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        _schemaSyncedForConnection = syncKey;
    }

    public void InvalidateSchema()
    {
        _schemaSyncedForConnection = null;
        SchemaProvider.Clear();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        SchemaProvider.BumpMetadataEpoch();
        SchemaInvalidated?.Invoke();
    }
}
