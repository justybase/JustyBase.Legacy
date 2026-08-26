using JustData.Application.Editor;
using JustData.Application.Variables;

namespace JustData.Application.Sql;

public interface IVariablePromptService
{
    Task<IReadOnlyDictionary<string, string>?> PromptAsync(
        IReadOnlyDictionary<string, string> unresolvedVariables,
        CancellationToken cancellationToken = default);
}

public enum SqlPreprocessCommandKind
{
    Scalar,
    NonQuery
}

/// <summary>
/// Database command requested while preprocessing a SQL document. The host
/// chooses the connection, risk policy, cancellation wiring and ADO method.
/// </summary>
public sealed record SqlPreprocessCommand(
    string Sql,
    SqlPreprocessCommandKind Kind,
    CancellationToken CancellationToken);

public sealed record PreprocessRequest(
    string SqlText,
    string ConnectionName,
    string DatabaseName,
    string DocumentKey,
    IReadOnlyDictionary<string, string> KnownParameters,
    bool AllowPrompts);

public sealed record PreprocessResult(
    string ProcessedSql,
    string? ExportFilePath,
    string? ExportOptionDirective,
    IReadOnlyDictionary<string, string> UpdatedKnownParameters,
    IReadOnlyDictionary<string, string> UpdatedSessionVariables,
    IReadOnlyDictionary<string, string> UpdatedGlobalVariables);

public interface ISqlPreprocessingService
{
    Task<PreprocessResult> PreprocessAsync(
        PreprocessRequest request,
        IVariablePromptService? promptService = null,
        Func<string, Task<object?>>? sqlEvaluator = null,
        CancellationToken cancellationToken = default,
        Func<SqlPreprocessCommand, Task<object?>>? sqlCommandEvaluator = null);
}
