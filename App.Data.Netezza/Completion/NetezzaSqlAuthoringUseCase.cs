using JustData.Application.Editor;
using JustData.Application.Sql;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Dialects;
using JustyBase.NetezzaSqlParser.Linter;

namespace AppBase.Data.Completion;

/// <summary>Maps parser authoring services to the neutral application API.</summary>
public sealed class NetezzaSqlAuthoringUseCase : ISqlAuthoringUseCase
{
    private readonly NetezzaSqlCompletionServices _completionServices;
    private readonly LegacySqlAuthoringServices _authoringServices;
    private readonly SqlDialectResolver _dialectResolver;
    private readonly INetezzaCompletionContext? _completionContext;

    public NetezzaSqlAuthoringUseCase(
        NetezzaSqlCompletionServices completionServices,
        LegacySqlAuthoringServices authoringServices)
        : this(completionServices, authoringServices, null, null)
    {
    }

    public NetezzaSqlAuthoringUseCase(
        NetezzaSqlCompletionServices completionServices,
        LegacySqlAuthoringServices authoringServices,
        IGeneralDbService? generalDbService,
        INetezzaCompletionContext? completionContext = null)
    {
        _completionServices = completionServices;
        _authoringServices = authoringServices;
        _dialectResolver = new SqlDialectResolver(generalDbService);
        _completionContext = completionContext;
    }

    public async Task<SqlLintResult> LintAsync(
        SqlLintRequest request,
        CancellationToken cancellationToken = default)
    {
        string databaseName = ResolveDatabaseName(request.ConnectionName, request.DatabaseName);
        SqlDialect dialect = _dialectResolver.Resolve(request.ConnectionName);
        return await _completionServices.WithAuthoringGateAsync(async () =>
        {
            if (dialect == SqlDialect.Netezza && _completionContext is not null)
            {
                _completionServices.EnsureSchemaForConnectionUnderAuthoringGate(
                    _completionContext, request.ConnectionName, databaseName);
            }

            IReadOnlyList<LintIssue> issues = await _authoringServices
                .LintAsyncUnderAuthoringGate(
                    request.SqlText,
                    request.DocumentId.ToString(),
                    cancellationToken,
                    request.KnownLineCount,
                    request.Invocation,
                    dialect,
                    databaseName,
                    request.CaretOffset)
                .ConfigureAwait(false);
            return new SqlLintResult(
                request.DocumentId,
                issues.Select(issue => MapDiagnostic(issue, dialect)).ToArray());
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(
        SqlCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve(request.ConnectionName);
        string databaseName = ResolveDatabaseName(request.ConnectionName, request.DatabaseName);
        return _completionServices.WithAuthoringGateAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dialect == SqlDialect.Netezza && _completionContext is not null)
            {
                _completionServices.EnsureSchemaForConnectionUnderAuthoringGate(
                    _completionContext, request.ConnectionName, databaseName);
            }

            var engine = _completionServices.CreateEngine(
                request.DocumentId.ToString(), dialect, databaseName);
            IReadOnlyList<SqlCompletionItem> items = engine
                .GetCompletions(request.SqlText ?? string.Empty, request.CaretOffset)
                .Select(item => new SqlCompletionItem(
                    item.Label,
                    item.Label,
                    item.Detail,
                    Kind: item.Kind.ToString()))
                .ToArray();
            return Task.FromResult(items);
        }, cancellationToken);
    }

    private string ResolveDatabaseName(string connectionName, string? requestedDatabaseName) =>
        !string.IsNullOrWhiteSpace(requestedDatabaseName)
            ? requestedDatabaseName
            : _completionContext is not null
                && string.Equals(_dialectResolver.Resolve(connectionName), SqlDialect.Netezza)
                && (string.IsNullOrWhiteSpace(connectionName)
                    || string.Equals(connectionName, _completionContext.SelectedConnectionName,
                        StringComparison.OrdinalIgnoreCase))
                ? _completionContext.SelectedDatabase
                : string.Empty;

    public Task<SqlSignatureHelp?> GetSignatureHelpAsync(
        SqlSignatureHelpRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var help = _authoringServices.GetSignatureHelp(
            request.SqlText ?? string.Empty,
            request.CaretOffset,
            request.DocumentId.ToString(),
            _dialectResolver.Resolve(request.ConnectionName));
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

    private static SqlDiagnostic MapDiagnostic(LintIssue issue, SqlDialect dialect)
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
            DialectRuntime.DiagnosticSource(dialect));
    }
}
