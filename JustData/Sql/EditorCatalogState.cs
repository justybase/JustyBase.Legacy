namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Immutable projection consumed by editor controls. The state belongs to a
/// scoped workspace, never to a static WinForms control.
/// </summary>
public sealed record EditorCatalogSnapshot(
    IReadOnlyList<string> Connections,
    IReadOnlyDictionary<string, IReadOnlyList<string>> DatabasesByConnection)
{
    public static EditorCatalogSnapshot Empty { get; } = new(
        Array.Empty<string>(),
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    public IReadOnlyList<string> DatabasesFor(string? connectionName) =>
        !string.IsNullOrWhiteSpace(connectionName)
        && DatabasesByConnection.TryGetValue(connectionName, out IReadOnlyList<string>? databases)
            ? databases
            : Array.Empty<string>();
}

public interface IEditorCatalogState
{
    EditorCatalogSnapshot Snapshot { get; }
    event Action<EditorCatalogSnapshot>? Changed;
    void AddConnection(string connectionName);
    void RemoveConnection(string connectionName);
    void ReplaceDatabases(string connectionName, IEnumerable<string> databases);
    void AddDatabase(string connectionName, string databaseName);
}

public sealed class EditorCatalogState : IEditorCatalogState
{
    private readonly object _sync = new();
    private readonly List<string> _connections = [];
    private readonly Dictionary<string, List<string>> _databases = new(StringComparer.OrdinalIgnoreCase);
    private EditorCatalogSnapshot _snapshot = EditorCatalogSnapshot.Empty;

    public EditorCatalogSnapshot Snapshot
    {
        get { lock (_sync) return _snapshot; }
    }

    public event Action<EditorCatalogSnapshot>? Changed;

    public void AddConnection(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName)) return;
        PublishIfChanged(() =>
        {
            if (_connections.Contains(connectionName, StringComparer.OrdinalIgnoreCase)) return false;
            _connections.Add(connectionName);
            return true;
        });
    }

    public void RemoveConnection(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName)) return;
        PublishIfChanged(() =>
        {
            bool removed = _connections.RemoveAll(item =>
                string.Equals(item, connectionName, StringComparison.OrdinalIgnoreCase)) > 0;
            removed |= _databases.Remove(connectionName);
            return removed;
        });
    }

    public void ReplaceDatabases(string connectionName, IEnumerable<string> databases)
    {
        if (string.IsNullOrWhiteSpace(connectionName)) return;
        string[] replacement = Normalize(databases).ToArray();
        PublishIfChanged(() =>
        {
            if (_databases.TryGetValue(connectionName, out List<string>? current)
                && current.SequenceEqual(replacement, StringComparer.OrdinalIgnoreCase)) return false;
            _databases[connectionName] = replacement.ToList();
            return true;
        });
    }

    public void AddDatabase(string connectionName, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(connectionName) || string.IsNullOrWhiteSpace(databaseName)) return;
        PublishIfChanged(() =>
        {
            if (!_databases.TryGetValue(connectionName, out List<string>? databases))
                _databases[connectionName] = databases = [];
            if (databases.Contains(databaseName, StringComparer.OrdinalIgnoreCase)) return false;
            databases.Add(databaseName);
            return true;
        });
    }

    private void PublishIfChanged(Func<bool> mutate)
    {
        EditorCatalogSnapshot? snapshot = null;
        lock (_sync)
        {
            if (!mutate()) return;
            _snapshot = snapshot = CreateSnapshot();
        }
        Changed?.Invoke(snapshot);
    }

    private EditorCatalogSnapshot CreateSnapshot() => new(
        _connections.ToArray(),
        _databases.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<string> Normalize(IEnumerable<string>? values) => (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase);
}
