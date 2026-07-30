using AppBase.Data.Completion;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// WinForms composition decorator for parser-only quick fixes. The neutral
/// authoring contract remains the boundary used by document VMs; parser
/// diagnostics and fix delegates never cross into the clean layers.
/// </summary>
public sealed class NetezzaSqlAuthoringUseCaseAdapter : ISqlAuthoringUseCase
{
    private readonly NetezzaSqlAuthoringUseCase _inner;
    private readonly LegacySqlAuthoringServices _legacyServices;

    public NetezzaSqlAuthoringUseCaseAdapter(
        NetezzaSqlAuthoringUseCase inner,
        LegacySqlAuthoringServices legacyServices)
    {
        _inner = inner;
        _legacyServices = legacyServices;
    }

    public Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default) =>
        _inner.LintAsync(request, cancellationToken);

    public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default) =>
        _inner.CompleteAsync(request, cancellationToken);

    public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default) =>
        _inner.GetSignatureHelpAsync(request, cancellationToken);

    public async Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(
        SqlCodeActionRequest request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SqlCodeAction> actions = await _inner
            .GetCodeActionsAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(request.Diagnostic.Code))
            return actions;

        IReadOnlyList<LintIssue> issues = await _legacyServices
            .LintAsync(
                request.SqlText,
                request.DocumentId.ToString(),
                cancellationToken,
                knownLineCount: -1,
                invocation: SqlLintInvocation.Manual)
            .ConfigureAwait(false);
        LintIssue? issue = issues.FirstOrDefault(candidate =>
            string.Equals(candidate.RuleId, request.Diagnostic.Code, StringComparison.OrdinalIgnoreCase)
            && Math.Abs(candidate.StartOffset - request.Diagnostic.StartOffset) <= 1);
        if (issue is null)
            return actions;

        var fixResult = NzLintCodeActions.GetQuickFix(issue, request.SqlText);
        if (!fixResult.HasValue)
            return actions;
        var fix = fixResult.Value;

        string replacement = fix.Apply(request.SqlText);
        if (string.Equals(replacement, request.SqlText, StringComparison.Ordinal))
            return actions;

        return [new SqlCodeAction(
            fix.Description,
            [CreateEdit(request.SqlText, replacement)],
            request.Diagnostic.Code), .. actions];
    }

    public void DisableRule(string ruleId) => _inner.DisableRule(ruleId);
    public void EnableRule(string ruleId) => _inner.EnableRule(ruleId);
    public void Release(EditorDocumentId documentId) => _inner.Release(documentId);

    private static SqlTextEdit CreateEdit(string original, string replacement)
    {
        int prefix = 0;
        int commonLength = Math.Min(original.Length, replacement.Length);
        while (prefix < commonLength && original[prefix] == replacement[prefix])
            prefix++;

        int suffix = 0;
        while (suffix < commonLength - prefix
            && original[original.Length - suffix - 1] == replacement[replacement.Length - suffix - 1])
        {
            suffix++;
        }

        return new SqlTextEdit(
            prefix,
            original.Length - prefix - suffix,
            replacement.Substring(prefix, replacement.Length - prefix - suffix));
    }
}
