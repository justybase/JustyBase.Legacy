using JustData.Application.Editor;

namespace JustData.Application.Sql;

public sealed record SqlLintRequest(
    EditorDocumentId DocumentId,
    string SqlText,
    string ConnectionName = "",
    bool IncludeQuickFixes = true);

public sealed record SqlLintResult(
    EditorDocumentId DocumentId,
    IReadOnlyList<SqlDiagnostic> Diagnostics,
    long Version = 0);

public sealed record SqlCompletionRequest(
    EditorDocumentId DocumentId,
    string SqlText,
    int CaretOffset,
    string ConnectionName = "");

public sealed record SqlCompletionItem(
    string Label,
    string InsertText,
    string? Detail = null,
    string? Documentation = null,
    string? Kind = null,
    int SortPriority = 0);

public sealed record SqlSignatureInformation(
    string Label,
    string? Documentation = null,
    IReadOnlyList<string>? Parameters = null);

public sealed record SqlSignatureHelp(
    IReadOnlyList<SqlSignatureInformation> Signatures,
    int ActiveSignature = 0,
    int ActiveParameter = 0);

public sealed record SqlSignatureHelpRequest(
    EditorDocumentId DocumentId,
    string SqlText,
    int CaretOffset,
    string ConnectionName = "");

public sealed record SqlTextEdit(int StartOffset, int Length, string NewText);

public sealed record SqlCodeAction(
    string Title,
    IReadOnlyList<SqlTextEdit> Edits,
    string? RuleId = null,
    bool IsEnabled = true,
    string? DisabledReason = null);

public sealed record SqlCodeActionRequest(
    EditorDocumentId DocumentId,
    string SqlText,
    SqlDiagnostic Diagnostic,
    string ConnectionName = "");

public interface ISqlAuthoringUseCase
{
    Task<SqlLintResult> LintAsync(
        SqlLintRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(
        SqlCompletionRequest request,
        CancellationToken cancellationToken = default);

    Task<SqlSignatureHelp?> GetSignatureHelpAsync(
        SqlSignatureHelpRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(
        SqlCodeActionRequest request,
        CancellationToken cancellationToken = default);

    void DisableRule(string ruleId);
    void EnableRule(string ruleId);
    void Release(EditorDocumentId documentId);
}
