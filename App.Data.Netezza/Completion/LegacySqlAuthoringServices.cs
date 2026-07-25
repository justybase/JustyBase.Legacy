using AppBase.Common;
using AppBase.Common.Interfaces;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Caching;
using JustyBase.NetezzaSqlParser.Formatter;
using JustyBase.NetezzaSqlParser.Lexer;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBase.NetezzaSqlParser.Parser;
using SQL.Formatter;

namespace AppBase.Data.Completion;

/// <summary>
/// Shared lint/format/hover services for Legacy FCTB SQL editors (schema via <see cref="NetezzaSqlCompletionServices"/>).
/// </summary>
public sealed class LegacySqlAuthoringServices : IDisposable
{
    private const int LintDebounceMs = 500;
    private const int LargeDocumentCharLimit = 500_000;

    private readonly NetezzaSqlCompletionServices _completionServices;
    private readonly LintEngine _lintEngine;
    private readonly NzSemanticTokenClassifier _semanticTokenClassifier;
    private readonly object _lintLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _lintCtsByDocument = new(StringComparer.Ordinal);
    private bool _disposed;

    public LegacySqlAuthoringServices(NetezzaSqlCompletionServices completionServices)
    {
        _completionServices = completionServices;
        _lintEngine = new LintEngine(_completionServices.ParsingCoordinator.GetOrCreate("legacy-lint-shared"));
        _semanticTokenClassifier = new NzSemanticTokenClassifier(_completionServices.SchemaProvider, _completionServices.ParsingCoordinator);
        _completionServices.SchemaInvalidated += CancelLintRuns;
    }

    public DocumentParsingCoordinator ParsingCoordinator => _completionServices.ParsingCoordinator;

    /// <summary>Raised on the UI thread after lint markers are applied.</summary>
    public event Action<string, IReadOnlyList<LintIssue>>? LintCompleted;

    public static string FormatSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        try
        {
            var tokens = NzLexer.Tokenize(sql).ToArray();
            var parser = new NzSqlParser(tokens);
            var statement = parser.Parse();
            if (statement is not null)
                return NzSqlFormatter.Format(statement);
        }
        catch
        {
            // Fall back to generic SQL formatter below.
        }

        return SqlFormatter.Format(sql);
    }

    public SqlHoverInfo? GetHover(string sql, int position, string documentUri)
        => NzHoverService.GetHover(sql, position, _completionServices.SchemaProvider, ParsingCoordinator, documentUri);

    public SqlSignatureHelpInfo? GetSignatureHelp(string sql, int position, string documentUri)
        => NzSignatureHelpService.GetSignatureHelp(sql, position, ParsingCoordinator, documentUri);

    public IReadOnlyList<SemanticTokenSpan> ClassifySemanticTokens(string sql, string documentUri)
        => _semanticTokenClassifier.Classify(sql, documentUri);

    /// <summary>
    /// Runs the parser/linter without touching a FastColoredTextBox. This is
    /// the adapter seam used by the document-scoped authoring VM; the existing
    /// ScheduleLint method remains responsible for FCTB marker rendering.
    /// </summary>
    public Task<IReadOnlyList<LintIssue>> LintAsync(
        string sql,
        string documentUri,
        CancellationToken cancellationToken = default)
    {
        sql ??= string.Empty;
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            int schemaEpoch = _completionServices.SchemaProvider.MetadataEpoch;
            LintResult result;
            if (sql.Length > LargeDocumentCharLimit)
            {
                result = new LintResult(
                    _lintEngine.RunCheapRules(sql),
                    _lintEngine.Queue.CheapRules.Count,
                    0,
                    0,
                    false);
            }
            else
            {
                var config = new LintConfig(
                    Sql: sql,
                    Schema: _completionServices.SchemaProvider,
                    DocumentUri: documentUri,
                    MetadataEpoch: schemaEpoch,
                    CancellationToken: cancellationToken);
                ParsingCoordinator.GetOrCreate(documentUri).Parse(sql);
                result = _lintEngine.RunFullLint(config);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<LintIssue>)result.Issues;
        }, cancellationToken);
    }

    public SqlRenameInfo? GetSymbol(string sql, int position)
        => NzSymbolService.GetSymbol(sql, position);

    public SymbolOccurrence? GetDefinition(string sql, int position)
        => NzSymbolService.GetDefinition(sql, position);

    public IReadOnlyList<SymbolOccurrence> GetReferences(string sql, int position)
        => NzSymbolService.GetReferences(sql, position);

    public bool IsValidIdentifier(string identifier) => NzRenameService.IsValidIdentifier(identifier);

    public string ApplyRename(string sql, SqlRenameInfo renameInfo, string newName)
        => NzRenameService.ApplyRename(sql, renameInfo, newName);

    public void ScheduleLint(
        FastColoredTextBox editor,
        FctbColors colors,
        string documentUri,
        IApplicationSettingsContext? applicationSettingsContext = null,
        INetezzaCompletionContext? completionContext = null,
        string? connectionName = null)
    {
        if (_disposed || editor is null || string.IsNullOrEmpty(documentUri))
            return;

        if (completionContext is not null && applicationSettingsContext is not null && !string.IsNullOrEmpty(connectionName))
        {
            _completionServices.EnsureSchemaForConnection(completionContext, connectionName);
            ApplyDisabledRules(applicationSettingsContext.Config.DisabledLintRules);
        }

        CancellationToken cancellationToken = RenewLintCancellation(documentUri);

        string sql = editor.Text;
        var capturedEditor = editor;
        var capturedColors = colors;
        var capturedUri = documentUri;
        var schemaEpoch = _completionServices.SchemaProvider.MetadataEpoch;

        _ = RunLintAsync(
            sql,
            capturedEditor,
            capturedColors,
            capturedUri,
            schemaEpoch,
            cancellationToken);
    }

    public void DisableRule(string ruleId)
    {
        if (!string.IsNullOrWhiteSpace(ruleId))
        {
            _lintEngine.Registry.SetSeverity(ruleId, RuleSeverityConfig.Off);
        }
    }

    public void EnableRule(string ruleId)
    {
        var rule = _lintEngine.Registry.AllRules.FirstOrDefault(rule =>
            string.Equals(rule.Id, ruleId, StringComparison.OrdinalIgnoreCase));
        if (rule is not null)
        {
            _lintEngine.Registry.SetSeverity(rule.Id, rule.DefaultSeverity switch
            {
                LintSeverity.Error => RuleSeverityConfig.Error,
                LintSeverity.Warning => RuleSeverityConfig.Warning,
                LintSeverity.Information => RuleSeverityConfig.Information,
                LintSeverity.Hint => RuleSeverityConfig.Hint,
                _ => RuleSeverityConfig.Warning
            });
        }
    }

    private void ApplyDisabledRules(IEnumerable<string>? ruleIds)
    {
        if (ruleIds is null)
        {
            return;
        }

        foreach (string ruleId in ruleIds.Where(ruleId => !string.IsNullOrWhiteSpace(ruleId)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            // Persist the parser's canonical rule IDs unchanged. In particular,
            // NZ021 and NZ022 are real parser rules and must not be remapped.
            DisableRule(ruleId);
        }
    }

    private Task RunLintAsync(
        string sql,
        FastColoredTextBox editor,
        FctbColors colors,
        string documentUri,
        int schemaEpoch,
        CancellationToken cancellationToken)
        => Task.Run(async () =>
        {
            if (!await TryDebounceAsync(LintDebounceMs, cancellationToken).ConfigureAwait(false))
                return;

            if (cancellationToken.IsCancellationRequested || _disposed || !IsSchemaEpochCurrent(schemaEpoch))
                return;

            try
            {
                var config = new LintConfig(
                    Sql: sql,
                    Schema: _completionServices.SchemaProvider,
                    DocumentUri: documentUri,
                    MetadataEpoch: schemaEpoch,
                    CancellationToken: cancellationToken);

                LintResult result;
                if (sql.Length > LargeDocumentCharLimit)
                {
                    var cheapIssues = _lintEngine.RunCheapRules(sql);
                    result = new LintResult(cheapIssues, _lintEngine.Queue.CheapRules.Count, 0, 0, false);
                }
                else
                {
                    ParsingCoordinator.GetOrCreate(documentUri).Parse(sql);
                    result = _lintEngine.RunFullLint(config);
                }

                if (cancellationToken.IsCancellationRequested || _disposed || !IsSchemaEpochCurrent(schemaEpoch))
                    return;

                editor.BeginInvoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested || _disposed || editor.IsDisposed || !IsSchemaEpochCurrent(schemaEpoch))
                        return;

                    ApplyLintMarkers(editor, colors, result.Issues);
                    LintCompleted?.Invoke(documentUri, result.Issues);
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _disposed)
            {
                // Superseded by a newer lint run or service shutdown — expected.
            }
        });

    private bool IsSchemaEpochCurrent(int schemaEpoch) =>
        _completionServices.SchemaProvider.MetadataEpoch == schemaEpoch;

    private void CancelLintRuns()
    {
        lock (_lintLock)
        {
            foreach (var cts in _lintCtsByDocument.Values)
                cts.Cancel();
        }
    }

    private static async Task<bool> TryDebounceAsync(int milliseconds, CancellationToken cancellationToken)
    {
        var debounceTask = Task.Delay(milliseconds);
        if (!cancellationToken.CanBeCanceled)
        {
            await debounceTask.ConfigureAwait(false);
            return true;
        }

        var cancelSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            cancelSignal);

        return await Task.WhenAny(debounceTask, cancelSignal.Task).ConfigureAwait(false) == debounceTask
               && !cancellationToken.IsCancellationRequested;
    }

    public void ReleaseLint(string documentUri)
    {
        if (string.IsNullOrEmpty(documentUri))
            return;

        lock (_lintLock)
        {
            if (_lintCtsByDocument.TryGetValue(documentUri, out var cts))
            {
                _lintCtsByDocument.Remove(documentUri);
                cts.Cancel();
                cts.Dispose();
            }
        }

        ParsingCoordinator.Release(documentUri);
    }

    private CancellationToken RenewLintCancellation(string documentUri)
    {
        lock (_lintLock)
        {
            if (_lintCtsByDocument.TryGetValue(documentUri, out var existing))
            {
                existing.Cancel();
                existing.Dispose();
            }

            var cts = new CancellationTokenSource();
            _lintCtsByDocument[documentUri] = cts;
            return cts.Token;
        }
    }

    private static void ApplyLintMarkers(FastColoredTextBox editor, FctbColors colors, IReadOnlyList<LintIssue> issues)
    {
        var documentRange = editor.Range;
        documentRange.ClearStyle(colors.ErrorStyle, colors.WarningStyle, colors.LintInfoStyle);

        foreach (var issue in issues)
        {
            if (issue.StartOffset < 0 || issue.StartOffset >= editor.TextLength)
                continue;

            int length = Math.Max(1, Math.Min(issue.EndOffset - issue.StartOffset, editor.TextLength - issue.StartOffset));
            var span = new FastColoredTextBoxNS.Range(editor)
            {
                Start = editor.PositionToPlace(issue.StartOffset),
                End = editor.PositionToPlace(issue.StartOffset + length)
            };
            span.SetStyle(ResolveLintStyle(colors, issue.Severity));
        }

        editor.Invalidate();
    }

    private static TextStyle ResolveLintStyle(FctbColors colors, LintSeverity severity) =>
        severity switch
        {
            LintSeverity.Error => colors.ErrorStyle,
            LintSeverity.Warning => colors.WarningStyle,
            _ => colors.LintInfoStyle,
        };

    public void Dispose()
    {
        _completionServices.SchemaInvalidated -= CancelLintRuns;

        lock (_lintLock)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var cts in _lintCtsByDocument.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _lintCtsByDocument.Clear();
            _lintEngine.Dispose();
            _completionServices.ParsingCoordinator.Release("legacy-lint-shared");
        }
    }
}
