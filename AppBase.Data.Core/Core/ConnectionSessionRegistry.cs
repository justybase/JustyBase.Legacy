using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace AppBase.Data.Core.Core;

/// <summary>
/// Thread-safe registry for database sessions keyed by the configured connection name.
/// </summary>
public interface IConnectionSessionRegistry : IReadOnlyDictionary<string, IGeneralDb>
{
    new IGeneralDb this[string connectionName] { get; set; }

    void Set(string connectionName, IGeneralDb database);

    bool Remove(string connectionName);

    void Clear();
}

/// <summary>
/// Compatibility-friendly replacement for the process-wide mutable dictionary used by
/// the legacy UI. Reads are snapshot-safe and writes are serialized, so a refresh cannot
/// observe a partially-mutated dictionary.
/// </summary>
public sealed class ConnectionSessionRegistry : IConnectionSessionRegistry
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, IGeneralDb> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public IGeneralDb this[string connectionName]
    {
        get
        {
            ValidateConnectionName(connectionName);

            lock (_syncRoot)
            {
                return _sessions[connectionName];
            }
        }
        set => Set(connectionName, value);
    }

    public IEnumerable<string> Keys => Snapshot().Select(pair => pair.Key);

    public IEnumerable<IGeneralDb> Values => Snapshot().Select(pair => pair.Value);

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _sessions.Count;
            }
        }
    }

    public void Set(string connectionName, IGeneralDb database)
    {
        ValidateConnectionName(connectionName);
        ArgumentNullException.ThrowIfNull(database);

        lock (_syncRoot)
        {
            _sessions[connectionName] = database;
        }
    }

    public bool TryGetValue(string connectionName, [MaybeNullWhen(false)] out IGeneralDb database)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            database = null;
            return false;
        }

        lock (_syncRoot)
        {
            return _sessions.TryGetValue(connectionName, out database);
        }
    }

    public bool ContainsKey(string connectionName)
    {
        return !string.IsNullOrWhiteSpace(connectionName)
            && TryGetValue(connectionName, out _);
    }

    public bool Remove(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _sessions.Remove(connectionName);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _sessions.Clear();
        }
    }

    public IEnumerator<KeyValuePair<string, IGeneralDb>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, IGeneralDb>>)Snapshot()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private KeyValuePair<string, IGeneralDb>[] Snapshot()
    {
        lock (_syncRoot)
        {
            return _sessions.ToArray();
        }
    }

    private static void ValidateConnectionName(string connectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
    }
}
