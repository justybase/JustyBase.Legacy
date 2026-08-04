using AppBase.Common;
using AppBase.Data;
using AppBase.Common.Interfaces;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS.Helpers;
using JustData.Application.Sql;
using JustyBase.NetezzaSqlParser.Authoring;
using SqlTypingPerfProbe = FastColoredTextBoxNS.Helpers.SqlTypingPerfProbe;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AppBase.Services.Helpers;

public partial class AutocompleteClass : IAutocompleteClass
{
    private readonly INetezzaCompletionContext _completionContext;
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private readonly IEditorHost _editorHost;
    private readonly IConnectionSessionRegistry _connectionSessions;
    private readonly INetezzaSchemaTableCatalog _schemaTables;
    private readonly INetezzaHelperService _netezzaHelperService;
    private readonly Action<string, string>? _onOneDatabaseAttached;

    public AutocompleteClass(
        INetezzaCompletionContext completionContext,
        IApplicationSettingsContext applicationSettingsContext,
        IEditorHost editorHost,
        IConnectionSessionRegistry connectionSessions,
        INetezzaSchemaTableCatalog schemaTables,
        INetezzaHelperService netezzaHelperService,
        Action<string, string>? onOneDatabaseAttached = null)
    {
        _completionContext = completionContext;
        _applicationSettingsContext = applicationSettingsContext;
        _editorHost = editorHost;
        _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
        _netezzaHelperService = netezzaHelperService ?? throw new ArgumentNullException(nameof(netezzaHelperService));
        _onOneDatabaseAttached = onOneDatabaseAttached;
    }

    public async Task AddAutocompleteForNZ(int selectionStart, string cleanSqlText)
    {
        if (_suggestionSearchInProgress)
        {
            return;
        }
        _suggestionSearchInProgress = true;
        long autocompleteStarted = Environment.TickCount64;
        bool skipDeepScan = false;
        int sqlLength = cleanSqlText?.Length ?? 0;

        try
        {
            if (_editorHost.ActualSuggestionList is not INetezzaAutocompleteSource)
            {
                return;
            }

        INetezzaAutocompleteSource dynamicCollectionNz = (INetezzaAutocompleteSource)_editorHost.ActualSuggestionList;
        var additionalDataWith = _editorHost.AdditionalDataWith;
        var additionalTabletData = _editorHost.AdditionalTabletData;

        int selStart = selectionStart;
        skipDeepScan = SqlPerformancePolicy.ShouldSkipDeepAutocompleteScan(-1, sqlLength)
            || SqlPerformancePolicy.ExceedsLineThreshold(cleanSqlText, SqlPerformancePolicy.HugeScriptLineThreshold);

        if (skipDeepScan)
            return;

        // Cap lookback so typing at the end of a huge single-statement script
        // does not walk/allocate the entire document on every autocomplete tick.
        string scanSql = cleanSqlText ?? string.Empty;
        int scanStart = selStart;
        if (sqlLength > SqlPerformancePolicy.AutocompleteLookbackCharLimit)
        {
            int windowStart = Math.Max(0, selStart - SqlPerformancePolicy.AutocompleteLookbackCharLimit);
            int windowEnd = Math.Min(sqlLength, selStart + 4_096);
            scanSql = cleanSqlText!.Substring(windowStart, windowEnd - windowStart);
            scanStart = selStart - windowStart;
        }

        if (!skipDeepScan && (_stopwatchAfterTableSearch.ElapsedMilliseconds > _withTempTablesMinInterval || !_stopwatchAfterTableSearch.IsRunning)) // not more often than 1 second from the end
        {
            await Task.Run(() =>
            {
                if (selectionStart == -1)
                    return;
                if (MakeCteTask(additionalDataWith, scanStart, scanSql) != -1)
                {
                    List<string> ls = new List<string>();
                    foreach (var item in additionalDataWith)
                    {
                        ls.Add(item.Key);
                        ls.AddRange(item.Value.Select(arg => $"{item.Key}.{arg}"));
                    }
                    dynamicCollectionNz.HintWithTable = ls;
                }

                if (MakeTempTableHintsTask(additionalTabletData, scanSql) != -1)
                {
                    foreach (var item in additionalTabletData)
                    {
                        dynamicCollectionNz.HintWithTable.Add(item.Key);
                        dynamicCollectionNz.HintWithTable.AddRange(item.Value.Select(arg => $"{item.Key}.{arg}"));
                    }
                }
            });
            _stopwatchAfterTableSearch.Restart();
        }

        await Task.Run(async () =>
        {
            if (selectionStart == -1)
            {
                return;
            }

            string betweenParentheses = "";
            try
            {
                betweenParentheses = BetweenParenthesesOrBrackets(scanStart, scanSql);
            }
            catch (Exception)
            {

                return;
            }

            if (!betweenParentheses.Contains("select", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string query2 = betweenParentheses;
            int nrSelect2 = LastSelect(ref query2);
            if (nrSelect2 != -1)
            {
                string afterSelect = query2.Substring(nrSelect2 + "select".Length).Trim();
                int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

                if (nr > 0)
                {
                    string between = afterSelect.Substring(0, nr + 1).Trim();
                    var list1 = between.SqlSplit().Select(arg => arg.LastWord()).ToList();
                    dynamicCollectionNz.State.ReplaceActualColumns(list1);
                }
            }

            string basicSQL = TextAfterFromWithoutRegular2(betweenParentheses);
            if (basicSQL == "")
            {
                return;
            }
            betweenParentheses = _rxBracketsWithoutSelect.Replace(betweenParentheses, "");

            List<string> pieces = basicSQL.SqlSplit().ToList<string>().Select(arg => arg.Trim()).ToList<string>();

            List<(string hint, string description)> subquerySuggestions = new List<(string hint, string description)>();
            List<(string hint, string description)> subqueryAliases = new List<(string hint, string description)>();
            List<int> toRemove = new List<int>();//piece numbers = subqueries

            //handling subqueries
            for (int i = 0; i < pieces.Count; i++)
            {
                string piece = pieces[i];

                Match subquery = _rxSubquery.Match(piece);
                //Regex.Match(piece, subqueryMask, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!subquery.Success)//this is not a subquery //NO BRACKETS = NO POINT IN CONTINUING
                {
                    continue;
                }
                toRemove.Add(i);//remove so it doesn't go in "normal" mode

                //string aliasX = Regex.Match(subquery.Value, aliasMask).Value;//last word = alias
                string aliasX = _rxAlias.Match(subquery.Value).Value;//last word = alias

                //string query = Regex.Match(subquery.Value, maskaZap, RegexOptions.Singleline).Value;
                //static Regex rxZapytanie = new Regex(@"^\(.*\)", RegexOptions.Compiled | RegexOptions.Singleline);
                string query = _rxQuery.Match(subquery.Value).Value;


                query = query.Substring(1, query.Length - 2).Trim();
                int selectIndex = LastSelect(ref query);
                if (selectIndex == -1)
                {
                    continue;
                }

                string afterSelect = query.Substring(selectIndex + "select".Length).Trim();
                int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

                if (nr == -1)
                {
                    nr = afterSelect.Length - 1;
                }

                if (nr > 0)
                {
                    string between = afterSelect.Substring(0, nr + 1).Trim();
                    //PROBLEM
                    //between = rxNesting.Replace(between, ""); //removing all nesting
                    //between = Regex.Replace(between, @"\(.*\)", "", RegexOptions.Singleline); //removing all nesting

                    subquerySuggestions.AddRange(between.SqlSplit().Select(arg => ($"{aliasX}.{arg.LastWord()}", "")));
                    subqueryAliases.Add((aliasX, ""));
                }
            }

            dynamicCollectionNz.AliasHints.Clear();
            dynamicCollectionNz.AliasHints.AddRange(subqueryAliases);
            dynamicCollectionNz.AliasHints.AddRange(subquerySuggestions);

            int l = 0;
            foreach (var i in toRemove)
            {
                pieces.RemoveAt(i - l);
                l++;
            }

            Dictionary<string, List<string>> tableAliasDictionary = ExtractElements(pieces.ToArray());

            foreach (var tableAliasEntry in tableAliasDictionary)
            {
                //string table = tableAliasEntry.Key;
                string databaseTable = tableAliasEntry.Key;
                List<string> aliases = tableAliasEntry.Value;

                int m = aliases.Count;

                for (int j = 0; j < m; j++)
                {
                    dynamicCollectionNz.AliasHints.Add((aliases[j], ""));
                }

                if (additionalDataWith.TryGetValue(databaseTable, out List<string> value))
                {
                    var columnList = value;
                    int n = columnList.Count;

                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            dynamicCollectionNz.AliasHints.Add((aliases[j] + "." + columnList[i], ""));
                        }
                    }
                }

                if (additionalTabletData.TryGetValue(databaseTable, out List<string> value2))
                {
                    var columnList = value2;
                    int n = columnList.Count;
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            dynamicCollectionNz.AliasHints.Add((aliases[j] + "." + columnList[i], ""));
                        }
                    }
                }

                //string databaseTable = tableAliasEntry.Key;
                int nr1 = databaseTable.IndexOf('.');
                int nr2 = databaseTable.LastIndexOf('.');

                string database = default;
                string table = default;

                if (nr1 > 0 && nr2 > nr1) // database..table or database.owner.table
                {
                    database = databaseTable.Substring(0, nr1);
                    table = databaseTable.Substring(nr2 + 1);
                }
                else if (nr1 > 0 && nr1 == nr2) // owner.table
                {
                    database = _completionContext.SelectedDatabase;
                    table = databaseTable.Substring(nr1 + 1);
                }
                else if (nr1 == -1 && nr2 == -1) // table
                {
                    database = _completionContext.SelectedDatabase;
                    table = databaseTable;
                }
                else
                {
                    continue;
                }

                string selConnName = _completionContext.SelectedConnectionName;
                if (_completionContext.DatabaseSchemaLookup.TryGetValue(selConnName, out var value3)
                && value3.TryGetValue(database, out var value4))
                {
                    if (value4.ContainsKey(table))
                    {
                        if (!_netezzaHelperService.SqliteInProgress && _completionContext.SchemaRefreshed && _applicationSettingsContext.Config.RefreshMode != 1 && _connectionSessions.TryGetValue(selConnName, out var gdb) && gdb.DatabaseType == DatabaseTypeEnum.Netezza
                        && gdb is INetezza netezza)
                        {
                            if (netezza.AttachedDbsToSchema.Count != netezza.DatabasesCount && !netezza.AttachedDbsToSchema.ContainsKey(database)
                            && !netezza.IsDbInProgress(database)
                            )
                            {
                                bool success = false;
                                try
                                {
                                    success = await netezza.DownloadOneDb(selConnName, database);
                                }
                                catch (Exception)
                                {

                                    success = false;
                                }

                                if (success)
                                {
                                    _onOneDatabaseAttached?.Invoke(selConnName, database);
                                }
                            }
                        }

                        if (selConnName != _completionContext.SelectedConnectionName)
                        {
                            return;
                        }

                        if (value4.TryGetValue(table, out var tmpTbl))
                        {
                            int tableId = tmpTbl.tableId;
                            if (_schemaTables.TablesByConnection.TryGetValue(selConnName, out var hlp0) &&
                            hlp0.TryGetValue(tableId, out var hlp)
                            && _completionContext.ColumnTablesDictionary.TryGetValue(selConnName, out var hlp2))
                            {
                                int firstColumnId = hlp.FIRST_COLUMN_ID;
                                int columnCount = hlp.COLUMN_COUNT;

                                int n = aliases.Count;
                                for (int j = 0; j < n; j++)
                                {
                                    for (int i = 0; i < columnCount; i++)
                                    {
                                        int idKol = firstColumnId + i;
                                        if (idKol < 0 || idKol >= hlp2.Count)
                                            break;
                                        var tmp1 = hlp2[idKol];
                                        dynamicCollectionNz.AliasHints.Add((aliases[j] + "." + tmp1.COLUMN_NAME, $"{tmp1.DATA_TYPE}|{tmp1.COLUMN_DESCRIPTION}"));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            dynamicCollectionNz.AliasHints.Sort();

        });

        }
        finally
        {
            // #region agent perf
            SqlTypingPerfProbe.Instance.EnsureInitialized();
            SqlTypingPerfProbe.Instance.Emit(
                "autocomplete.nz_delayed",
                "end",
                Environment.TickCount64 - autocompleteStarted,
                meta: $"sel={selectionStart};chars={sqlLength};skipDeep={skipDeepScan}");
            // #endregion
            _suggestionSearchInProgress = false;
        }
    }

    public async Task AddAutocompleteForGeneral(int selectionStart, string cleanSqlText)
    {
        if (_suggestionSearchInProgress)
            return;

        _suggestionSearchInProgress = true;
        try
        {
            string connectionName = _completionContext.SelectedConnectionName;
            if (!_connectionSessions.TryGetValue(connectionName, out IGeneralDb? generalDatabase))
                return;
            IAutocompleteSuggestionStore suggestions = generalDatabase.AutocompleteSuggestions;
            int selStart = selectionStart;

            await Task.Run(() =>
            {
            if (selStart == -1)
            {
                return;
            }

            List<string> oneWordTemp = new List<string>();
            List<string> twoWordsTemp = new List<string>();

            Dictionary<string, List<string>> additionalWithTable = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> additionalTempTable = new Dictionary<string, List<string>>();

            if (_stopwatchAfterTableSearch.ElapsedMilliseconds > _withTempTablesMinInterval || !_stopwatchAfterTableSearch.IsRunning)
            {
                if (MakeCteTask(additionalWithTable, selStart, cleanSqlText) != -1)
                {
                    foreach (var item in additionalWithTable)
                    {
                        oneWordTemp.Add(item.Key);
                        twoWordsTemp.AddRange(item.Value.Select(arg => $"{item.Key}.{arg}"));
                    }
                }

                if (MakeTempTableHintsTask(additionalTempTable, cleanSqlText) != -1)
                {
                    foreach (var item in additionalTempTable)
                    {
                        oneWordTemp.Add(item.Key);
                        twoWordsTemp.AddRange(item.Value.Select(arg => $"{item.Key}.{arg}"));
                    }
                }
                _stopwatchAfterTableSearch.Restart();
            }

            string betweenParentheses = "";
            try
            {
                betweenParentheses = BetweenParenthesesOrBrackets(selStart, cleanSqlText);
            }
            catch (Exception)
            {
                return;
            }

            if (!betweenParentheses.Contains("select", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string query2 = betweenParentheses;
            int nrSelect2 = LastSelect(ref query2);
            if (nrSelect2 != -1)
            {
                string afterSelect = query2.Substring(nrSelect2 + "select".Length).Trim();
                int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

                if (nr > 0)
                {
                    string between = afterSelect.Substring(0, nr + 1).Trim();
                    var betweenSelectAndFrom = between.SqlSplit().Select(arg => arg.LastWord()).ToList();
                    suggestions.ActualColumnList.Clear();
                    suggestions.ActualColumnList = betweenSelectAndFrom;
                }
            }

            string basicSQL = TextAfterFromWithoutRegular2(betweenParentheses);
            if (basicSQL == "")
            {
                return;
            }

            betweenParentheses = _rxBracketsWithoutSelect.Replace(betweenParentheses, "");

            List<string> pieces = basicSQL.SqlSplit().ToList<string>().Select(arg => arg.Trim()).ToList<string>();
            List<string> subquerySuggestions = new List<string>();
            List<string> subqueryAliases = new List<string>();
            List<int> toRemove = new List<int>();//piece numbers = subqueries

            //handling subqueries
            for (int i = 0; i < pieces.Count; i++)
            {
                string piece = pieces[i];

                //always finds the first and widest subquery -> ((aaaaa)bbb) cc (xxxxxxxxxxxx) will find ((aaaaa)bbb) cc
                Match subquery = _rxSubquery.Match(piece);
                //Regex.Match(piece, subqueryMask, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!subquery.Success)//this is not a subquery //NO BRACKETS = NO POINT IN CONTINUING
                {
                    continue;
                }
                toRemove.Add(i);//remove so it doesn't go in "normal" mode

                //string aliasX = Regex.Match(subquery.Value, aliasMask).Value;//last word = alias
                string aliasX = _rxAlias.Match(subquery.Value).Value;//last word = alias

                //string query = Regex.Match(subquery.Value, maskaZap, RegexOptions.Singleline).Value;
                //static Regex rxZapytanie = new Regex(@"^\(.*\)", RegexOptions.Compiled | RegexOptions.Singleline);
                string query = _rxQuery.Match(subquery.Value).Value;

                query = query.Substring(1, query.Length - 2).Trim();
                int selectIndex = LastSelect(ref query);
                if (selectIndex == -1)
                {
                    continue;
                }

                string afterSelect = query.Substring(selectIndex + "select".Length).Trim();
                int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

                if (nr == -1)
                {
                    nr = afterSelect.Length - 1;
                }

                if (nr > 0)
                {
                    string between = afterSelect.Substring(0, nr + 1).Trim();
                    subquerySuggestions.AddRange(between.SqlSplit().Select(arg => $"{aliasX}.{arg.LastWord()}"));
                    subqueryAliases.Add(aliasX);
                }
            }

            int l = 0;
            foreach (var i in toRemove)
            {
                pieces.RemoveAt(i - l);
                l++;
            }

            Dictionary<string, List<string>> tableAliasDictionary = ExtractElements(pieces.ToArray());
            foreach (var tableAliasEntry in tableAliasDictionary)
            {
                //string table = tableAliasEntry.Key;
                string databaseTable = tableAliasEntry.Key;
                List<string> aliases = tableAliasEntry.Value;

                string[] cols = Array.Empty<string>();
                if (_connectionSessions.TryGetValue(connectionName, out IGeneralDb value) && value is not null)
                {
                    var gd = value;
                    int firstDot = databaseTable.FirstDot();
                    if (firstDot != -1)
                    {
                        string database = databaseTable.Substring(0, firstDot);
                        string table = databaseTable.Substring(firstDot + 1);
                        if (gd != null && gd.DatabaseType != DatabaseTypeEnum.MsSqlDb)
                        {
                            cols = gd.GetColumns("", database, table);
                        }
                        else if (gd != null)
                        {
                            int secDot = table.FirstDot();
                            if (secDot != -1)
                            {
                                cols = gd.GetColumns(database, table.Substring(0, secDot), table.Substring(secDot + 1));
                            }
                        }
                    }
                    else
                    {
                        string database = connectionName;
                        string table = databaseTable;
                        if (gd != null)
                        {
                            cols = gd.GetColumns("", database, table);
                        }
                        if (cols.Length == 0)
                        {
                            if (additionalWithTable.TryGetValue(table, out List<string> value3)) // try with
                            {
                                cols = value3.ToArray();
                            }
                            else if (additionalTempTable.TryGetValue(table, out List<string> value2))
                            {
                                cols = value2.ToArray();
                            }
                            // to do try temp table
                        }
                    }
                }
                oneWordTemp.AddRange(aliases);

                int m = aliases.Count;

                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        twoWordsTemp.Add($"{aliases[i]}.{cols[j]}");
                    }
                }
            }
            suggestions.OneWordAdditions.Clear();
            suggestions.OneWordAdditions = subqueryAliases;
            suggestions.OneWordAdditions.AddRange(oneWordTemp);
            suggestions.OneWordAdditions.Sort();
            suggestions.TwoWordsAdditions.Clear();
            suggestions.TwoWordsAdditions = subquerySuggestions;
            suggestions.TwoWordsAdditions.AddRange(twoWordsTemp);
            suggestions.TwoWordsAdditions.Sort();
            });
        }
        finally
        {
            _suggestionSearchInProgress = false;
        }
    }


    private static readonly Regex _rxSubquery = RegexSubquery();
    private static readonly Regex _rxQuery = RegexZapytanie();
    private static readonly Regex _rxAlias = RegexAlias();
    private static readonly Regex _rxWith = RegexWith();
    private static readonly Regex _rxTable = RegexTable();
    private static readonly Regex _rxSpacing = RegexSpacing();
    private static readonly Regex _rxBracketsWithoutSelect = RegexBracketsWithoutSelect();
    private static readonly Regex _rxExcessiveSpaces = RegexExcessiveSpaces();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex RegexExcessiveSpaces();

    [GeneratedRegex("\\(((?!(select|\\(|\\)|;)).)*\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "pl-PL")]
    private static partial Regex RegexBracketsWithoutSelect();

    [GeneratedRegex("(\\s|\\n)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexSpacing();

    [GeneratedRegex("\\b(create\\s+temp\\s+table|create\\s+table)\\s+(?<tableAlias>\\w+?)\\b\\s*as\\b\\s*\\({0,1}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexTable();

    [GeneratedRegex("^\\(.*\\)(\\s)+\\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "pl-PL")]
    private static partial Regex RegexSubquery();

    [GeneratedRegex("^\\(.*\\)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex RegexZapytanie();

    [GeneratedRegex("\\w+$", RegexOptions.Compiled)]
    private static partial Regex RegexAlias();

    [GeneratedRegex("\\b(with\\s+)?(?<tableAlias>\\w+?)\\b\\s*as\\b\\s*\\({0,1}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexWith();

    private string BetweenSemicolons(int position, string cleanSqlText)
        => SqlTextCursorParser.BetweenSemicolons(position, cleanSqlText);

    private string BetweenParenthesesOrBrackets(int position, string cleanSqlText)
        => SqlTextCursorParser.BetweenParenthesesOrBrackets(position, cleanSqlText);

    public int LastSelect(ref string inner, bool doTrim = true)
        => SqlTextCursorParser.LastSelect(ref inner, doTrim);

    public int FirstFrom(string afterSelect)
        => SqlTextCursorParser.FirstFrom(afterSelect);

    public int FirstWhereGroupLimit(string text)
        => SqlTextCursorParser.FirstWhereGroupLimit(text);

    private int FindClosingBracket(ref string sqlFragment, int start = 0)
        => SqlTextCursorParser.FindClosingBracket(sqlFragment, start);

    private string TextAfterFromWithoutRegular2(string inner)
    {
        int selectIndex = LastSelect(ref inner);

        if (selectIndex == -1)
        {
            return "";
        }

        string afterSelect = inner.Substring(selectIndex + "select".Length).Trim();
        int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

        StringBuilder sb = default;

        if (nr > 0)
        {
            string afterFrom = afterSelect.Substring(nr + "from".Length + 1).Trim();
            int contextEnd = FirstWhereGroupLimit(afterFrom);
            string text = afterFrom.Substring(0, contextEnd > 0 ? contextEnd : afterFrom.Length);


            //(\b|\s|\n)(inner|outer|cross|)(\b|\s|\n)
            // join -> ,
            int n = text.Length;
            sb = new StringBuilder(n);

            for (int i = 0; i < n; i++) // 1 because SQL doesn't start with such words
            {
                if (
                    (i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n' || text[i - 1] == '\r' || text[i - 1] == '(' || text[i - 1] == ')' || text[i - 1] == '\t')
                    && i < n - 3
                    && (text[i] == 'j' || text[i] == 'J')
                    && (text[i + 1] == 'o' || text[i + 1] == 'O')
                    && (text[i + 2] == 'i' || text[i + 2] == 'I')
                    && (text[i + 3] == 'n' || text[i + 3] == 'N')
                    //ostatna litera lub po spacji itp
                    && (i + 3 == n - 1 || text[i + 4] == ' ' || text[i + 4] == '\n' || text[i + 4] == '\r' || text[i + 4] == '(' || text[i + 4] == ')' || text[i + 4] == '\t')
                  )
                {
                    sb.Append(',');
                    i += 3;
                }
                else if ( //left, full
                    (i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n' || text[i - 1] == '\r' || text[i - 1] == '(' || text[i - 1] == ')' || text[i - 1] == '\t')
                    &&
                    (
                    i < n - 3
                    && (
                        (text[i] == 'l' || text[i] == 'L')
                        && (text[i + 1] == 'e' || text[i + 1] == 'E')
                        && (text[i + 2] == 'f' || text[i + 2] == 'F')
                        && (text[i + 3] == 't' || text[i + 3] == 'T')

                        ||

                        (text[i] == 'f' || text[i] == 'F')
                        && (text[i + 1] == 'u' || text[i + 1] == 'U')
                        && (text[i + 2] == 'l' || text[i + 2] == 'L')
                        && (text[i + 3] == 'l' || text[i + 3] == 'L')
                    )
                    //ostatna litera lub po spacji itp
                    && (i + 3 == n - 1 || text[i + 4] == ' ' || text[i + 4] == '\n' || text[i + 4] == '\r' || text[i + 4] == '(' || text[i + 4] == ')' || text[i + 4] == '\t')

                    )
                  )
                {
                    i += 3;
                }
                else if ( //as
                    (i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n' || text[i - 1] == '\r' || text[i - 1] == '(' || text[i - 1] == ')' || text[i - 1] == '\t')
                    &&
                    (
                    i < n - 1
                    && (
                        (text[i] == 'a' || text[i] == 'A')
                        && (text[i + 1] == 's' || text[i + 1] == 'S')
                    )
                    //ostatna litera lub po spacji itp
                    && (i + 1 == n - 1 || text[i + 2] == ' ' || text[i + 2] == '\n' || text[i + 2] == '\r' || text[i + 2] == '(' || text[i + 2] == ')' || text[i + 2] == '\t')

                    )
                  )
                {
                    i += 1;
                }
                else if ( // inner, outer, cross
                    (i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n' || text[i - 1] == '\r' || text[i - 1] == '(' || text[i - 1] == ')' || text[i - 1] == '\t')
                    &&
                    (
                    i < n - 4
                    && (
                        (text[i] == 'i' || text[i] == 'I')
                        && (text[i + 1] == 'n' || text[i + 1] == 'N')
                        && (text[i + 2] == 'n' || text[i + 2] == 'N')
                        && (text[i + 3] == 'e' || text[i + 3] == 'E')
                        && (text[i + 4] == 'r' || text[i + 4] == 'R')
                        ||
                        (text[i] == 'o' || text[i] == 'O')
                        && (text[i + 1] == 'u' || text[i + 1] == 'U')
                        && (text[i + 2] == 't' || text[i + 2] == 'T')
                        && (text[i + 3] == 'e' || text[i + 3] == 'E')
                        && (text[i + 4] == 'r' || text[i + 4] == 'R')
                        ||
                        (text[i] == 'c' || text[i] == 'C')
                        && (text[i + 1] == 'r' || text[i + 1] == 'R')
                        && (text[i + 2] == 'o' || text[i + 2] == 'O')
                        && (text[i + 3] == 's' || text[i + 3] == 'S')
                        && (text[i + 4] == 's' || text[i + 4] == 'S')
                    )
                    //ostatna litera lub po spacji itp
                    && (i + 4 == n - 1 || text[i + 5] == ' ' || text[i + 5] == '\n' || text[i + 5] == '\r' || text[i + 5] == '(' || text[i + 5] == ')' || text[i + 5] == '\t')

                    )
                  )
                {
                    i += 4;
                }
                else
                {
                    sb.Append(text[i]);
                }

            }

        }
        if (sb == null)
        {
            return "";
        }

        return sb.ToString();
    }

    private Dictionary<string, List<string>> ExtractElements(string[] pieces)
    {
        Dictionary<string, List<string>> tableAliasDictionary = new Dictionary<string, List<string>>();
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i] = _rxExcessiveSpaces.Replace(pieces[i].Trim('\n', '\r', ' '), " ");

            if (pieces[i].StartsWith(" ON ", StringComparison.OrdinalIgnoreCase) || pieces[i].StartsWith("ON ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int whereSpace = pieces[i].IndexOf(' ');
            if (whereSpace == -1)
            {
                continue;
            }
            string table = pieces[i].Substring(0, whereSpace);
            string alias = pieces[i].Substring(whereSpace + 1);
            int whereWhitespace = _rxSpacing.Match(alias).Index;

            if (whereWhitespace > 0)
            {
                alias = alias.Substring(0, whereWhitespace);
            }

            if (!tableAliasDictionary.ContainsKey(table))
            {
                tableAliasDictionary.Add(table, new List<string>());
            }
            tableAliasDictionary[table].Add(alias);
        }
        return tableAliasDictionary;
    }

    private void ProcessSqlSet(ref string withOrTableSet, Dictionary<string, List<string>> additionalTableColumn, char separator = ',', bool isTable = false)
    {
        foreach (string item in withOrTableSet.SqlSplit(separator))
        {
            string currentWithTable = item;//item.Clone() as string;
            Match mWithTable = default;
            if (!isTable)//with
            {
                mWithTable = _rxWith.Match(currentWithTable);
            }
            else//table / temp table
            {
                mWithTable = _rxTable.Match(currentWithTable);
            }


            if (!mWithTable.Success)
            {
                continue;
            }

            string withAlias = mWithTable.Groups["tableAlias"].Value;

            int bracketIndex = FindClosingBracket(ref currentWithTable, mWithTable.Index + mWithTable.Length);
            if (bracketIndex == -1)
            {
                bracketIndex = currentWithTable.IndexOf("distribute", mWithTable.Index + mWithTable.Length, StringComparison.OrdinalIgnoreCase);
                if (bracketIndex == -1)
                {
                    bracketIndex = currentWithTable.Length;
                }
            }
            if (bracketIndex == -1)
            {
                continue;
            }

            string query = currentWithTable.Substring(mWithTable.Index + mWithTable.Length, bracketIndex - (mWithTable.Index + mWithTable.Length));
            int selectIndex = LastSelect(ref query);
            if (selectIndex == -1)
            {
                continue;
            }
            string afterSelect = query.Substring(selectIndex + 6).Trim(); // 6 = "select".Length
            int nr = FirstFrom(afterSelect);// first top-level FROM after SELECT

            if (nr == -1)
            {
                nr = afterSelect.Length - 1;
            }

            if (nr > 0)
            {
                string between = afterSelect.Substring(0, nr + 1).Trim();

                if (!additionalTableColumn.ContainsKey(withAlias))
                {
                    additionalTableColumn[withAlias] = new List<string>();
                }

                additionalTableColumn[withAlias].AddRange(between.SqlSplit().Select(arg => arg.LastWord()));
            }
        }
    }

    private bool _suggestionSearchInProgress = false;

    private readonly Stopwatch _stopwatchAfterTableSearch = new Stopwatch();

    private const int _withTempTablesMinInterval = 500;


    private int MakeTempTableHintsTask(Dictionary<string, List<string>> additionalTempTable, string cleanSqlText)
    {
        additionalTempTable.Clear();
        string query = cleanSqlText;

        int selectWithPosition = LastSelect(ref query, doTrim: false);

        if (selectWithPosition != -1)
        {
            string tableSet = query.Substring(0, selectWithPosition);
            ProcessSqlSet(ref tableSet, additionalTempTable, ';', isTable: true);
        }
        return selectWithPosition;
    }

    private int MakeCteTask(Dictionary<string, List<string>> additionalWithTable, int selStart, string cleanSqlText)
    {
        string fromSemicolonToSemicolon = BetweenSemicolons(selStart, cleanSqlText);

        additionalWithTable.Clear();

        fromSemicolonToSemicolon = _rxBracketsWithoutSelect.Replace(fromSemicolonToSemicolon, "");

        int nrSelectGora = LastSelect(ref fromSemicolonToSemicolon);
        if (nrSelectGora != -1)
        {
            string withSet = fromSemicolonToSemicolon.Substring(0, nrSelectGora);
            ProcessSqlSet(ref withSet, additionalWithTable);
        }
        return nrSelectGora;
    }
}
