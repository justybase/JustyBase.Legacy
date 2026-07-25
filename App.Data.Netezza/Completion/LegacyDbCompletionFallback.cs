using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS;

namespace AppBase.Data.Completion;

/// <summary>
/// Thin live-DB fallback when NzCompletionEngine returns no schema objects.
/// Dot-notation traversal over DatabaseSchemaLookup (no regex SQL context).
/// </summary>
public sealed class LegacyDbCompletionFallback
{
    private readonly INetezzaCompletionContext _completionContext;
    private readonly IGeneralDbService _generalDbService;
    private readonly INetezzaSchemaTableCatalog _schemaTables;

    private string _cacheText1;
    private string _cacheText2;

    public LegacyDbCompletionFallback(
        INetezzaCompletionContext completionContext,
        IGeneralDbService generalDbService,
        INetezzaSchemaTableCatalog schemaTables)
    {
        _completionContext = completionContext ?? throw new ArgumentNullException(nameof(completionContext));
        _generalDbService = generalDbService;
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
    }

    public void ResetCache()
    {
        _cacheText1 = null;
        _cacheText2 = null;
        DynamicCollectionForNettezaHelpers.ResetCache();
    }

    public IEnumerable<AutocompleteItem> GetCompletions(string text)
    {
        if (!_completionContext.SchemaRefreshed)
            yield break;

        if (_generalDbService.DriverName(_completionContext.SelectedConnectionName) != "NetezzaSQL")
            yield break;

        string selectedConnectionName = _completionContext.SelectedConnectionName;

        if (!DynamicCollectionForNettezaHelpers.DatabaseArray.TryGetValue(selectedConnectionName, out var selectedDatabaseList))
            yield break;

        _schemaTables.TablesByConnection.TryGetValue(selectedConnectionName, out var databasesTablesSelected);
        _completionContext.DatabaseSchemaLookup.TryGetValue(selectedConnectionName, out var databaseSchemaDictionarySelected);
        _completionContext.ColumnTablesDictionary.TryGetValue(selectedConnectionName, out var selectedColumns);

        _completionContext.DatabaseOwners.TryGetValue(selectedConnectionName, out var ownersForSelectedConnection);
        if (ownersForSelectedConnection is null)
            yield break;

        ownersForSelectedConnection.TryGetValue(_completionContext.SelectedDatabase, out var ownersForSelectedConnectionSelectedDatabase);
        if (ownersForSelectedConnectionSelectedDatabase is null)
            yield break;

        if (!text.EndsWith('.'))
        {
            foreach (var item in selectedDatabaseList)
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2(item), CompletionIconKind.Database, "Database");
        }

        int lastDotIndex = text.LastDot();
        int firstDotIndex = text.FirstDot();
        int dotCount = text.DotCounter();

        if (dotCount == 0)
        {
            if (databaseSchemaDictionarySelected is not null
                && databaseSchemaDictionarySelected.TryGetValue(_completionContext.SelectedDatabase, out var r2)
                && databasesTablesSelected is not null)
            {
                var candidates = r2.Where(arg => arg.Key.StartsWith(text, StringComparison.OrdinalIgnoreCase));
                if (candidates.Count() < 300)
                {
                    foreach (var tableEntry in candidates)
                    {
                        if (!TryGetTableDesc(databasesTablesSelected, tableEntry.Value.tableId, out var desc))
                            continue;

                        yield return Table(tableEntry.Key, desc);
                    }
                }
            }

            yield break;
        }

        if (databasesTablesSelected is null || selectedColumns is null || databaseSchemaDictionarySelected is null)
            yield break;

        string firstWord = text[..firstDotIndex];
        string textUpToLastDot = text[..lastDotIndex];
        string end = text[(lastDotIndex + 1)..];
        bool isFirstWordDatabase = ownersForSelectedConnection?.ContainsKey(firstWord) == true;

        if (dotCount == 1)
        {
            foreach (var item in YieldOneDot(
                text, firstWord, end, isFirstWordDatabase,
                ownersForSelectedConnection, ownersForSelectedConnectionSelectedDatabase,
                databaseSchemaDictionarySelected, databasesTablesSelected, selectedColumns,
                selectedConnectionName))
                yield return item;

            yield break;
        }

        string secondWord = textUpToLastDot[(firstWord.Length + 1)..];

        if (dotCount == 2)
        {
            foreach (var item in YieldTwoDots(
                text, firstWord, secondWord, end, textUpToLastDot, isFirstWordDatabase,
                ownersForSelectedConnection, databaseSchemaDictionarySelected,
                databasesTablesSelected, selectedColumns))
                yield return item;
        }
        else if (dotCount >= 3 && ownersForSelectedConnection.ContainsKey(firstWord))
        {
            foreach (var item in YieldThreePlusDots(
                text, firstWord, textUpToLastDot, databaseSchemaDictionarySelected,
                databasesTablesSelected, selectedColumns))
                yield return item;
        }
    }

    private IEnumerable<AutocompleteItem> YieldOneDot(
        string text, string firstWord, string end,
        bool isFirstWordDatabase,
        Dictionary<string, Dictionary<string, string>> ownersForSelectedConnection,
        Dictionary<string, string> ownersForSelectedConnectionSelectedDatabase,
        Dictionary<string, Dictionary<string, (string owner, int tableId)>> databaseSchemaDictionarySelected,
        Dictionary<int, NetezzaTableInfo> databasesTablesSelected,
        List<NetezzaColumnInfoRow> selectedColumns,
        string selectedConnectionName)
    {
        if (isFirstWordDatabase && ownersForSelectedConnection?.TryGetValue(firstWord, out var owners) == true)
        {
            foreach (var owner in owners.Keys)
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2($"{firstWord}.{owner}"), CompletionIconKind.Schema, "Schema");
        }
        else if (ownersForSelectedConnectionSelectedDatabase?.ContainsKey(firstWord) == true
                 && databaseSchemaDictionarySelected.TryGetValue(_completionContext.SelectedDatabase, out var dbTables))
        {
            var candidates = dbTables
                .Where(arg => arg.Value.owner == firstWord && arg.Key.Contains(end, StringComparison.OrdinalIgnoreCase))
                .Select(arg => (arg.Key, TableId: arg.Value.tableId));

            int candidateCount = _cacheText2 is null || !text.StartsWith(_cacheText2, StringComparison.OrdinalIgnoreCase)
                ? candidates.Count()
                : 0;

            if (candidateCount < 300)
            {
                if (_cacheText2 is null || !text.StartsWith(_cacheText2, StringComparison.OrdinalIgnoreCase))
                {
                    _cacheText2 = text;
                    DynamicCollectionForNettezaHelpers.CacheList2 = candidates
                        .Select(arg => (arg.Key, TryGetTableDesc(databasesTablesSelected, arg.TableId, out var d) ? d : ""))
                        .ToList();
                }

                foreach (var (hint, description) in DynamicCollectionForNettezaHelpers.CacheList2)
                    yield return Table($"{firstWord}.{hint}", description);
            }
        }
        else if (_completionContext.DatabaseSchemaLookup.TryGetValue(selectedConnectionName, out var r1)
                 && r1.TryGetValue(_completionContext.SelectedDatabase, out var r2)
                 && r2.TryGetValue(firstWord, out var value)
                 && databasesTablesSelected.TryGetValue(value.tableId, out var tableInfo))
        {
            int firstColumnId = tableInfo.FIRST_COLUMN_ID;
            int columnCount = tableInfo.COLUMN_COUNT;
            for (int i = 0; i < columnCount; i++)
            {
                int columnId = firstColumnId + i;
                yield return Column($"{firstWord}.{selectedColumns[columnId].COLUMN_NAME}", selectedColumns[columnId]);
            }
        }
    }

    private IEnumerable<AutocompleteItem> YieldTwoDots(
        string text, string firstWord, string secondWord, string end, string textUpToLastDot,
        bool isFirstWordDatabase,
        Dictionary<string, Dictionary<string, string>> ownersForSelectedConnection,
        Dictionary<string, Dictionary<string, (string owner, int tableId)>> databaseSchemaDictionarySelected,
        Dictionary<int, NetezzaTableInfo> databasesTablesSelected,
        List<NetezzaColumnInfoRow> selectedColumns)
    {
        if (isFirstWordDatabase
            && databaseSchemaDictionarySelected.TryGetValue(firstWord, out var tmp))
        {
            IEnumerable<(string hint, string description)> popCandidate;

            if (ownersForSelectedConnection[firstWord].ContainsKey(secondWord))
            {
                popCandidate = tmp.Where(arg => arg.Key.Contains(end, StringComparison.OrdinalIgnoreCase)
                        && arg.Value.owner.Equals(secondWord, StringComparison.OrdinalIgnoreCase))
                    .Select(arg => ($"{textUpToLastDot}.{arg.Key}",
                        TryGetTableDesc(databasesTablesSelected, arg.Value.tableId, out var d) ? d : ""));
            }
            else
            {
                popCandidate = tmp.Where(arg => arg.Key.Contains(end, StringComparison.OrdinalIgnoreCase))
                    .Select(arg => ($"{textUpToLastDot}.{arg.Key}",
                        TryGetTableDesc(databasesTablesSelected, arg.Value.tableId, out var d) ? d : ""));
            }

            int candidateCount = _cacheText1 is null || !text.StartsWith(_cacheText1, StringComparison.OrdinalIgnoreCase)
                ? popCandidate.Count()
                : 0;

            if (candidateCount < 1000)
            {
                if (_cacheText1 is null || !text.StartsWith(_cacheText1, StringComparison.OrdinalIgnoreCase)
                    || DynamicCollectionForNettezaHelpers.CacheList1.Count == 0)
                {
                    _cacheText1 = text;
                    DynamicCollectionForNettezaHelpers.CacheList1 = popCandidate.ToList();
                }

                foreach (var (hint, description) in DynamicCollectionForNettezaHelpers.CacheList1)
                    yield return Table(hint, description);
            }
        }
        else if (ownersForSelectedConnection.ContainsKey(firstWord)
                 && databaseSchemaDictionarySelected.TryGetValue(_completionContext.SelectedDatabase, out var dbTables)
                 && dbTables.TryGetValue(secondWord, out var thisTable1)
                 && databasesTablesSelected.TryGetValue(thisTable1.tableId, out var tableInfo))
        {
            int firstColumnId = tableInfo.FIRST_COLUMN_ID;
            int columnCount = tableInfo.COLUMN_COUNT;
            for (int i = 0; i < columnCount; i++)
            {
                int columnId = firstColumnId + i;
                yield return Column($"{textUpToLastDot}.{selectedColumns[columnId].COLUMN_NAME}", selectedColumns[columnId]);
            }
        }
    }

    private static IEnumerable<AutocompleteItem> YieldThreePlusDots(
        string text, string firstWord, string textUpToLastDot,
        Dictionary<string, Dictionary<string, (string owner, int tableId)>> databaseSchemaDictionarySelected,
        Dictionary<int, NetezzaTableInfo> databasesTablesSelected,
        List<NetezzaColumnInfoRow> selectedColumns)
    {
        if (!databaseSchemaDictionarySelected.TryGetValue(firstWord, out var dbTables))
            yield break;

        string table = text[..text.LastDot()];
        int l = table.LastDot();
        table = table[(l + 1)..];

        if (!dbTables.TryGetValue(table, out var thisTable)
            || !databasesTablesSelected.TryGetValue(thisTable.tableId, out var tmpTab))
            yield break;

        int firstColumnId = tmpTab.FIRST_COLUMN_ID;
        int columnCount = tmpTab.COLUMN_COUNT;

        for (int i = 0; i < columnCount; i++)
        {
            int columnId = firstColumnId + i;
            yield return Column($"{textUpToLastDot}.{selectedColumns[columnId].COLUMN_NAME}", selectedColumns[columnId]);
        }
    }

    private static bool TryGetTableDesc(
        Dictionary<int, NetezzaTableInfo> databasesTablesSelected,
        int tableId,
        out string desc)
    {
        desc = null;
        return databasesTablesSelected is not null
            && databasesTablesSelected.TryGetValue(tableId, out var tableInfo)
            && (desc = tableInfo.TABLE_DESC) is not null;
    }

    private static AutocompleteItem Table(string label, string description)
    {
        var item = new MethodAutocompleteItem2(label)
        {
            ToolTipTitle = "Table",
            ToolTipText = description
        };
        return CompletionItemAppearance.Apply(item, CompletionIconKind.Table, "Table", description);
    }

    private static AutocompleteItem Column(string label, NetezzaColumnInfoRow column)
    {
        var item = new MethodAutocompleteItem2(label);
        return CompletionItemAppearance.Apply(
            item,
            CompletionIconKind.Column,
            column?.DATA_TYPE ?? "Column",
            column?.COLUMN_DESCRIPTION);
    }
}
