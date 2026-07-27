using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using SpreadSheetTasks;

namespace JustyBaseLegacy.UI.Sql;

public interface ISqlExecutionEngine
{
    bool CanExecute(string driverName);

    IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lets the router avoid synthesizing statement lifecycle events when an
/// engine already emits them from its provider pipeline.
/// </summary>
internal interface IStatementLifecycleSqlExecutionEngine
{
    bool EmitsStatementLifecycle(SqlExecutionRequest request);
}

/// <summary>
/// Engine slice for DB2, Oracle, Postgres, SQL Server, SQLite, MySQL and
/// OLEDB.  The reader loop remains owned by the presenter while results are
/// streamed through the application event contract.
/// </summary>
public sealed class GeneralSqlExecutionEngine : ISqlExecutionEngine, IStatementLifecycleSqlExecutionEngine
{
    private readonly ISqlExecutionSessionRegistry? _sessions;
    private readonly IImportExportTasks? _exportTasks;
    private readonly IConnectionSessionRegistry? _databaseSessions;
    private readonly IApplicationSettingsContext? _settings;

    public GeneralSqlExecutionEngine(
        ISqlExecutionSessionRegistry sessions,
        IImportExportTasks? exportTasks,
        IConnectionSessionRegistry databaseSessions,
        IApplicationSettingsContext? settings = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _exportTasks = exportTasks;
        _databaseSessions = databaseSessions ?? throw new ArgumentNullException(nameof(databaseSessions));
        _settings = settings;
    }

    internal bool EmitsStatementEvents => _sessions is not null;
    bool IStatementLifecycleSqlExecutionEngine.EmitsStatementLifecycle(SqlExecutionRequest request) =>
        EmitsStatementEvents;
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
            throw new InvalidOperationException("No SQL execution backend is configured.");

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

            if (!request.KeepConnectionOpen)
                _sessions.ReleaseRetainedConnection(request.DocumentId);

            DbConnection? retainedConnection = null;
            bool hasRetainedConnection = request.KeepConnectionOpen
                && _sessions.TryGetRetainedConnection(
                    request.DocumentId,
                    request.ConnectionName,
                    request.DatabaseName,
                    out retainedConnection);
            DbConnection connection = retainedConnection ?? (string.IsNullOrWhiteSpace(request.DatabaseName)
                ? database.GetConnection()
                : database.GetConnection(request.DatabaseName));

            try
            {
                // F5 can split a selected script into multiple commands, but
                // all commands share this one connection. SingleBatch remains
                // the only mode that sends the full text as one command.
                session.SetConnection(connection, ownsConnection: false);
                session.SetProviderAbort(() => database.AbortAsync("x"));
                if (connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(connection, cancellationToken).ConfigureAwait(false);

                for (int statementIndex = 0; statementIndex < batches.Count; statementIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string sql = batches[statementIndex];
                    yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementStarted, request.DocumentId)
                    { StatementIndex = statementIndex, StatementCount = batches.Count, StatementText = SqlSensitiveDataRedactor.Redact(sql) };

                    IAsyncEnumerable<SqlExecutionEvent> statementEvents = request.ContinueOnError
                        ? ExecuteStatementContinuingOnErrorAsync(
                            connection, session, request, sql, statementIndex, cancellationToken)
                        : ExecuteStatementAsync(connection, session, request, sql, statementIndex, cancellationToken);
                    await foreach (SqlExecutionEvent statementEvent in statementEvents
                        .WithCancellation(cancellationToken))
                        yield return statementEvent;

                    yield return new SqlExecutionEvent(SqlExecutionEventKind.StatementCompleted, request.DocumentId)
                    { StatementIndex = statementIndex, StatementCount = batches.Count };
                }
            }
            finally
            {
                bool keepConnection = request.KeepConnectionOpen
                    && !session.IsCancelling
                    && connection.State == ConnectionState.Open;
                if (keepConnection)
                {
                    _sessions.RetainConnection(
                        request.DocumentId,
                        request.ConnectionName,
                        request.DatabaseName,
                        connection);
                }
                else if (hasRetainedConnection)
                {
                    _sessions.ReleaseRetainedConnection(request.DocumentId);
                }
                else
                {
                    try { if (connection.State != ConnectionState.Closed) connection.Close(); }
                    finally { connection.Dispose(); }
                }
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

    private async IAsyncEnumerable<SqlExecutionEvent> ExecuteStatementContinuingOnErrorAsync(
        DbConnection connection,
        ISqlExecutionSession session,
        SqlExecutionRequest request,
        string sql,
        int statementIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Exception? statementFailure = null;
        await using IAsyncEnumerator<SqlExecutionEvent> enumerator = ExecuteStatementAsync(
                connection, session, request, sql, statementIndex, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested && !session.IsCancelling)
            {
                statementFailure = exception;
                break;
            }

            if (!hasNext)
                break;
            yield return enumerator.Current;
        }

        if (statementFailure is not null)
        {
            string message = SqlSensitiveDataRedactor.Redact(statementFailure.Message);
            yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, request.DocumentId)
            {
                StatementIndex = statementIndex,
                Message = $"Statement failed: {message}",
                Log = new SqlLogEntry(DateTimeOffset.Now, SqlLogLevel.Error, message, statementIndex)
            };
        }
    }

    private async IAsyncEnumerable<SqlExecutionEvent> ExecuteStatementAsync(
        DbConnection connection,
        ISqlExecutionSession session,
        SqlExecutionRequest request,
        string sql,
        int statementIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using DbCommand command = CreateCommand(connection, sql, request.CommandTimeoutSeconds ?? 0);
        session.SetCommand(command, ownsCommand: false);

        if (request.OutputMode == SqlOutputMode.LogOnly)
        {
            int affected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
            yield return new SqlExecutionEvent(SqlExecutionEventKind.AffectedRows, request.DocumentId)
            { StatementIndex = statementIndex, AffectedRows = affected };
            yield break;
        }

        using DbDataReader reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
        if (request.OutputMode is SqlOutputMode.Csv or SqlOutputMode.Xlsx or SqlOutputMode.Xlsb)
        {
            if (string.IsNullOrWhiteSpace(request.OutputPath))
                throw new ArgumentException("An output path is required for SQL export.", nameof(request));

            if (request.OutputMode == SqlOutputMode.Csv)
            {
                IImportExportTasks tasks = _exportTasks
                    ?? throw new InvalidOperationException("CSV export is not configured.");
                string separatorSetting = _settings?.Config.SepInExportedCsv ?? ";";
                char separator = string.IsNullOrEmpty(separatorSetting) ? ';' : separatorSetting[0];
                string newLineSetting = _settings?.Config.SepRowsInExportedCsv ?? Environment.NewLine;
                await ExportCsvAsync(
                    tasks,
                    reader,
                    ResolveEncoding(_settings?.Config.EncondingName),
                    request.OutputPath,
                    separator,
                    ResolveNewLine(newLineSetting),
                    progress: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ExportXlsxAsync(
                    reader,
                    request.OutputPath,
                    request.OutputMode == SqlOutputMode.Xlsb,
                    sql,
                    cancellationToken).ConfigureAwait(false);
            }

            yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, request.DocumentId)
            {
                StatementIndex = statementIndex,
                Message = $"Results exported to {request.OutputPath}."
            };
            yield break;
        }

        bool sawResultSet = false;
        int resultNumber = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.FieldCount <= 0)
                continue;

            sawResultSet = true;
            string resultSetId = $"{request.DocumentId}-{statementIndex}-{resultNumber++}";
            ResultColumnDescriptor[] columns = Enumerable.Range(0, reader.FieldCount)
                .Select(i => new ResultColumnDescriptor(i, reader.GetName(i), reader.GetDataTypeName(i)))
                .ToArray();
            yield return SqlExecutionEvent.Result(request.DocumentId,
                new ResultSetDescriptor(
                    resultSetId,
                    $"Result {resultNumber}",
                    columns,
                    statementIndex,
                    ExecutedSql: SqlSensitiveDataRedactor.Redact(sql)));

            var batch = new List<IReadOnlyList<object?>>(500);
            long rows = 0;
            long limit = request.RowLimit ?? long.MaxValue;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                object?[] row = new object?[reader.FieldCount];
                for (int i = 0; i < row.Length; i++)
                    row[i] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                        ? null
                        : reader.GetValue(i);
                batch.Add(row);
                rows++;
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

        if (!sawResultSet)
        {
            long affectedRows = reader.RecordsAffected;
            if (affectedRows >= 0)
            {
                yield return new SqlExecutionEvent(SqlExecutionEventKind.AffectedRows, request.DocumentId)
                { StatementIndex = statementIndex, AffectedRows = affectedRows };
            }
            yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, request.DocumentId)
            { StatementIndex = statementIndex, Message = "Statement completed without a result set." };
        }
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
                string resultSetPath = AppendResultSetIndex(outputPath, resultSetIndex);
                exportTasks.ExportCSVReader(encoding, reader, resultSetPath, separator.ToString(), false, newLine, progress);
                resultSetCompleted?.Invoke(resultSetPath);
                resultSetIndex++;
            }
            while (reader.NextResult());
        }, cancellationToken);
    }

    private static string AppendResultSetIndex(string outputPath, int resultSetIndex)
    {
        if (resultSetIndex <= 0)
            return outputPath;

        string extension = Path.GetExtension(outputPath);
        return string.IsNullOrEmpty(extension)
            ? outputPath + resultSetIndex
            : outputPath[..^extension.Length] + resultSetIndex + extension;
    }

    private static Encoding ResolveEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return int.TryParse(name, out int page) ? Encoding.GetEncoding(page) : Encoding.GetEncoding(name);
    }

    private static string ResolveNewLine(string? value) =>
        string.IsNullOrEmpty(value) ? Environment.NewLine : value.Replace("\\r", "\r").Replace("\\n", "\n");
}

/// <summary>
/// Netezza execution engine. Normal document execution uses the same
/// provider-neutral ADO/event pipeline as the other relational providers.
/// </summary>
public enum NetezzaExecutionRoute
{
    Provider
}

public sealed class NetezzaSqlExecutionEngine : ISqlExecutionEngine, IStatementLifecycleSqlExecutionEngine
{
    private readonly GeneralSqlExecutionEngine? _providerEngine;
    private readonly IConnectionSessionRegistry? _databaseSessions;
    private readonly IGeneralDbService? _generalDbService;
    private readonly IDatabaseRuntimeContext? _databaseRuntimeContext;
    private readonly ILogger? _logger;
    private readonly IImportExportTasks? _exportTasks;

    /// <summary>Production constructor: Netezza has no BaseWindow fallback.</summary>
    public NetezzaSqlExecutionEngine(
        ISqlExecutionSessionRegistry sessions,
        IImportExportTasks? exportTasks,
        IConnectionSessionRegistry databaseSessions,
        IGeneralDbService? generalDbService = null,
        IDatabaseRuntimeContext? databaseRuntimeContext = null,
        ILogger? logger = null,
        IApplicationSettingsContext? settings = null)
    {
        _providerEngine = new GeneralSqlExecutionEngine(sessions, exportTasks, databaseSessions, settings);
        _databaseSessions = databaseSessions;
        _generalDbService = generalDbService;
        _databaseRuntimeContext = databaseRuntimeContext;
        _logger = logger;
        _exportTasks = exportTasks;
    }

    public bool CanExecute(string driverName) =>
        string.Equals(driverName, "NetezzaSQL", StringComparison.OrdinalIgnoreCase);

    public IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_providerEngine is not null && GetRoute(request) == NetezzaExecutionRoute.Provider)
            return ExecuteProviderAsync(request, cancellationToken);

        throw new InvalidOperationException("No Netezza SQL execution backend is configured.");
    }

    public static NetezzaExecutionRoute GetRoute(SqlExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return NetezzaExecutionRoute.Provider;
    }

    bool IStatementLifecycleSqlExecutionEngine.EmitsStatementLifecycle(SqlExecutionRequest request) =>
        _providerEngine is not null && GetRoute(request) == NetezzaExecutionRoute.Provider;

    private async IAsyncEnumerable<SqlExecutionEvent> ExecuteProviderAsync(
        SqlExecutionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureConnectionSession(request.ConnectionName);
        SqlExecutionRequest providerRequest = request.Explain
            ? request with
            {
                // Preserve the established Netezza editor behaviour: EXPLAIN
                // applies to the selected batch by prefixing its first SQL
                // statement, and the returned plan is rendered as a result.
                SqlText = "explain verbose " + request.SqlText,
                Explain = false
            }
            : request;
        await foreach (SqlExecutionEvent executionEvent in _providerEngine!
            .ExecuteAsync(providerRequest, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            yield return executionEvent;
        }
    }

    private void EnsureConnectionSession(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName)
            || _databaseSessions is null
            || _databaseSessions.TryGetValue(connectionName, out _)
            || _generalDbService is null
            || _databaseRuntimeContext is null
            || _logger is null)
        {
            return;
        }

        IGeneralDb database = _generalDbService.GetGeneralDb(
            _databaseRuntimeContext,
            _logger,
            _exportTasks,
            connectionName,
            out _);
        database.Username = _generalDbService.UserName(connectionName);
        _databaseSessions.Set(connectionName, database);
    }
}

/// <summary>
/// Provider router used by ViewModels.  It provides one, normalized event
/// envelope (started/statement/terminal) for every provider engine.
/// </summary>
public sealed class SqlExecutionRouter : ISqlExecutionUseCase
{
    private readonly IGeneralDbService _generalDbService;
    private readonly IReadOnlyList<ISqlExecutionEngine> _engines;
    private readonly ISqlExecutionSessionRegistry? _sessions;

    public SqlExecutionRouter(
        IGeneralDbService generalDbService,
        IEnumerable<ISqlExecutionEngine> engines,
        ISqlExecutionSessionRegistry? sessions = null)
    {
        _generalDbService = generalDbService ?? throw new ArgumentNullException(nameof(generalDbService));
        _engines = engines?.ToArray() ?? throw new ArgumentNullException(nameof(engines));
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
                if (_sessions is not null)
                    _ = _sessions.CancelAsync(request.DocumentId);
            });

        yield return SqlExecutionEvent.Started(request.DocumentId, 1);
        bool engineEmitsStatementEvents = engine is IStatementLifecycleSqlExecutionEngine lifecycleEngine
            && lifecycleEngine.EmitsStatementLifecycle(request);
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
