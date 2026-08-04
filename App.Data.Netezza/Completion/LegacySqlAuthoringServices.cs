using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Caching;
using JustyBase.NetezzaSqlParser.Dialects;
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
    private readonly NetezzaSqlCompletionServices _completionServices;
    private readonly SqlDialectResolver _dialectResolver;
    private readonly Dictionary<SqlDialect, LintEngine> _lintEngines = new();
    private readonly Dictionary<SqlDialect, NzSemanticTokenClassifier> _semanticTokenClassifiers = new();
    private readonly object _lintLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _lintCtsByDocument = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _lintGenerationByDocument = new(StringComparer.Ordinal);
    private bool _disposed;

    public LegacySqlAuthoringServices(NetezzaSqlCompletionServices completionServices)
        : this(completionServices, null)
    {
    }

    public LegacySqlAuthoringServices(
        NetezzaSqlCompletionServices completionServices,
        IGeneralDbService? generalDbService)
    {
        _completionServices = completionServices ?? throw new ArgumentNullException(nameof(completionServices));
        _dialectResolver = new SqlDialectResolver(generalDbService);
        _completionServices.SchemaInvalidated += CancelLintRuns;
    }

    public DocumentParsingCoordinator ParsingCoordinator => _completionServices.ParsingCoordinator;

    /// <summary>Raised on the UI thread after lint markers are applied.</summary>
    public event Action<string, IReadOnlyList<LintIssue>>? LintCompleted;

    public SqlDialect ResolveDialect(string? connectionName) => _dialectResolver.Resolve(connectionName);

    public static string FormatSql(string sql) => FormatSql(sql, SqlDialect.Netezza);

    public static string FormatSql(string sql, SqlDialect dialect)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        try
        {
            var tokens = DialectRuntime.Tokenize(sql, dialect).ToArray();
            var parser = DialectRuntime.CreateParser(tokens, dialect);
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

    public SqlHoverInfo? GetHover(string sql, int position, string documentUri, SqlDialect dialect = SqlDialect.Netezza)
        => NzHoverService.GetHover(
            sql,
            position,
            _completionServices.SchemaProvider,
            ParsingCoordinator,
            documentUri,
            DialectRuntime.AuthoringCatalog(dialect),
            dialect);

    public SqlSignatureHelpInfo? GetSignatureHelp(string sql, int position, string documentUri, SqlDialect dialect = SqlDialect.Netezza)
        => NzSignatureHelpService.GetSignatureHelp(
            sql,
            position,
            ParsingCoordinator,
            documentUri,
            DialectRuntime.AuthoringCatalog(dialect),
            dialect);

    public IReadOnlyList<SemanticTokenSpan> ClassifySemanticTokens(string sql, string documentUri, SqlDialect dialect = SqlDialect.Netezza)
        => GetSemanticTokenClassifier(dialect).Classify(sql, documentUri);

    /// <summary>
    /// Runs the parser/linter without touching a FastColoredTextBox. This is
    /// the adapter seam used by the document-scoped authoring VM; the existing
    /// ScheduleLint method remains responsible for FCTB marker rendering.
    /// </summary>
    public Task<IReadOnlyList<LintIssue>> LintAsync(
        string sql,
        string documentUri,
        CancellationToken cancellationToken = default,
        int knownLineCount = -1,
        SqlLintInvocation invocation = SqlLintInvocation.Live,
        SqlDialect dialect = SqlDialect.Netezza)
    {
        sql ??= string.Empty;
        int lineCount = SqlPerformancePolicy.ResolveLineCountForLintGate(sql, knownLineCount);
        if (SqlPerformancePolicy.ShouldSkipLint(invocation, lineCount, sql.Length))
            return Task.FromResult<IReadOnlyList<LintIssue>>([]);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            int schemaEpoch = _completionServices.SchemaProvider.MetadataEpoch;
            LintEngine lintEngine = GetLintEngine(dialect);
            LintResult result;
            if (SqlPerformancePolicy.ShouldRunCheapLintOnly(lineCount, sql.Length))
            {
                result = new LintResult(
                    lintEngine.RunCheapRules(sql),
                    lintEngine.Queue.CheapRules.Count,
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
                    CancellationToken: cancellationToken,
                    Dialect: dialect);
                ParsingCoordinator.GetOrCreate(documentUri, dialect).Parse(sql);
                result = lintEngine.RunIncrementalLint(config);
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

        var dialect = ResolveDialect(connectionName);
        if (completionContext is not null && applicationSettingsContext is not null && !string.IsNullOrEmpty(connectionName))
        {
            if (dialect == SqlDialect.Netezza)
                _completionServices.EnsureSchemaForConnection(completionContext, connectionName);
            ApplyDisabledRules(applicationSettingsContext.Config.DisabledLintRules);
        }

        (CancellationToken cancellationToken, long generation) = RenewLintCancellation(documentUri);

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
            generation,
            dialect,
            cancellationToken);
    }

    public void DisableRule(string ruleId)
    {
        if (!string.IsNullOrWhiteSpace(ruleId))
        {
            foreach (var lintEngine in GetAllLintEngines())
                lintEngine.Registry.SetSeverity(ruleId, RuleSeverityConfig.Off);
        }
    }

    public void EnableRule(string ruleId)
    {
        foreach (var lintEngine in GetAllLintEngines())
        {
            var rule = lintEngine.Registry.AllRules.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, ruleId, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
                continue;

            lintEngine.Registry.SetSeverity(rule.Id, rule.DefaultSeverity switch
            {
                LintSeverity.Error => RuleSeverityConfig.Error,
                LintSeverity.Warning => RuleSeverityConfig.Warning,
                LintSeverity.Information => RuleSeverityConfig.Information,
                LintSeverity.Hint => RuleSeverityConfig.Hint,
                _ => RuleSeverityConfig.Warning
            });
        }
    }

    private LintEngine GetLintEngine(SqlDialect dialect)
    {
        lock (_lintLock)
        {
            if (_lintEngines.TryGetValue(dialect, out var engine))
                return engine;

            engine = new LintEngine(dialect, ParsingCoordinator.GetOrCreate("legacy-lint-shared", dialect));
            _lintEngines[dialect] = engine;
            return engine;
        }
    }

    private NzSemanticTokenClassifier GetSemanticTokenClassifier(SqlDialect dialect)
    {
        if (_semanticTokenClassifiers.TryGetValue(dialect, out var classifier))
            return classifier;

        classifier = new NzSemanticTokenClassifier(
            _completionServices.SchemaProvider,
            ParsingCoordinator,
            dialect);
        _semanticTokenClassifiers[dialect] = classifier;
        return classifier;
    }

    private IReadOnlyList<LintEngine> GetAllLintEngines()
    {
        lock (_lintLock)
        {
            return
            [
                GetLintEngine(SqlDialect.Netezza),
                GetLintEngine(SqlDialect.Db2)
            ];
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
        long generation,
        SqlDialect dialect,
        CancellationToken cancellationToken)
        => Task.Run(async () =>
        {
            int debounceMs = SqlPerformancePolicy.GetLintDebounceMs(sql);
            if (!await TryDebounceAsync(debounceMs, cancellationToken).ConfigureAwait(false))
                return;

            if (cancellationToken.IsCancellationRequested || _disposed || !IsSchemaEpochCurrent(schemaEpoch) || !IsLintGenerationCurrent(documentUri, generation))
                return;

            try
            {
                LintEngine lintEngine = GetLintEngine(dialect);
                var config = new LintConfig(
                    Sql: sql,
                    Schema: _completionServices.SchemaProvider,
                    DocumentUri: documentUri,
                    MetadataEpoch: schemaEpoch,
                    CancellationToken: cancellationToken,
                    Dialect: dialect);

                int length = sql.Length;
                int lineCount = SqlPerformancePolicy.ResolveLineCountForLintGate(sql);
                LintResult result;
                if (SqlPerformancePolicy.ShouldSkipLint(SqlLintInvocation.Live, lineCount, length))
                {
                    result = new LintResult([], 0, 0, 0, false);
                }
                else if (SqlPerformancePolicy.ShouldRunCheapLintOnly(lineCount, length))
                {
                    var cheapIssues = lintEngine.RunCheapRules(sql);
                    result = new LintResult(cheapIssues, lintEngine.Queue.CheapRules.Count, 0, 0, false);
                }
                else
                {
                    ParsingCoordinator.GetOrCreate(documentUri, dialect).Parse(sql);
                    result = lintEngine.RunIncrementalLint(config);
                }

                if (cancellationToken.IsCancellationRequested || _disposed || !IsSchemaEpochCurrent(schemaEpoch) || !IsLintGenerationCurrent(documentUri, generation))
                    return;

                editor.BeginInvoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested || _disposed || editor.IsDisposed || !IsSchemaEpochCurrent(schemaEpoch) || !IsLintGenerationCurrent(documentUri, generation))
                        return;
                    if (!string.Equals(editor.Text, sql, StringComparison.Ordinal))
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

    private bool IsLintGenerationCurrent(string documentUri, long generation)
    {
        lock (_lintLock)
        {
            return _lintGenerationByDocument.TryGetValue(documentUri, out var current)
                   && current == generation;
        }
    }

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

            _lintGenerationByDocument.Remove(documentUri);
        }

        ParsingCoordinator.Release(documentUri);
    }

    private (CancellationToken Token, long Generation) RenewLintCancellation(string documentUri)
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
            long generation = _lintGenerationByDocument.TryGetValue(documentUri, out var previous)
                ? previous + 1
                : 1;
            _lintGenerationByDocument[documentUri] = generation;
            return (cts.Token, generation);
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
            _lintGenerationByDocument.Clear();
            foreach (var lintEngine in _lintEngines.Values)
                lintEngine.Dispose();
            _lintEngines.Clear();
            _semanticTokenClassifiers.Clear();
            _completionServices.ParsingCoordinator.Release("legacy-lint-shared");
        }
    }
}
