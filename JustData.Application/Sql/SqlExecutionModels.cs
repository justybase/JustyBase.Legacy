using JustData.Application.Editor;
using System.Text.RegularExpressions;

namespace JustData.Application.Sql;

public enum SqlExecutionMode
{
    Selection,
    RunToCursor,
    SingleBatch,
    Script
}

public enum SqlOutputMode
{
    Grid,
    Csv,
    Xlsx,
    Xlsb,
    LogOnly
}

public enum SqlExecutionState
{
    Idle,
    Running,
    Cancelling,
    Succeeded,
    Failed
}

public enum SqlExecutionOutcome
{
    Success,
    Cancelled,
    Failed,
    Blocked
}

public enum SqlDiagnosticSeverity
{
    Error,
    Warning,
    Information,
    Hint
}

public enum SqlLogLevel
{
    Trace,
    Information,
    Warning,
    Error
}

public enum SqlExecutionEventKind
{
    Started,
    StatementStarted,
    StatementCompleted,
    Log,
    Diagnostic,
    AffectedRows,
    ResultSet,
    Rows,
    Truncated,
    Completed
}

public static partial class SqlSensitiveDataRedactor
{
    [GeneratedRegex(
        "(?ix)(?<key>password|passwd|pwd|secret|token|access[\\s_-]?token|user[\\s_-]?id|uid)\\s*=\\s*(?<value>'(?:''|[^'])*'|\"(?:\"\"|[^\"])*\"|[^;\\s,]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueSecretRegex();

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return KeyValueSecretRegex().Replace(text, match => $"{match.Groups["key"].Value}=[redacted]");
    }
}

/// <summary>
/// A provider-neutral request. Editor offsets are captured before an adapter
/// starts splitting or rewriting SQL, so background execution has a stable
/// document context.
/// </summary>
public sealed record SqlExecutionRequest
{
    public SqlExecutionRequest(EditorDocumentId documentId, string sqlText)
    {
        DocumentId = documentId;
        SqlText = sqlText ?? string.Empty;
    }

    public EditorDocumentId DocumentId { get; init; }
    public string SqlText { get; init; }
    public string ConnectionName { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public SqlExecutionMode Mode { get; init; } = SqlExecutionMode.Selection;
    public SqlOutputMode OutputMode { get; init; } = SqlOutputMode.Grid;
    public int SelectionStart { get; init; }
    public int SelectionLength { get; init; }
    public int CaretOffset { get; init; }
    public bool KeepConnectionOpen { get; init; }
    public bool ContinueOnError { get; init; }
    public bool Explain { get; init; }
    public string? OutputPath { get; init; }
    public int? CommandTimeoutSeconds { get; init; }
    public long? RowLimit { get; init; }
    public bool ConfirmRiskyQueries { get; init; } = true;
    public bool ConfirmParameters { get; init; } = true;
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public SqlExecutionRequest WithMode(SqlExecutionMode mode, SqlOutputMode? outputMode = null) => this with
    {
        Mode = mode,
        OutputMode = outputMode ?? OutputMode
    };
}

public sealed record ResultColumnDescriptor(
    int Ordinal,
    string Name,
    string TypeName,
    bool IsNullable = true);

public sealed record ResultSetDescriptor(
    string ResultSetId,
    string Name,
    IReadOnlyList<ResultColumnDescriptor> Columns,
    int StatementIndex = 0,
    bool IsPinned = false);

public sealed record SqlDiagnostic(
    SqlDiagnosticSeverity Severity,
    string Message,
    int StartOffset = -1,
    int Length = 0,
    string? Code = null,
    string? Source = null,
    IReadOnlyList<SqlCodeAction>? CodeActions = null);

public sealed record SqlLogEntry(
    DateTimeOffset Timestamp,
    SqlLogLevel Level,
    string Message,
    int? StatementIndex = null);

public sealed record SqlExecutionEvent
{
    public SqlExecutionEvent(SqlExecutionEventKind kind, EditorDocumentId documentId)
    {
        Kind = kind;
        DocumentId = documentId;
    }

    public SqlExecutionEventKind Kind { get; init; }
    public EditorDocumentId DocumentId { get; init; }
    public int StatementIndex { get; init; } = -1;
    public int StatementCount { get; init; } = -1;
    public string? StatementText { get; init; }
    public string? ResultSetId { get; init; }
    public string? Message { get; init; }
    public SqlLogEntry? Log { get; init; }
    public SqlDiagnostic? Diagnostic { get; init; }
    public long? AffectedRows { get; init; }
    public ResultSetDescriptor? ResultSet { get; init; }
    public IReadOnlyList<IReadOnlyList<object?>>? Rows { get; init; }
    public long RowCount { get; init; }
    public bool IsTruncated { get; init; }
    public SqlExecutionOutcome? Outcome { get; init; }
    public string? ErrorMessage { get; init; }

    public static SqlExecutionEvent Started(EditorDocumentId id, int statementCount = -1) =>
        new(SqlExecutionEventKind.Started, id) { StatementCount = statementCount };

    public static SqlExecutionEvent Result(EditorDocumentId id, ResultSetDescriptor descriptor) =>
        new(SqlExecutionEventKind.ResultSet, id) { ResultSet = descriptor };

    public static SqlExecutionEvent RowsBatch(
        EditorDocumentId id,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        int statementIndex = -1,
        string? resultSetId = null) =>
        new(SqlExecutionEventKind.Rows, id)
        {
            Rows = rows,
            RowCount = rows.Count,
            StatementIndex = statementIndex,
            ResultSetId = resultSetId
        };

    public static SqlExecutionEvent RowsObserved(
        EditorDocumentId id,
        long rowCount,
        int statementIndex = -1,
        string? resultSetId = null) =>
        new(SqlExecutionEventKind.Rows, id)
        {
            RowCount = Math.Max(0, rowCount),
            StatementIndex = statementIndex,
            ResultSetId = resultSetId
        };

    public static SqlExecutionEvent Completed(
        EditorDocumentId id,
        SqlExecutionOutcome outcome,
        string? errorMessage = null) =>
        new(SqlExecutionEventKind.Completed, id)
        {
            Outcome = outcome,
            ErrorMessage = errorMessage
        };
}

public interface ISqlExecutionUseCase
{
    IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default);
}
