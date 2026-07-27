using AppBase.Common.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Login;
using JustyBaseLegacy.UI.Sql;

namespace JustyBaseLegacy.UI.Schema;

/// <summary>
/// Synchronizes SQL editor connection/database combos with the refreshed schema catalog.
/// </summary>
public sealed class EditorCatalogProjection : IDisposable
{
    private readonly IEditorCatalogState _catalog;
    private readonly IDatabaseRuntimeContext _runtime;
    private readonly IMessenger _messenger;

    public EditorCatalogProjection(
        IEditorCatalogState catalog,
        IDatabaseRuntimeContext runtime,
        IMessenger messenger)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _messenger.Register<SchemaRefreshedMessage>(this, (_, message) => SyncConnection(message.ConnectionName));
    }

    public void SeedFromProfiles(IConnectionProfileCatalog profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        foreach (string connectionName in profiles.ConnectionNames)
            _catalog.AddConnection(connectionName);
    }

    private void SyncConnection(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            return;

        _catalog.AddConnection(connectionName);
        if (_runtime.DatabaseDictionary.TryGetValue(connectionName, out var databases) && databases.Count > 0)
        {
            string[] names = databases.Values
                .Select(item => item.DatabaseName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (names.Length > 0)
                _catalog.ReplaceDatabases(connectionName, names);
        }
    }

    public void Dispose() => _messenger.UnregisterAll(this);
}
