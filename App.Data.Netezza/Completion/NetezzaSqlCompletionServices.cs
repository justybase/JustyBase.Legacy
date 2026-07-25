using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Caching;
using JustyBase.NetezzaSqlParser.Completion;
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

    public NzCompletionEngine CreateEngine(string documentUri)
    {
        var engine = new NzCompletionEngine(SchemaProvider, ParsingCoordinator);
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

    public void InvalidateSchema()
    {
        _schemaSyncedForConnection = null;
        SchemaProvider.Clear();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        SchemaProvider.BumpMetadataEpoch();
        SchemaInvalidated?.Invoke();
    }
}
