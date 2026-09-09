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
using JustData.Application.Sql;
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
    private readonly Dictionary<string, HashSet<string>> _lintParsingUrisByDocument = new(StringComparer.Ordinal);
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
        SqlDialect dialect = SqlDialect.Netezza,
        string? databaseName = null,
        int caretOffset = -1)
        => _completionServices.WithAuthoringGateAsync(
            () => LintAsyncUnderAuthoringGate(
                sql,
                documentUri,
                cancellationToken,
                knownLineCount,
                invocation,
                dialect,
                databaseName,
                caretOffset),
            cancellationToken);

    internal Task<IReadOnlyList<LintIssue>> LintAsyncUnderAuthoringGate(
        string sql,
        string documentUri,
        CancellationToken cancellationToken = default,
        int knownLineCount = -1,
        SqlLintInvocation invocation = SqlLintInvocation.Live,
        SqlDialect dialect = SqlDialect.Netezza,
        string? databaseName = null,
        int caretOffset = -1)
    {
        sql ??= string.Empty;
        string authoringSql = SqlPreprocessingService.RemoveAmpersandDeclarationsForAuthoring(sql);
        int lineCount = SqlPerformancePolicy.ResolveLineCountForLintGate(sql, knownLineCount);
        if (SqlPerformancePolicy.ShouldSkipLint(invocation, lineCount, sql.Length))
            return Task.FromResult<IReadOnlyList<LintIssue>>([]);

        string lintDocumentUri = BuildLintDocumentUri(documentUri, dialect, databaseName);
        RegisterLintParsingUri(documentUri, lintDocumentUri);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            int schemaEpoch = _completionServices.SchemaProvider.MetadataEpoch;
            LintEngine lintEngine = GetLintEngine(dialect);
            LintResult result;
            if (SqlPerformancePolicy.ShouldRunCheapLintOnly(lineCount, sql.Length))
            {
                result = new LintResult(
                    lintEngine.RunCheapRules(authoringSql),
                    lintEngine.Queue.CheapRules.Count,
                    0,
                    0,
                    false);
            }
            else
            {
                var config = new LintConfig(
                    Sql: authoringSql,
                    Schema: _completionServices.SchemaProvider,
                    DocumentUri: lintDocumentUri,
                    MetadataEpoch: schemaEpoch,
                    CancellationToken: cancellationToken,
                    Dialect: dialect,
                    ActiveDatabase: databaseName);
                ParsingCoordinator.GetOrCreate(lintDocumentUri, dialect).Parse(authoringSql);
                result = lintEngine.RunIncrementalLint(config);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<LintIssue>)FilterLiveIssues(sql, result.Issues, invocation, caretOffset);
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
                _completionServices.EnsureSchemaForConnection(
                    completionContext, connectionName, completionContext.SelectedDatabase);
            ApplyDisabledRules(applicationSettingsContext.Config.DisabledLintRules);
        }

        (CancellationToken cancellationToken, long generation) = RenewLintCancellation(documentUri);

        string sql = editor.Text;
        var capturedEditor = editor;
        var capturedColors = colors;
        var capturedUri = documentUri;
        RegisterLintParsingUri(
            capturedUri,
            BuildLintDocumentUri(capturedUri, dialect, completionContext?.SelectedDatabase));

        _ = RunLintAsync(
            sql,
            capturedEditor,
            capturedColors,
            capturedUri,
            generation,
            dialect,
            connectionName,
            completionContext,
            completionContext?.SelectedDatabase,
            editor.SelectionStart,
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
                GetLintEngine(SqlDialect.Db2),
                GetLintEngine(SqlDialect.Mssql),
                GetLintEngine(SqlDialect.PostgreSql)
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
        long generation,
        SqlDialect dialect,
        string? connectionName,
        INetezzaCompletionContext? completionContext,
        string? databaseName,
        int caretOffset,
        CancellationToken cancellationToken)
        => Task.Run(async () =>
        {
            int debounceMs = SqlPerformancePolicy.GetLintDebounceMs(sql);
            if (!await TryDebounceAsync(debounceMs, cancellationToken).ConfigureAwait(false))
                return;

            try
            {
                await _completionServices.WithAuthoringGateAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested || _disposed || !IsLintGenerationCurrent(documentUri, generation))
                        return true;

                    if (dialect == SqlDialect.Netezza
                        && completionContext is not null
                        && !string.IsNullOrEmpty(connectionName))
                    {
                        _completionServices.EnsureSchemaForConnectionUnderAuthoringGate(
                            completionContext, connectionName, databaseName);
                    }

                    int schemaEpoch = _completionServices.SchemaProvider.MetadataEpoch;
                    LintEngine lintEngine = GetLintEngine(dialect);
                    string authoringSql = SqlPreprocessingService.RemoveAmpersandDeclarationsForAuthoring(sql);
                    var config = new LintConfig(
                        Sql: authoringSql,
                        Schema: _completionServices.SchemaProvider,
                        DocumentUri: BuildLintDocumentUri(documentUri, dialect, databaseName),
                        MetadataEpoch: schemaEpoch,
                        CancellationToken: cancellationToken,
                        Dialect: dialect,
                        ActiveDatabase: databaseName);

                    int length = sql.Length;
                    int lineCount = SqlPerformancePolicy.ResolveLineCountForLintGate(sql);
                    LintResult result;
                    if (SqlPerformancePolicy.ShouldSkipLint(SqlLintInvocation.Live, lineCount, length))
                    {
                        result = new LintResult([], 0, 0, 0, false);
                    }
                    else if (SqlPerformancePolicy.ShouldRunCheapLintOnly(lineCount, length))
                    {
                        var cheapIssues = lintEngine.RunCheapRules(authoringSql);
                        result = new LintResult(cheapIssues, lintEngine.Queue.CheapRules.Count, 0, 0, false);
                    }
                    else
                    {
                        ParsingCoordinator.GetOrCreate(
                            config.DocumentUri, dialect).Parse(authoringSql);
                        result = lintEngine.RunIncrementalLint(config);
                    }

                    result = result with
                    {
                        Issues = FilterLiveIssues(sql, result.Issues, SqlLintInvocation.Live, caretOffset)
                    };

                    if (cancellationToken.IsCancellationRequested || _disposed || !IsSchemaEpochCurrent(schemaEpoch) || !IsLintGenerationCurrent(documentUri, generation))
                        return true;

                    editor.BeginInvoke(() =>
                    {
                        if (cancellationToken.IsCancellationRequested || _disposed || editor.IsDisposed || !IsSchemaEpochCurrent(schemaEpoch) || !IsLintGenerationCurrent(documentUri, generation))
                            return;
                        if (!string.Equals(editor.Text, sql, StringComparison.Ordinal))
                            return;

                        ApplyLintMarkers(editor, colors, result.Issues);
                        LintCompleted?.Invoke(documentUri, result.Issues);
                    });
                    return true;
                }, cancellationToken).ConfigureAwait(false);
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

    private void RegisterLintParsingUri(string documentUri, string lintDocumentUri)
    {
        lock (_lintLock)
        {
            if (!_lintParsingUrisByDocument.TryGetValue(documentUri, out var uris))
            {
                uris = new HashSet<string>(StringComparer.Ordinal);
                _lintParsingUrisByDocument[documentUri] = uris;
            }

            uris.Add(lintDocumentUri);
        }
    }

    private static string BuildLintDocumentUri(string documentUri, SqlDialect dialect, string? databaseName) =>
        $"{documentUri}|dialect:{dialect}|database:{databaseName ?? string.Empty}";

    private static IReadOnlyList<LintIssue> FilterLiveIssues(
        string sql,
        IReadOnlyList<LintIssue> issues,
        SqlLintInvocation invocation,
        int caretOffset)
    {
        if (invocation != SqlLintInvocation.Live || issues.Count == 0)
            return issues;

        int end = Math.Clamp(caretOffset < 0 ? sql.Length : caretOffset, 0, sql.Length);
        string prefix = sql[..end].TrimEnd();
        if (prefix.Length == 0 || !prefix.Contains('.'))
            return issues;

        int start = prefix.Length - 1;
        while (start >= 0 && (char.IsLetterOrDigit(prefix[start])
            || prefix[start] is '_' or '"' or '[' or ']' or '.'))
            start--;
        start++;
        string fragment = prefix[start..];
        // Only a trailing dot is an unambiguously incomplete qualifier. A
        // complete-looking name such as JUST_DATA.NO_SUCH_TABLE must remain a
        // real diagnostic in live linting.
        if (!fragment.EndsWith(".", StringComparison.Ordinal))
            return issues;

        return issues.Where(issue =>
            issue.RuleId is not ("PAR001" or "SQL006" or "SQL007")
            || issue.StartOffset < start
            || issue.StartOffset > end + 1).ToArray();
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

        string[] scopedUris;
        lock (_lintLock)
        {
            if (_lintCtsByDocument.TryGetValue(documentUri, out var cts))
            {
                _lintCtsByDocument.Remove(documentUri);
                cts.Cancel();
                cts.Dispose();
            }

            _lintGenerationByDocument.Remove(documentUri);
            scopedUris = _lintParsingUrisByDocument.TryGetValue(documentUri, out var uris)
                ? uris.ToArray()
                : [];
            _lintParsingUrisByDocument.Remove(documentUri);
        }

        _completionServices.WithAuthoringGate(() =>
        {
            foreach (string scopedUri in scopedUris)
                ParsingCoordinator.Release(scopedUri);
            ParsingCoordinator.Release(documentUri);
        });
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

        string[] scopedUris;
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
            scopedUris = _lintParsingUrisByDocument.Values
                .SelectMany(uris => uris)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            _lintParsingUrisByDocument.Clear();
            foreach (var lintEngine in _lintEngines.Values)
                lintEngine.Dispose();
            _lintEngines.Clear();
            _semanticTokenClassifiers.Clear();
        }

        _completionServices.WithAuthoringGate(() =>
        {
            foreach (string scopedUri in scopedUris)
                _completionServices.ParsingCoordinator.Release(scopedUri);
            _completionServices.ParsingCoordinator.Release("legacy-lint-shared");
        });
    }
}
