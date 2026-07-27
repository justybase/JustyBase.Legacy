using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBase.NetezzaSqlParser.Completion;
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
    private EditorDocumentId _documentId;
    private readonly ISchemaProvider _schemaProvider;
    private readonly LegacySnippetsProvider _snippetsProvider;
    private readonly LegacyDbCompletionFallback _dbFallback;
    private readonly INetezzaAutocompleteState _state;
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
        EditorDocumentId? documentId = null)
    {
        _menu = menu;
        _completionContext = completionContext;
        _activeDocumentTitleProvider = activeDocumentTitleProvider;
        _generalDbService = generalDbService;
        _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
        _completionServices = completionServices;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _schemaProvider = completionServices.SchemaProvider;
        _snippetsProvider = new LegacySnippetsProvider(
            applicationSettingsContext,
            sessionVariableStore,
            _state);
        _dbFallback = new LegacyDbCompletionFallback(completionContext, generalDbService, _schemaTables);

        _documentId = documentId ?? EditorDocumentId.New();
        _completionEngine = completionServices.CreateEngine(_documentId.ToString());
    }

    public void SetDocumentId(EditorDocumentId documentId)
    {
        if (_documentId == documentId)
            return;
        _completionServices.ParsingCoordinator.Release(_documentId.ToString());
        _documentId = documentId;
        _completionEngine = _completionServices.CreateEngine(_documentId.ToString());
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

        if (_generalDbService.DriverName(_completionContext.SelectedConnectionName) != "NetezzaSQL")
        {
            foreach (var item in YieldGeneralDriverFallback())
                yield return PrepareLegacyItem(item);
            yield break;
        }

        _completionServices.EnsureSchemaForConnection(_completionContext, _completionContext.SelectedConnectionName);

        foreach (var item in GetSqlCompletions(text))
            yield return PrepareLegacyItem(item);
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

    private IEnumerable<AutocompleteItem> GetSqlCompletions(string fragmentText)
    {
        var tb = _menu.Fragment.tb;
        var sql = tb.Text;
        var cursorOffset = tb.PlaceToPosition(_menu.Fragment.End);
        // The parser engine already caches tokenization and parsing through the
        // shared DocumentParsingCoordinator. Do not cache the mapped FCTB list:
        // AliasHints and HintWithTable are intentionally mutated asynchronously
        // by AutocompleteClass and must be reflected immediately.
        foreach (var item in BuildSqlCompletions(fragmentText, sql, cursorOffset))
            yield return item;
    }

    private IEnumerable<AutocompleteItem> BuildSqlCompletions(string text, string sql, int cursorOffset)
    {
        var ddTables = FctbCompletionMapper.MapDatabaseDoubleDotTables(
            text, _schemaProvider, _completionServices.MetadataSnapshot);
        if (ddTables is { Count: > 0 })
        {
            foreach (var item in ddTables)
                yield return item;
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

            AliasHints.Sort(DynamicCollectionForNettezaHelpers.SortMethodAliases(_keyValuePairsForAutocomplete));
        }

        var engineItems = _completionEngine.GetCompletions(sql, cursorOffset).ToList();
        var mappedEngineItems = FctbCompletionMapper.MapEngineItems(
            engineItems, text, _schemaProvider, _completionServices.MetadataSnapshot, sql).ToList();
        var emittedLabels = BuildEmittedLabelSet(engineItems, text);

        foreach (var item in mappedEngineItems)
            yield return item;

        bool needsLegacyFallback = LegacyCompletionPolicy.ShouldRunLegacyPath(engineItems, sql)
            || (text.Contains('.') && mappedEngineItems.Count == 0);

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

    private IEnumerable<AutocompleteItem> YieldSupplementalAliasHints(
        IReadOnlyList<CompletionItem> engineItems,
        HashSet<string> emittedLabels)
    {
        if (engineItems.Any(i => i.Kind is CompletionKind.Table or CompletionKind.View or CompletionKind.Column
                or CompletionKind.Schema or CompletionKind.Cte or CompletionKind.Database))
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

    private IEnumerable<AutocompleteItem> YieldGeneralDriverFallback()
    {
        if (!_connectionSessions.TryGetValue(_completionContext.SelectedConnectionName, out IGeneralDb database))
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
}
