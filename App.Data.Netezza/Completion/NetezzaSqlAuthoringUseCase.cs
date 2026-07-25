using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Linter;

namespace AppBase.Data.Completion;

/// <summary>Maps parser authoring services to the neutral application API.</summary>
public sealed class NetezzaSqlAuthoringUseCase : ISqlAuthoringUseCase
{
    private readonly NetezzaSqlCompletionServices _completionServices;
    private readonly LegacySqlAuthoringServices _authoringServices;

    public NetezzaSqlAuthoringUseCase(
        NetezzaSqlCompletionServices completionServices,
        LegacySqlAuthoringServices authoringServices)
    {
        _completionServices = completionServices;
        _authoringServices = authoringServices;
    }

    public async Task<SqlLintResult> LintAsync(
        SqlLintRequest request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LintIssue> issues = await _authoringServices
            .LintAsync(request.SqlText, request.DocumentId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        return new SqlLintResult(
            request.DocumentId,
            issues.Select(MapDiagnostic).ToArray());
    }

    public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(
        SqlCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var engine = _completionServices.CreateEngine(request.DocumentId.ToString());
        IReadOnlyList<SqlCompletionItem> items = engine
            .GetCompletions(request.SqlText ?? string.Empty, request.CaretOffset)
            .Select(item => new SqlCompletionItem(
                item.Label,
                item.Label,
                item.Detail,
                Kind: item.Kind.ToString()))
            .ToArray();
        return Task.FromResult(items);
    }

    public Task<SqlSignatureHelp?> GetSignatureHelpAsync(
        SqlSignatureHelpRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var help = _authoringServices.GetSignatureHelp(
            request.SqlText ?? string.Empty,
            request.CaretOffset,
            request.DocumentId.ToString());
        if (help is null)
            return Task.FromResult<SqlSignatureHelp?>(null);

        var signatures = help.Signatures
            .Select(signature => new SqlSignatureInformation(
                signature.Label,
                signature.Documentation,
                signature.Parameters.Select(parameter => parameter.Label).ToArray()))
            .ToArray();
        return Task.FromResult<SqlSignatureHelp?>(new SqlSignatureHelp(
            signatures,
            help.ActiveSignature,
            help.ActiveParameter));
    }

    public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(
        SqlCodeActionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actions = new List<SqlCodeAction>();
        if (!string.IsNullOrWhiteSpace(request.Diagnostic.Code))
        {
            actions.Add(new SqlCodeAction(
                $"Disable rule {request.Diagnostic.Code}",
                [],
                request.Diagnostic.Code));
        }
        return Task.FromResult<IReadOnlyList<SqlCodeAction>>(actions);
    }

    public void DisableRule(string ruleId) => _authoringServices.DisableRule(ruleId);
    public void EnableRule(string ruleId) => _authoringServices.EnableRule(ruleId);

    public void Release(EditorDocumentId documentId) =>
        _authoringServices.ReleaseLint(documentId.ToString());

    private static SqlDiagnostic MapDiagnostic(LintIssue issue)
    {
        SqlDiagnosticSeverity severity = issue.Severity switch
        {
            LintSeverity.Error => SqlDiagnosticSeverity.Error,
            LintSeverity.Warning => SqlDiagnosticSeverity.Warning,
            LintSeverity.Information => SqlDiagnosticSeverity.Information,
            _ => SqlDiagnosticSeverity.Hint
        };
        return new SqlDiagnostic(
            severity,
            issue.Message,
            issue.StartOffset,
            Math.Max(0, issue.EndOffset - issue.StartOffset),
            issue.RuleId,
            "Netezza");
    }
}
