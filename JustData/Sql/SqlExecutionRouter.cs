using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using SpreadSheetTasks;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Narrow boundary between provider execution and WinForms presentation.  An
/// engine depends on this boundary, never on <c>BaseWindow</c> itself.
/// </summary>
public interface ISqlExecutionDocumentPresenter
{
    IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default);

    void Cancel(EditorDocumentId documentId, string connectionName);
}

/// <summary>Document lifecycle adapter used by the SQL engines.</summary>
public sealed class SqlExecutionEngineContext
{
    private readonly object _sync = new();
    private ISqlExecutionDocumentPresenter? _presenter;

    public void AttachPresenter(ISqlExecutionDocumentPresenter presenter)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        lock (_sync)
            _presenter = presenter;
    }

    public void DetachPresenter(ISqlExecutionDocumentPresenter presenter)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_presenter, presenter))
                _presenter = null;
        }
    }

    internal IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ISqlExecutionDocumentPresenter? presenter;
        lock (_sync)
            presenter = _presenter;

        return presenter?.ExecuteAsync(request, cancellationToken)
            ?? MissingPresenter(request);
    }

    internal void Cancel(EditorDocumentId documentId, string connectionName)
    {
        ISqlExecutionDocumentPresenter? presenter;
        lock (_sync)
            presenter = _presenter;
        presenter?.Cancel(documentId, connectionName);
    }

    private static async IAsyncEnumerable<SqlExecutionEvent> MissingPresenter(SqlExecutionRequest request)
    {
        yield return SqlExecutionEvent.Completed(
            request.DocumentId,
            SqlExecutionOutcome.Blocked,
            "The SQL document presenter is not available.");
        await Task.CompletedTask;
    }
}

public interface ISqlExecutionEngine
{
    bool CanExecute(string driverName);

    IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Engine slice for DB2, Oracle, Postgres, SQL Server, SQLite, MySQL and
/// OLEDB.  The reader loop remains owned by the presenter while results are
/// streamed through the application event contract.
/// </summary>
public sealed class GeneralSqlExecutionEngine : ISqlExecutionEngine
{
    private readonly SqlExecutionEngineContext? _legacyContext;
    private readonly ISqlExecutionSessionRegistry? _sessions;
    private readonly IImportExportTasks? _exportTasks;
    private readonly IConnectionSessionRegistry? _databaseSessions;

    // Kept for composition tests while the Netezza engine is migrated.  New
    // production composition uses the provider constructor below.
    public GeneralSqlExecutionEngine(SqlExecutionEngineContext context) => _legacyContext = context;

    public GeneralSqlExecutionEngine(
        ISqlExecutionSessionRegistry sessions,
        IImportExportTasks? exportTasks = null,
        IConnectionSessionRegistry? databaseSessions = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _exportTasks = exportTasks;
        _databaseSessions = databaseSessions ?? IGeneralDbService.ConnectionSessions;
    }

    internal bool EmitsStatementEvents => _sessions is not null;
    private static readonly HashSet<string> SupportedDrivers = new(StringComparer.OrdinalIgnoreCase)
    {
        "DB2", "Microsoft.ACE.OLEDB.12.0", "Oracle", "Postgres", "MySql",
        "SQLite", "MsSqlStd", "MsSqlTrusted"
    };

    public bool CanExecute(string driverName) => SupportedDrivers.Contains(driverName);

    public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_sessions is null)
        {
            if (_legacyContext is null)
                throw new InvalidOperationException("No SQL execution backend is configured.");
            await foreach (SqlExecutionEvent item in _legacyContext.ExecuteAsync(request, cancellationToken)
                .WithCancellation(cancellationToken))
                yield return item;
            yield break;
        }

        if (_databaseSessions is null
            || !_databaseSessions.TryGetValue(request.ConnectionName, out var database))
        {
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Blocked,
                "The selected database connection is not available.");
            yield break;
        }
        if (!_sessions.TryStart(request.DocumentId, request.ConnectionName, out ISqlExecutionSession session))
        {
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Blocked,
                "A SQL command is already running for this document.");
            yield break;
        }

        try
        {
            IReadOnlyList<string> batches = BuildBatches(request.SqlText, request.Mode, request.OutputMode);
            if (batches.Count == 0)
            {
                yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Blocked, "Nothing to execute.");
                yield break;
            }

            for (int statementIndex = 0; statementIndex < batches.Count; statementIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string sql = batches[statementIndex];
                yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementStarted, request.DocumentId)
                { StatementIndex = statementIndex, StatementCount = batches.Count, StatementText = SqlSensitiveDataRedactor.Redact(sql) };

                using DbConnection connection = string.IsNullOrWhiteSpace(request.DatabaseName)
                    ? database.GetConnection()
                    : database.GetConnection(request.DatabaseName);
                session.SetConnection(connection, ownsConnection: false);
                session.SetProviderAbort(() => database.AbortAsync("x"));
                await OpenConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
                using DbCommand command = CreateCommand(connection, sql, request.CommandTimeoutSeconds ?? 0);
                session.SetCommand(command, ownsCommand: false);

                if (request.OutputMode == SqlOutputMode.LogOnly)
                {
                    int affected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
                    yield return new SqlExecutionEvent(SqlExecutionEventKind.AffectedRows, request.DocumentId)
                    { StatementIndex = statementIndex, AffectedRows = affected };
                }
                else
                {
                    using DbDataReader reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
                    if (request.OutputMode is SqlOutputMode.Xlsx or SqlOutputMode.Xlsb)
                    {
                        if (string.IsNullOrWhiteSpace(request.OutputPath))
                            throw new InvalidOperationException("An output path is required for spreadsheet export.");
                        await ExportXlsxAsync(reader, request.OutputPath, request.OutputMode == SqlOutputMode.Xlsb,
                            sql, cancellationToken).ConfigureAwait(false);
                        yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, request.DocumentId)
                        { StatementIndex = statementIndex, Message = $"Exported to {request.OutputPath}" };
                    }
                    else if (request.OutputMode == SqlOutputMode.Csv)
                    {
                        if (string.IsNullOrWhiteSpace(request.OutputPath))
                            throw new InvalidOperationException("An output path is required for CSV export.");
                        if (_exportTasks is null)
                            throw new InvalidOperationException("CSV export is not configured.");
                        await ExportCsvAsync(_exportTasks, reader, Encoding.UTF8, request.OutputPath, ',', Environment.NewLine,
                            progress => { }, cancellationToken: cancellationToken).ConfigureAwait(false);
                        yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, request.DocumentId)
                        { StatementIndex = statementIndex, Message = $"Exported to {request.OutputPath}" };
                    }
                    else
                    {
                        int resultNumber = 0;
                        do
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (reader.FieldCount <= 0) continue;
                            string resultSetId = $"{request.DocumentId}-{statementIndex}-{resultNumber++}";
                            ResultColumnDescriptor[] columns = Enumerable.Range(0, reader.FieldCount)
                                .Select(i => new ResultColumnDescriptor(i, reader.GetName(i), reader.GetDataTypeName(i)))
                                .ToArray();
                            yield return SqlExecutionEvent.Result(request.DocumentId,
                                new ResultSetDescriptor(resultSetId, $"Result {resultNumber}", columns, statementIndex));

                            var batch = new List<IReadOnlyList<object?>>(500);
                            long rows = 0;
                            long limit = request.RowLimit ?? long.MaxValue;
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                            object?[] row = new object?[reader.FieldCount];
                            for (int i = 0; i < row.Length; i++)
                                row[i] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
                            batch.Add(row); rows++;
                            if (batch.Count == 500)
                            {
                                yield return SqlExecutionEvent.RowsBatch(request.DocumentId, batch.ToArray(), statementIndex, resultSetId);
                                batch.Clear();
                            }
                            if (rows >= limit)
                            {
                                yield return new SqlExecutionEvent(SqlExecutionEventKind.Truncated, request.DocumentId)
                                { StatementIndex = statementIndex, ResultSetId = resultSetId, RowCount = rows, IsTruncated = true };
                                break;
                            }
                            }
                            if (batch.Count > 0)
                                yield return SqlExecutionEvent.RowsBatch(request.DocumentId, batch.ToArray(), statementIndex, resultSetId);
                        } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
                    }
                }
                yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementCompleted, request.DocumentId)
                { StatementIndex = statementIndex, StatementCount = batches.Count };
            }
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Success);
        }
        finally { _sessions.Complete(request.DocumentId); }
    }

    /// <summary>
    /// Preserves the legacy General DB batching rules in a provider/UI-neutral
    /// form. The ADO reader migration can therefore consume the same plan
    /// without needing an editor or a WinForms control.
    /// </summary>
    public static IReadOnlyList<string> BuildBatches(
        string sql,
        SqlExecutionMode mode,
        SqlOutputMode outputMode)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return [];

        bool splitStatements = mode is SqlExecutionMode.Selection or SqlExecutionMode.RunToCursor or SqlExecutionMode.Script
            && outputMode is SqlOutputMode.Grid or SqlOutputMode.LogOnly;
        if (!splitStatements)
            return sql.Length >= 3 ? [sql] : [];

        return sql.SqlSplitAdvanced(';')
            .Select(statement => statement.Trim())
            .Where(statement => statement.Length >= 2)
            .ToArray();
    }

    /// <summary>
    /// Provider-only reader loop used by the General DB slice. It deliberately
    /// knows nothing about grids, dialogs or DockSuite; callers decide how a
    /// row is presented and whether streaming should continue.
    /// </summary>
    public static Task<long> StreamRowsAsync(
        DbDataReader reader,
        Func<object?[], long, bool> onRow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(onRow);

        return Task.Run(() =>
        {
            long rowCount = 0;
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = new object?[reader.FieldCount];
                for (int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                    row[ordinal] = reader.GetValue(ordinal);

                rowCount++;
                if (!onRow(row, rowCount))
                    break;
            }

            return rowCount;
        }, cancellationToken);
    }

    public static DbCommand CreateCommand(DbConnection connection, string sql, int commandTimeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(sql);
        DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        return command;
    }

    public static Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    /// <summary>
    /// Some supported providers expose only the synchronous ADO surface. Keep
    /// those calls off the UI thread while still giving the execution session
    /// a cancellable observation point.
    /// </summary>
    public static Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return Task.Run(connection.Open, cancellationToken);
    }

    public static Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.Run(command.ExecuteReader, cancellationToken);
    }

    public static Task ExportXlsxAsync(
        DbDataReader reader,
        string outputPath,
        bool useXlsb,
        string executedSql,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(executedSql);
        return Task.Run(() =>
        {
            ExcelWriter writer = useXlsb
                ? new XlsbWriter(outputPath) { SuppressYear1000Dates = true }
                : new XlsxWriter(outputPath) { SuppressYear1000Dates = true };
            try
            {
                int sheetNumber = 1;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.AddSheet($"Sheet_{sheetNumber}");
                    writer.WriteSheet(reader, doAutofilter: true);
                    writer.AddSheet($"SQL_{sheetNumber}", hidden: true);
                    writer.WriteSheet(StringExtension.Sqlparts(executedSql));
                    sheetNumber++;
                }
                while (reader.NextResult());
            }
            finally { writer.Dispose(); }
        }, cancellationToken);
    }

    public static Task ExportCsvAsync(
        IImportExportTasks exportTasks,
        DbDataReader reader,
        Encoding encoding,
        string outputPath,
        char separator,
        string newLine,
        Action<long>? progress,
        Action<string>? resultSetCompleted = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportTasks);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrEmpty(newLine);
        return Task.Run(() =>
        {
            int resultSetIndex = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                string resultSetPath = outputPath + (resultSetIndex > 0 ? resultSetIndex.ToString() : string.Empty);
                exportTasks.ExportCSVReader(encoding, reader, resultSetPath, separator.ToString(), false, newLine, progress);
                resultSetCompleted?.Invoke(resultSetPath);
                resultSetIndex++;
            }
            while (reader.NextResult());
        }, cancellationToken);
    }
}

/// <summary>
/// Netezza execution engine. Normal document execution uses the same
/// provider-neutral ADO/event pipeline as the other relational providers.
/// Legacy presentation remains available for modes whose semantics still
/// depend on the old BaseWindow execution surface.
/// </summary>
public sealed class NetezzaSqlExecutionEngine : ISqlExecutionEngine
{
    private readonly GeneralSqlExecutionEngine? _providerEngine;
    private readonly SqlExecutionEngineContext? _legacyContext;

    /// <summary>Compatibility constructor used by the transitional presenter tests.</summary>
    public NetezzaSqlExecutionEngine(SqlExecutionEngineContext context)
    {
        _legacyContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Production constructor. The legacy context is retained only for
    /// unsupported/legacy modes during the remaining migration.
    /// </summary>
    public NetezzaSqlExecutionEngine(
        ISqlExecutionSessionRegistry sessions,
        IImportExportTasks? exportTasks,
        SqlExecutionEngineContext legacyContext,
        IConnectionSessionRegistry? databaseSessions = null)
    {
        _providerEngine = new GeneralSqlExecutionEngine(sessions, exportTasks, databaseSessions);
        _legacyContext = legacyContext ?? throw new ArgumentNullException(nameof(legacyContext));
    }

    public bool CanExecute(string driverName) =>
        string.Equals(driverName, "NetezzaSQL", StringComparison.OrdinalIgnoreCase);

    public IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_providerEngine is not null && CanUseProviderEngine(request))
            return _providerEngine.ExecuteAsync(request, cancellationToken);

        if (_legacyContext is not null)
            return _legacyContext.ExecuteAsync(request, cancellationToken);

        throw new InvalidOperationException("No Netezza SQL execution backend is configured.");
    }

    private static bool CanUseProviderEngine(SqlExecutionRequest request) =>
        request.OutputMode is SqlOutputMode.Grid or SqlOutputMode.LogOnly
        && !request.KeepConnectionOpen
        && !request.ContinueOnError
        && !request.Explain;
}

/// <summary>
/// Provider router used by ViewModels.  It provides one, normalized event
/// envelope (started/statement/terminal) for every provider engine.
/// </summary>
public sealed class SqlExecutionRouter : ISqlExecutionUseCase
{
    private readonly IGeneralDbService _generalDbService;
    private readonly IReadOnlyList<ISqlExecutionEngine> _engines;
    private readonly SqlExecutionEngineContext _context;
    private readonly ISqlExecutionSessionRegistry? _sessions;

    public SqlExecutionRouter(
        IGeneralDbService generalDbService,
        IEnumerable<ISqlExecutionEngine> engines,
        SqlExecutionEngineContext context,
        ISqlExecutionSessionRegistry? sessions = null)
    {
        _generalDbService = generalDbService ?? throw new ArgumentNullException(nameof(generalDbService));
        _engines = engines?.ToArray() ?? throw new ArgumentNullException(nameof(engines));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessions = sessions;
    }

    public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string driverName = _generalDbService.DriverName(request.ConnectionName);
        ISqlExecutionEngine? engine = _engines.FirstOrDefault(engine => engine.CanExecute(driverName));
        if (engine is null)
        {
            yield return SqlExecutionEvent.Completed(
                request.DocumentId,
                SqlExecutionOutcome.Blocked,
                $"SQL execution is not implemented for driver '{driverName}'.");
            yield break;
        }

        using CancellationTokenRegistration cancellation = cancellationToken.Register(
            () =>
            {
                _context.Cancel(request.DocumentId, request.ConnectionName);
                if (_sessions is not null)
                    _ = _sessions.CancelAsync(request.DocumentId);
            });

        yield return SqlExecutionEvent.Started(request.DocumentId, 1);
        bool engineEmitsStatementEvents = engine is GeneralSqlExecutionEngine generalEngine
            && generalEngine.EmitsStatementEvents;
        if (!engineEmitsStatementEvents)
        {
            yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementStarted, request.DocumentId)
            {
                StatementIndex = 0,
                StatementCount = 1,
                StatementText = SqlSensitiveDataRedactor.Redact(request.SqlText)
            };
        }

        SqlExecutionEvent? terminalEvent = null;
        await using IAsyncEnumerator<SqlExecutionEvent> enumerator = engine
            .ExecuteAsync(request, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            SqlExecutionEvent? executionEvent = null;
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                terminalEvent = SqlExecutionEvent.Completed(
                    request.DocumentId,
                    SqlExecutionOutcome.Failed,
                    SqlSensitiveDataRedactor.Redact(exception.Message));
                break;
            }

            if (!hasNext)
                break;

            executionEvent = enumerator.Current;
            if (executionEvent.Kind == SqlExecutionEventKind.Completed)
            {
                terminalEvent = executionEvent;
                break;
            }
            else
                yield return executionEvent;
        }

        if (!engineEmitsStatementEvents)
        {
            yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementCompleted, request.DocumentId)
            {
                StatementIndex = 0,
                StatementCount = 1
            };
        }
        yield return terminalEvent
            ?? SqlExecutionEvent.Completed(
                request.DocumentId,
                SqlExecutionOutcome.Failed,
                "The SQL execution engine ended without a completion event.");
    }
}
