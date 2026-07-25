using JustData.Application.Editor;
using System.Data;
using System.Data.Common;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Owns the short lived ADO resources used by an SQL document.  The registry is
/// deliberately keyed by document id, rather than a WinForms tab or a
/// connection hash: a tab can be re-parented by DockSuite while a command is
/// still running.
/// </summary>
public interface ISqlExecutionSessionRegistry
{
    bool TryStart(EditorDocumentId documentId, string connectionName, out ISqlExecutionSession session);
    bool TryGet(EditorDocumentId documentId, out ISqlExecutionSession? session);
    Task CancelAsync(EditorDocumentId documentId);
    bool TryConsumeCancellation(EditorDocumentId documentId);
    void Complete(EditorDocumentId documentId);
    void Cleanup(EditorDocumentId documentId);
}

public interface ISqlExecutionSession : IDisposable
{
    EditorDocumentId DocumentId { get; }
    string ConnectionName { get; }
    bool IsCancelling { get; }
    void SetConnection(DbConnection connection, bool ownsConnection = true);
    void SetCommand(DbCommand command, bool ownsCommand = true);
    void SetProviderAbort(Func<Task>? abortAsync);
}

public sealed class SqlExecutionSessionRegistry : ISqlExecutionSessionRegistry, IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<EditorDocumentId, SqlExecutionSession> _sessions = [];
    private readonly HashSet<EditorDocumentId> _cancelledDocuments = [];

    public bool TryStart(EditorDocumentId documentId, string connectionName, out ISqlExecutionSession session)
    {
        lock (_sync)
        {
            if (_sessions.ContainsKey(documentId))
            {
                session = null!;
                return false;
            }

            var created = new SqlExecutionSession(documentId, connectionName);
            _cancelledDocuments.Remove(documentId);
            _sessions.Add(documentId, created);
            session = created;
            return true;
        }
    }

    public bool TryGet(EditorDocumentId documentId, out ISqlExecutionSession? session)
    {
        lock (_sync)
        {
            bool found = _sessions.TryGetValue(documentId, out SqlExecutionSession? value);
            session = value;
            return found;
        }
    }

    public async Task CancelAsync(EditorDocumentId documentId)
    {
        SqlExecutionSession? session;
        lock (_sync)
            _sessions.TryGetValue(documentId, out session);
        if (session is not null)
        {
            lock (_sync)
                _cancelledDocuments.Add(documentId);
            await session.CancelAsync().ConfigureAwait(false);
        }
    }

    public bool TryConsumeCancellation(EditorDocumentId documentId)
    {
        lock (_sync)
            return _cancelledDocuments.Remove(documentId);
    }

    public void Complete(EditorDocumentId documentId) => Remove(documentId);

    public void Cleanup(EditorDocumentId documentId) => Remove(documentId);

    private void Remove(EditorDocumentId documentId)
    {
        SqlExecutionSession? session;
        lock (_sync)
        {
            if (!_sessions.Remove(documentId, out session))
                return;
        }
        session.Dispose();
    }

    public void Dispose()
    {
        SqlExecutionSession[] sessions;
        lock (_sync)
        {
            sessions = _sessions.Values.ToArray();
            _sessions.Clear();
        }
        foreach (SqlExecutionSession session in sessions)
            session.Dispose();
    }

    private sealed class SqlExecutionSession(EditorDocumentId documentId, string connectionName) : ISqlExecutionSession
    {
        private readonly object _sync = new();
        private DbConnection? _connection;
        private DbCommand? _command;
        private Func<Task>? _providerAbort;
        private bool _ownsConnection;
        private bool _ownsCommand;
        private bool _disposed;

        public EditorDocumentId DocumentId { get; } = documentId;
        public string ConnectionName { get; } = connectionName;
        public bool IsCancelling { get; private set; }

        public void SetConnection(DbConnection connection, bool ownsConnection = true)
        {
            ArgumentNullException.ThrowIfNull(connection);
            lock (_sync) { ThrowIfDisposed(); _connection = connection; _ownsConnection = ownsConnection; }
        }

        public void SetCommand(DbCommand command, bool ownsCommand = true)
        {
            ArgumentNullException.ThrowIfNull(command);
            lock (_sync) { ThrowIfDisposed(); _command = command; _ownsCommand = ownsCommand; }
        }

        public void SetProviderAbort(Func<Task>? abortAsync)
        {
            lock (_sync) { ThrowIfDisposed(); _providerAbort = abortAsync; }
        }

        public async Task CancelAsync()
        {
            DbCommand? command;
            Func<Task>? abort;
            lock (_sync)
            {
                if (_disposed || IsCancelling)
                    return;
                IsCancelling = true;
                command = _command;
                abort = _providerAbort;
            }
            try { command?.Cancel(); }
            catch (Exception) { /* provider cancellation is best effort */ }
            if (abort is not null)
            {
                try { await abort().ConfigureAwait(false); }
                catch (Exception) { /* command cancellation remains authoritative */ }
            }
        }

        public void Dispose()
        {
            DbCommand? command;
            DbConnection? connection;
            bool ownsCommand;
            bool ownsConnection;
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;
                command = _command; connection = _connection;
                ownsCommand = _ownsCommand; ownsConnection = _ownsConnection;
                _command = null; _connection = null; _providerAbort = null;
            }
            if (ownsCommand) command?.Dispose();
            if (ownsConnection)
            {
                try { if (connection?.State != ConnectionState.Closed) connection?.Close(); }
                finally { connection?.Dispose(); }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SqlExecutionSession));
        }
    }
}
