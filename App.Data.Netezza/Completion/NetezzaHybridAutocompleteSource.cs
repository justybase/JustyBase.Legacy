using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustData.Application.Sql;
using JustyBase.Netezza.Completion;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Dialects;
using JustyBase.NetezzaSqlParser.Visitor;
using System.Collections;

namespace AppBase.Data.Completion;

/// <summary>
/// Engine-first Netezza autocomplete for FastColoredTextBox.
/// Replaces regex-based DynamicCollectionForNetteza SQL context logic.
/// </summary>
public sealed class NetezzaHybridAutocompleteSource : IEnumerable<AutocompleteItem>, INetezzaAutocompleteSource
{
    private readonly AutocompleteMenu _menu;
    private readonly INetezzaCompletionContext _completionContext;
    private readonly Func<string> _activeDocumentTitleProvider;
    private readonly IGeneralDbService _generalDbService;
    private readonly IConnectionSessionRegistry _connectionSessions;
    private readonly INetezzaSchemaTableCatalog _schemaTables;
    private readonly NetezzaSqlCompletionServices _completionServices;
    private NzCompletionEngine _completionEngine;
    private SqlDialect _dialect;
    private EditorDocumentId _documentId;
    private readonly ISchemaProvider _schemaProvider;
    private readonly LegacySnippetsProvider _snippetsProvider;
    private readonly LegacyDbCompletionFallback _dbFallback;
    private readonly INetezzaAutocompleteState _state;
    private readonly Func<string> _connectionNameProvider;
    private readonly Func<string> _databaseNameProvider;
    private readonly Dictionary<string, int> _keyValuePairsForAutocomplete = new();

    public NetezzaHybridAutocompleteSource(
        AutocompleteMenu menu,
        FastColoredTextBox editor,
        IApplicationSettingsContext applicationSettingsContext,
        INetezzaCompletionContext completionContext,
        JustData.Application.Variables.ISessionVariableStore sessionVariableStore,
        Func<string> activeDocumentTitleProvider,
        IGeneralDbService generalDbService,
        IConnectionSessionRegistry connectionSessions,
        INetezzaSchemaTableCatalog schemaTables,
        NetezzaSqlCompletionServices completionServices,
        INetezzaAutocompleteState state,
        EditorDocumentId? documentId = null,
        Func<string>? connectionNameProvider = null,
        Func<string>? databaseNameProvider = null)
    {
        _menu = menu;
        _completionContext = completionContext;
        _activeDocumentTitleProvider = activeDocumentTitleProvider;
        _generalDbService = generalDbService;
        _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
        _completionServices = completionServices;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _connectionNameProvider = connectionNameProvider ?? (() => _completionContext.SelectedConnectionName);
        _databaseNameProvider = databaseNameProvider ?? (() => _completionContext.SelectedDatabase);
        _schemaProvider = completionServices.SchemaProvider;
        _snippetsProvider = new LegacySnippetsProvider(
            applicationSettingsContext,
            sessionVariableStore,
            _state);
        _dbFallback = new LegacyDbCompletionFallback(
            completionContext,
            generalDbService,
            _schemaTables,
            _connectionSessions);

        _documentId = documentId ?? EditorDocumentId.New();
        _dialect = ResolveDialect(_connectionNameProvider());
        _completionEngine = completionServices.CreateEngine(_documentId.ToString(), _dialect);
    }

    public void SetDocumentId(EditorDocumentId documentId)
    {
        if (_documentId == documentId)
            return;
        _completionServices.ParsingCoordinator.Release(_documentId.ToString());
        _documentId = documentId;
        _completionEngine = _completionServices.CreateEngine(_documentId.ToString(), _dialect);
    }

    public void ResetCache() => _dbFallback.ResetCache();

    public List<(string basicHint, string description)> AliasHints { get; set; } = new();
    public List<string> HintWithTable { get; set; } = new();
    public INetezzaAutocompleteState State => _state;

    public IEnumerator<AutocompleteItem> GetEnumerator()
    {
        var text = _menu.Fragment.Text;
        bool isQualifiedSql = !text.StartsWith("@@") && text.Contains('.');

        if (!isQualifiedSql)
        {
            foreach (var item in _snippetsProvider.YieldPreambleItems(_activeDocumentTitleProvider()))
                yield return PrepareLegacyItem(item);

            if (_snippetsProvider.TryYieldAtPrefixItems(text, AliasHints, out var prefixItems))
            {
                foreach (var item in prefixItems)
                    yield return PrepareLegacyItem(item);
                yield break;
            }

            foreach (var item in prefixItems)
                yield return PrepareLegacyItem(item);

            foreach (var item in _snippetsProvider.YieldKeywordsAndSnippets(text))
                yield return PrepareLegacyItem(item);
        }
        else if (text.StartsWith("@@"))
        {
            if (_snippetsProvider.TryYieldAtPrefixItems(text, AliasHints, out var prefixItems))
            {
                foreach (var item in prefixItems)
                    yield return PrepareLegacyItem(item);
                yield break;
            }
        }

        if (!_completionContext.SchemaRefreshed)
            yield break;

        string activeConnectionName = _connectionNameProvider();
        string activeDatabaseName = _databaseNameProvider();
        SqlDialect previousDialect = _dialect;
        _dialect = ResolveDialect(activeConnectionName);
        if (_dialect != previousDialect)
            _completionServices.InvalidateSchema();
        _completionEngine = _completionServices.CreateEngine(_documentId.ToString(), _dialect);

        if (_dialect is not (SqlDialect.Netezza or SqlDialect.Db2))
        {
            foreach (var item in YieldGeneralDriverFallback(activeConnectionName))
                yield return PrepareLegacyItem(item);
            yield break;
        }

        if (_dialect == SqlDialect.Netezza)
            _completionServices.EnsureSchemaForConnection(_completionContext, activeConnectionName);
        else if (_connectionSessions.TryGetValue(activeConnectionName, out IGeneralDb? database)
            && database.DatabaseType == DatabaseTypeEnum.DB2)
            _completionServices.EnsureDb2Schema(database, activeConnectionName, activeDatabaseName);

        List<AutocompleteItem> engineItems = GetSqlCompletions(
            text,
            activeConnectionName,
            activeDatabaseName).ToList();
        if (_dialect != SqlDialect.Db2)
        {
            foreach (var item in engineItems)
                yield return PrepareLegacyItem(item);
            yield break;
        }

        var db2Items = new List<AutocompleteItem>(engineItems);
        var emitted = new HashSet<string>(
            db2Items.Select(item => item.ToString()),
            StringComparer.OrdinalIgnoreCase);
        foreach (var item in _dbFallback.GetCompletions(
            text,
            activeConnectionName,
            activeDatabaseName,
            _menu.Fragment.tb.Text))
        {
            if (emitted.Add(item.ToString()))
                db2Items.Add(item);
        }

        foreach (var item in YieldGeneralDriverFallback(activeConnectionName))
        {
            if (emitted.Add(item.ToString()))
                db2Items.Add(item);
        }

        int cursorOffset = _menu.Fragment.tb.PlaceToPosition(_menu.Fragment.End);
        foreach (var item in FctbCompletionMapper.PrioritizeSchemasForRelationContext(
            db2Items,
            _menu.Fragment.tb.Text,
            cursorOffset))
        {
            yield return PrepareLegacyItem(item);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static AutocompleteItem PrepareLegacyItem(AutocompleteItem item)
    {
        if (item is null || item.ImageIndex >= 0)
            return item;

        if (string.Equals(item.ToolTipTitle, "View", StringComparison.OrdinalIgnoreCase))
            return CompletionItemAppearance.Apply(item, CompletionIconKind.View, "View");

        if (string.Equals(item.ToolTipTitle, "Table", StringComparison.OrdinalIgnoreCase))
            return CompletionItemAppearance.Apply(item, CompletionIconKind.Table, "Table");

        if (string.Equals(item.ToolTipTitle, "with", StringComparison.OrdinalIgnoreCase))
            return CompletionItemAppearance.Apply(item, CompletionIconKind.Cte, "CTE");

        if (item is MethodAutocompleteItem2)
            return CompletionItemAppearance.Apply(item, CompletionIconKind.Reference, "Reference");

        return CompletionItemAppearance.Apply(item, CompletionIconKind.Keyword, "Keyword");
    }

    private IEnumerable<AutocompleteItem> GetSqlCompletions(
        string fragmentText,
        string activeConnectionName,
        string activeDatabaseName)
    {
        var tb = _menu.Fragment.tb;
        var sql = tb.Text;
        var cursorOffset = tb.PlaceToPosition(_menu.Fragment.End);
        // The parser engine already caches tokenization and parsing through the
        // shared DocumentParsingCoordinator. Do not cache the mapped FCTB list:
        // AliasHints and HintWithTable are intentionally mutated asynchronously
        // by AutocompleteClass and must be reflected immediately.
        foreach (var item in BuildSqlCompletions(fragmentText, sql, cursorOffset, tb.LinesCount, activeDatabaseName))
            yield return item;
    }

    private IEnumerable<AutocompleteItem> BuildSqlCompletions(
        string text,
        string sql,
        int cursorOffset,
        int lineCount,
        string activeDatabaseName)
    {
        var ddTables = FctbCompletionMapper.MapDatabaseDoubleDotTables(
            text, _schemaProvider, _completionServices.MetadataSnapshot);
        if (ddTables is { Count: > 0 })
        {
            foreach (var item in ddTables)
                yield return item;
            yield break;
        }

        bool largeDoc = SqlPerformancePolicy.ShouldSkipDeepAutocompleteScan(lineCount, sql.Length);
        bool forced = _menu.LastAutocompleteForced;
        if (largeDoc && !forced)
        {
            int probe = Math.Min(cursorOffset, Math.Max(0, sql.Length - 1));
            (int stmtStart, _) = SqlTextCursorParser.GetStatementBounds(probe, sql);
            int stmtChars = stmtStart >= 0 ? cursorOffset - stmtStart : sql.Length;
            if (stmtChars > SqlPerformancePolicy.PassiveAutocompleteStatementCharLimit)
                yield break;
        }

        if (!string.IsNullOrWhiteSpace(_state.CurrentColumn))
        {
            _keyValuePairsForAutocomplete.Clear();
            foreach (var (basicHint, _) in AliasHints)
            {
                _keyValuePairsForAutocomplete[basicHint] = SqlTextModifyDefaultSqlImplementations.DamerauLevenshteinDistance(
                    basicHint, text + _state.CurrentColumn);
            }

            AliasHints.Sort(NetezzaLegacyCompletionHelpers.SortMethodAliases(_keyValuePairsForAutocomplete));
        }

        (string engineSql, int engineCursor) = SliceSqlForEngine(sql, cursorOffset, largeDoc, forced);
        var engineItems = _completionEngine.GetCompletions(engineSql, engineCursor).ToList();
        var mappedEngineItems = FctbCompletionMapper.MapEngineItems(
            engineItems, text, _schemaProvider, _completionServices.MetadataSnapshot, sql).ToList();
        var emittedLabels = BuildEmittedLabelSet(engineItems, text);

        foreach (var item in mappedEngineItems)
            yield return item;

        bool needsLegacyFallback = _dialect == SqlDialect.Netezza
            && (SqlCompletionMergePolicy.ShouldRunLegacyPath(engineItems, sql)
                || (text.Contains('.') && mappedEngineItems.Count == 0));

        if (needsLegacyFallback)
        {
            var seen = new HashSet<string>(emittedLabels, StringComparer.OrdinalIgnoreCase);
            foreach (var item in _dbFallback.GetCompletions(text))
            {
                var label = item.ToString();
                if (!seen.Add(label))
                    continue;
                yield return item;
            }
        }

        foreach (var item in YieldSupplementalAliasHints(engineItems, emittedLabels))
            yield return item;
    }

    public static (string Sql, int CursorOffset) SliceSqlForEngine(
        string sql,
        int cursorOffset,
        bool largeDoc,
        bool forcedAutocomplete = true)
    {
        if (!largeDoc || sql.Length <= SqlPerformancePolicy.AutocompleteLookbackCharLimit)
            return (sql, cursorOffset);

        int probe = Math.Min(cursorOffset, Math.Max(0, sql.Length - 1));
        (int stmtStart, _) = SqlTextCursorParser.GetStatementBounds(probe, sql);
        if (stmtStart >= 0)
        {
            int stmtChars = cursorOffset - stmtStart;
            int stmtLimit = forcedAutocomplete
                ? SqlPerformancePolicy.AutocompleteLookbackCharLimit
                : SqlPerformancePolicy.PassiveAutocompleteStatementCharLimit;
            if (stmtChars > 0 && stmtChars <= stmtLimit)
            {
                string block = sql.Substring(stmtStart, stmtChars);
                return (block, block.Length);
            }
        }

        int windowStart = Math.Max(0, cursorOffset - SqlPerformancePolicy.AutocompleteLookbackCharLimit);
        int windowEnd = Math.Min(sql.Length, cursorOffset + 4_096);
        string window = sql.Substring(windowStart, windowEnd - windowStart);
        return (window, cursorOffset - windowStart);
    }

    private IEnumerable<AutocompleteItem> YieldSupplementalAliasHints(
        IReadOnlyList<CompletionItem> engineItems,
        HashSet<string> emittedLabels)
    {
        if (engineItems.Any(i => i.Kind is CompletionKind.Table or CompletionKind.View or CompletionKind.ExternalTable
                or CompletionKind.Column or CompletionKind.Schema or CompletionKind.Cte or CompletionKind.Database))
            yield break;

        foreach (var item in YieldAliasAndCteHints(emittedLabels))
            yield return item;
    }

    private IEnumerable<AutocompleteItem> YieldAliasAndCteHints(HashSet<string> emittedLabels)
    {
        foreach (var (basicHint, description) in AliasHints)
        {
            if (emittedLabels.Contains(basicHint))
                continue;

            string desc = "empty desc";
            string dataType = null;
            if (description is not null && description.Contains('|'))
            {
                int indx = description.IndexOf('|');
                dataType = description[..indx];
                if (!description.EndsWith('|'))
                    desc = description[(indx + 1)..];
            }
            else
            {
                desc = null;
            }

            var aliasItem = new MethodAutocompleteItem2(basicHint)
            {
                ToolTipTitle = dataType,
                ToolTipText = desc
            };
            yield return CompletionItemAppearance.Apply(aliasItem, CompletionIconKind.Alias, dataType, desc);
        }

        foreach (var item in HintWithTable)
        {
            if (!emittedLabels.Contains(item))
            {
                var cteItem = new MethodAutocompleteItem2(item) { ToolTipTitle = "with" };
                yield return CompletionItemAppearance.Apply(cteItem, CompletionIconKind.Cte, "CTE");
            }
        }
    }

    private static HashSet<string> BuildEmittedLabelSet(IReadOnlyList<CompletionItem> engineItems, string fragmentText)
    {
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ci in engineItems)
        {
            labels.Add(ci.Label);
            labels.Add(FctbCompletionMapper.QualifyLabel(ci.Label, fragmentText));
        }

        return labels;
    }

    private IEnumerable<AutocompleteItem> YieldGeneralDriverFallback(string connectionName)
    {
        if (!_connectionSessions.TryGetValue(connectionName, out IGeneralDb database))
            yield break;

        IAutocompleteSuggestionStore suggestions = database.AutocompleteSuggestions;
        foreach (var item in suggestions.OneWord)
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item), CompletionIconKind.Keyword, "Keyword");

        foreach (var item in suggestions.ActualColumnList)
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item), CompletionIconKind.Column, "Column");

        foreach (var item in suggestions.OneWordAdditions)
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item), CompletionIconKind.Keyword, "Keyword");

        foreach (var item in suggestions.TwoWords)
            yield return CompletionItemAppearance.Apply(
                new MethodAutocompleteItem2(item), CompletionIconKind.Keyword, "Keyword");

        foreach (var item in suggestions.TwoWordsAdditions)
            yield return CompletionItemAppearance.Apply(
                new MethodAutocompleteItem2(item), CompletionIconKind.Keyword, "Keyword");

        if (suggestions.TreeWords.Count > 0)
        {
            foreach (var item in suggestions.TreeWords)
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2(item), CompletionIconKind.Reference, "Reference");
        }

        foreach (var item in _state.Keywords)
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item), CompletionIconKind.Keyword, "Keyword");
    }

    private SqlDialect ResolveDialect(string connectionName) =>
        string.Equals(
            _generalDbService.DriverName(connectionName),
            "DB2",
            StringComparison.OrdinalIgnoreCase)
            ? SqlDialect.Db2
            : SqlDialect.Netezza;
}
