using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

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
    private readonly IConnectionSessionRegistry? _connectionSessions;
    private static readonly Regex Db2TableReferenceRegex = new(
        @"\b(?:FROM|JOIN)\s+(?<name>(?:[\w$""]+\.){1,2}[\w$""]+)(?:\s+(?:AS\s+)?(?<alias>[A-Za-z_][\w$]*))?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private string _cacheText1;
    private string _cacheText2;
    private List<(string hint, string description)> _cacheList1 = [];
    private List<(string hint, string description)> _cacheList2 = [];

    public LegacyDbCompletionFallback(
        INetezzaCompletionContext completionContext,
        IGeneralDbService generalDbService,
        INetezzaSchemaTableCatalog schemaTables,
        IConnectionSessionRegistry? connectionSessions = null)
    {
        _completionContext = completionContext ?? throw new ArgumentNullException(nameof(completionContext));
        _generalDbService = generalDbService;
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
        _connectionSessions = connectionSessions;
    }

    public void ResetCache()
    {
        _cacheText1 = null;
        _cacheText2 = null;
        _cacheList1.Clear();
        _cacheList2.Clear();
    }

    public IEnumerable<AutocompleteItem> GetCompletions(string text)
    {
        if (!_completionContext.SchemaRefreshed)
            yield break;

        if (_generalDbService.DriverName(_completionContext.SelectedConnectionName) != "NetezzaSQL")
            yield break;

        string selectedConnectionName = _completionContext.SelectedConnectionName;

        if (!_completionContext.DatabaseDictionary.TryGetValue(selectedConnectionName, out var selectedDatabases))
            yield break;
        string[] selectedDatabaseList = selectedDatabases.Values.Select(database => database.DatabaseName).ToArray();

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

    /// <summary>Completion fallback scoped to the editor tab's connection/database.</summary>
    public IEnumerable<AutocompleteItem> GetCompletions(
        string text,
        string connectionName,
        string databaseName,
        string? sql = null)
    {
        if (_generalDbService.DriverName(connectionName).Equals("DB2", StringComparison.OrdinalIgnoreCase))
        {
            foreach (AutocompleteItem item in GetDb2Completions(text, connectionName, databaseName, sql))
                yield return item;
            yield break;
        }

        foreach (AutocompleteItem item in GetCompletions(text))
            yield return item;
    }

    private IEnumerable<AutocompleteItem> GetDb2Completions(
        string text,
        string connectionName,
        string databaseName,
        string? sql)
    {
        if (_connectionSessions is null
            || !_connectionSessions.TryGetValue(connectionName, out IGeneralDb? database)
            || database.DatabaseType != DatabaseTypeEnum.DB2)
            yield break;

        Dictionary<string, Dictionary<string, TypeInDatabase>> catalog = database.objectInSchema;
        string[] parts = text.Split('.', StringSplitOptions.None);
        int dotCount = Math.Max(0, parts.Length - 1);

        // The column qualifier is often an alias (for example, A. in
        // FROM JBL_LIVE.JBL_DEPARTMENTS A). Resolve it against the statement
        // and use the same live DB2 column lookup as schema.table.
        if (text.EndsWith(".", StringComparison.Ordinal)
            && TryResolveDb2Alias(sql, text[..^1], databaseName, out string aliasDatabase,
                out string aliasSchema, out string aliasObject)
            && catalog.TryGetValue(aliasSchema, out Dictionary<string, TypeInDatabase>? aliasObjects)
            && aliasObjects.TryGetValue(aliasObject, out TypeInDatabase aliasKind)
            && aliasKind is TypeInDatabase.table or TypeInDatabase.view
                or TypeInDatabase.synonym or TypeInDatabase.db2alias or TypeInDatabase.db2nickname)
        {
            foreach (string column in database.GetColumns(aliasDatabase, aliasSchema, aliasObject)
                         .Where(column => !string.IsNullOrWhiteSpace(column)))
            {
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2($"{text[..^1]}.{column}"),
                    CompletionIconKind.Column,
                    "Column");
            }

            yield break;
        }

        if (dotCount == 0)
        {
            foreach (string schema in catalog.Keys
                         .Where(schema => schema.Contains(text, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(schema => schema, StringComparer.OrdinalIgnoreCase))
            {
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2(schema), CompletionIconKind.Schema, "Schema");
            }

            if (text.Length > 0)
            {
                foreach ((string schema, Dictionary<string, TypeInDatabase> objects) in catalog)
                {
                    foreach ((string name, TypeInDatabase kind) in objects
                                 .Where(item => item.Key.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return CreateDb2ObjectCompletion(name, kind, schema);
                    }
                }
            }

            yield break;
        }

        int schemaPartIndex = 0;
        if (dotCount >= 2
            && parts[0].Equals(databaseName, StringComparison.OrdinalIgnoreCase))
        {
            schemaPartIndex = 1;
        }

        string schemaName = parts[schemaPartIndex];
        string objectPrefix = parts[^1];

        if (!catalog.TryGetValue(schemaName, out Dictionary<string, TypeInDatabase>? schemaObjects))
            yield break;

        // schema.object. -> columns of the selected DB2 object
        int objectPartIndex = schemaPartIndex + 1;
        if (parts.Length > objectPartIndex + 1 && parts[^1].Length == 0)
        {
            string objectName = parts[objectPartIndex];
            if (schemaObjects.TryGetValue(objectName, out TypeInDatabase objectKind)
                && objectKind is TypeInDatabase.table or TypeInDatabase.view
                    or TypeInDatabase.synonym or TypeInDatabase.db2alias or TypeInDatabase.db2nickname)
            {
                string[] columns = database.GetColumns(databaseName, schemaName, objectName);
                foreach (string column in columns.Where(column => !string.IsNullOrWhiteSpace(column)))
                {
                    yield return CompletionItemAppearance.Apply(
                        new MethodAutocompleteItem2($"{schemaName}.{objectName}.{column}"),
                        CompletionIconKind.Column,
                        "Column");
                }
            }

            yield break;
        }

        foreach ((string name, TypeInDatabase kind) in schemaObjects
                     .Where(item => item.Key.StartsWith(objectPrefix, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            yield return CreateDb2ObjectCompletion($"{schemaName}.{name}", kind, schemaName);
        }
    }

    private static bool TryResolveDb2Alias(
        string? sql,
        string qualifier,
        string selectedDatabase,
        out string database,
        out string schema,
        out string objectName)
    {
        database = selectedDatabase;
        schema = string.Empty;
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(qualifier))
            return false;

        MatchCollection references = Db2TableReferenceRegex.Matches(sql);
        foreach (Match reference in references)
        {
            string alias = reference.Groups["alias"].Value;
            if (!alias.Equals(qualifier, StringComparison.OrdinalIgnoreCase))
                continue;

            string[] nameParts = reference.Groups["name"].Value
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim().Trim('"'))
                .ToArray();
            if (nameParts.Length == 2)
            {
                schema = nameParts[0];
                objectName = nameParts[1];
            }
            else if (nameParts.Length == 3)
            {
                database = nameParts[0];
                schema = nameParts[1];
                objectName = nameParts[2];
            }

            if (schema.Length > 0 && objectName.Length > 0)
                return true;
        }

        return false;
    }

    private static AutocompleteItem CreateDb2ObjectCompletion(
        string name,
        TypeInDatabase kind,
        string schema)
    {
        CompletionIconKind icon = kind switch
        {
            TypeInDatabase.view => CompletionIconKind.View,
            TypeInDatabase.procedure or TypeInDatabase.function => CompletionIconKind.Function,
            TypeInDatabase.synonym or TypeInDatabase.db2alias or TypeInDatabase.db2nickname => CompletionIconKind.Alias,
            _ => CompletionIconKind.Table
        };
        return CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2(name),
            icon,
            kind.ToString(),
            schema);
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
                    _cacheList2 = candidates
                        .Select(arg => (arg.Key, TryGetTableDesc(databasesTablesSelected, arg.TableId, out var d) ? d : ""))
                        .ToList();
                }

                foreach (var (hint, description) in _cacheList2)
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
                    || _cacheList1.Count == 0)
                {
                    _cacheText1 = text;
                    _cacheList1 = popCandidate.ToList();
                }

                foreach (var (hint, description) in _cacheList1)
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
