using JustData.Application.Editor;
using JustData.Application.Variables;

namespace JustData.Application.Sql;

public interface IVariablePromptService
{
    Task<IReadOnlyDictionary<string, string>> PromptAsync(
        IReadOnlyDictionary<string, string> unresolvedVariables,
        CancellationToken cancellationToken = default);
}

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
        CancellationToken cancellationToken = default);
}
