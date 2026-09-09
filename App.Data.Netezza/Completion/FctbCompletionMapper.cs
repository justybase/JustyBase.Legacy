using AppBase.Common;
using FastColoredTextBoxNS;
using JustyBase.Core.Database;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Visitor;
using System.Text.RegularExpressions;

namespace AppBase.Data.Completion;

/// <summary>
/// Maps parser CompletionItem to FCTB AutocompleteItem with qualified paths
/// (database..table, schema.table) so MethodAutocompleteItem2.Compare works.
/// </summary>
public static class FctbCompletionMapper
{
    private const int MaxDoubleDotTables = 500;
    private const string QuotedIdentifierPattern = "\"(?:\"\"|[^\"])*\"";
    private const string IdentifierPattern = "(?:" + QuotedIdentifierPattern + "|[A-Za-z_][\\w$]*)";
    private static readonly Regex RelationStartRegex = new(
        @"\b(?:FROM|JOIN)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RelationCompletionContextRegex = new(
        @"(?:\b(?:FROM|JOIN|UPDATE|INTO|MERGE\s+INTO|DELETE\s+FROM)\s+)" +
        "(?:" + IdentifierPattern + @"(?:\s*\.\s*" + IdentifierPattern + @")*\s*(?:\.\s*)?)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Keeps schemas above tables while completing a relation reference. DB2
    /// exposes tables from several schemas in one flat completion list, so the
    /// schema choice must be the first navigation level after FROM/JOIN.
    /// </summary>
    public static IReadOnlyList<AutocompleteItem> PrioritizeSchemasForRelationContext(
        IReadOnlyList<AutocompleteItem> items,
        string sql,
        int cursorOffset)
    {
        if (items is null || items.Count < 2 || !IsRelationCompletionContext(sql, cursorOffset))
            return items ?? [];

        return items
            .Select((item, index) => (item, index))
            .OrderBy(pair => Db2RelationCompletionPriority(pair.item))
            .ThenBy(pair => pair.index)
            .Select(pair => pair.item)
            .ToArray();
    }

    private static int Db2RelationCompletionPriority(AutocompleteItem item)
    {
        if (item.ImageIndex == (int)CompletionIconKind.Schema)
            return 0;
        if (item.ImageIndex == (int)CompletionIconKind.Table)
            return 1;
        if (item.ImageIndex == (int)CompletionIconKind.View)
            return 2;
        if (string.Equals(item.DetailText, "db2nickname", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.DetailText, "nickname", StringComparison.OrdinalIgnoreCase))
            return 3;

        return 4;
    }

    private static bool IsRelationCompletionContext(string sql, int cursorOffset)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        int safeOffset = Math.Clamp(cursorOffset, 0, sql.Length);
        return RelationCompletionContextRegex.IsMatch(sql[..safeOffset]);
    }
    /// <summary>
    /// Fast path for Netezza <c>database..table</c> — schema lookup only, deduped by table name.
    /// </summary>
    public static List<AutocompleteItem> MapDatabaseDoubleDotTables(
        string fragmentText,
        ISchemaProvider schema,
        NetezzaSchemaSnapshot metadata = null)
    {
        if (schema is null || !TryParseDatabaseDoubleDot(fragmentText, out var database, out var tablePrefix))
            return null;

        var tables = schema.GetTableNames(database, null);
        if (tables is null || tables.Count == 0)
            return null;

        int dd = fragmentText.IndexOf("..", StringComparison.Ordinal);
        string qualifyPrefix = fragmentText[..(dd + 2)];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AutocompleteItem>(Math.Min(tables.Count, MaxDoubleDotTables));

        foreach (var (name, kind) in tables)
        {
            if (!seen.Add(name))
                continue;

            if (tablePrefix.Length > 0
                && !name.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var tableMetadata = metadata?.Tables.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                t.Database?.Equals(database, StringComparison.OrdinalIgnoreCase) == true);
            var icon = kind == TableKind.View ? CompletionIconKind.View : CompletionIconKind.Table;
            var item = CompletionItemAppearance.Apply(
                new MethodAutocompleteItem2(qualifyPrefix + name),
                icon,
                kind == TableKind.View ? "View" : "Table",
                tableMetadata?.Description);
            item.ToolTipTitle = kind == TableKind.View ? "View" : "Table";
            item.ToolTipText = tableMetadata?.Description;
            result.Add(item);

            if (result.Count >= MaxDoubleDotTables)
                break;
        }

        if (result.Count == 0)
            return null;

        result.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public static IEnumerable<AutocompleteItem> MapEngineItems(
        IReadOnlyList<CompletionItem> engineItems,
        string fragmentText,
        ISchemaProvider schema,
        NetezzaSchemaSnapshot metadata = null,
        string sql = null)
    {
        if (TryParseDatabaseDoubleDot(fragmentText, out _, out _))
            yield break;

        var databaseFilter = TryGetDatabaseAfterDoubleDot(fragmentText);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ci in engineItems)
        {
            if (databaseFilter is not null
                && ci.Kind is CompletionKind.Table or CompletionKind.View or CompletionKind.ExternalTable
                && schema is not null
                && !TableBelongsToDatabase(schema, ci.Label, databaseFilter))
                continue;

            if (ci.Kind is CompletionKind.Table or CompletionKind.View or CompletionKind.ExternalTable)
            {
                if (!seen.Add(ci.Label))
                    continue;
            }
            else
            {
                var qualified = QualifyLabel(ci.Label, fragmentText);
                if (!seen.Add(qualified))
                    continue;
            }

            foreach (var item in MapSingle(ci, fragmentText, schema, metadata, sql))
                yield return item;
        }
    }

    /// <summary>Maps a neutral live-database word-list item without changing its insert text.</summary>
    public static AutocompleteItem MapWordListItem(SqlWordListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CompletionIconKind icon = item.Kind switch
        {
            SqlWordListKind.Table => CompletionIconKind.Table,
            SqlWordListKind.View => CompletionIconKind.View,
            SqlWordListKind.Column => CompletionIconKind.Column,
            SqlWordListKind.Database => CompletionIconKind.Database,
            SqlWordListKind.Schema => CompletionIconKind.Schema,
            SqlWordListKind.Function or SqlWordListKind.Procedure => CompletionIconKind.Function,
            SqlWordListKind.With => CompletionIconKind.Cte,
            SqlWordListKind.TempTable => CompletionIconKind.Table,
            SqlWordListKind.Subquery => CompletionIconKind.Reference,
            SqlWordListKind.Alias => CompletionIconKind.Alias,
            SqlWordListKind.Snippet => CompletionIconKind.Snippet,
            SqlWordListKind.DataType => CompletionIconKind.DataType,
            SqlWordListKind.Variable => CompletionIconKind.Variable,
            _ => CompletionIconKind.Keyword
        };

        AutocompleteItem result = icon is CompletionIconKind.Keyword
            or CompletionIconKind.DataType
            or CompletionIconKind.Variable
            ? new AutocompleteItem(item.Label)
            : new MethodAutocompleteItem2(item.Label);

        return CompletionItemAppearance.Apply(
            result,
            icon,
            item.Detail ?? item.Kind.ToString(),
            item.Description);
    }

    private static IEnumerable<AutocompleteItem> MapSingle(
        CompletionItem ci,
        string fragmentText,
        ISchemaProvider schema,
        NetezzaSchemaSnapshot metadata,
        string sql)
    {
        if (ci.Kind is CompletionKind.Snippet)
        {
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem2(QualifyLabel(ci.Label, fragmentText)),
                CompletionIconKind.Snippet,
                "Snippet");
            yield break;
        }

        if (ci.Kind is CompletionKind.Column or CompletionKind.Table or CompletionKind.View
            or CompletionKind.ExternalTable
            or CompletionKind.Schema or CompletionKind.Database or CompletionKind.Cte
            or CompletionKind.Alias or CompletionKind.Function)
        {
            var item = new MethodAutocompleteItem2(QualifyLabel(ci.Label, fragmentText));
            if (ci.Kind is CompletionKind.Table or CompletionKind.View or CompletionKind.ExternalTable)
            {
                var table = metadata?.Tables.FirstOrDefault(t =>
                    t.Name.Equals(ci.Label, StringComparison.OrdinalIgnoreCase));
                var detail = ci.Kind switch
                {
                    CompletionKind.View => "View",
                    CompletionKind.ExternalTable => "External",
                    _ => "Table"
                };
                string description = string.IsNullOrWhiteSpace(ci.Documentation)
                    ? table?.Description
                    : ci.Documentation;
                item.ToolTipTitle = detail;
                item.ToolTipText = description;
                CompletionItemAppearance.Apply(
                    item,
                    ci.Kind == CompletionKind.View ? CompletionIconKind.View : CompletionIconKind.Table,
                    detail,
                    description);
            }
            else if (ci.Kind == CompletionKind.Column)
            {
                var column = FindColumn(schema, metadata, ci.Label, ci.Detail, sql);
                string columnDetail = column?.DataType
                    ?? (ci.Detail?.Contains('.') == true ? null : ci.Detail)
                    ?? ci.Kind.ToString();
                string description = string.IsNullOrWhiteSpace(ci.Documentation)
                    ? column?.Description
                    : ci.Documentation;
                item.ToolTipTitle = columnDetail;
                item.ToolTipText = description;
                CompletionItemAppearance.Apply(
                    item,
                    CompletionIconKind.Column,
                    columnDetail,
                    description);
            }
            else
            {
                item.ToolTipTitle = ci.Detail ?? ci.Kind.ToString();
                CompletionItemAppearance.ApplyKind(item, ci.Kind, ci.Detail);
            }

            yield return item;
            yield break;
        }

        var fallback = CompletionItemAppearance.ApplyKind(
            new AutocompleteItem(ci.Label),
            ci.Kind,
            ci.Detail ?? ci.Kind.ToString());
        fallback.ToolTipTitle = ci.Detail;
        yield return fallback;
    }

    private static (string DataType, string Description)? FindColumn(
        ISchemaProvider schema,
        NetezzaSchemaSnapshot metadata,
        string columnName,
        string detail,
        string sql)
    {
        if (schema is null)
            return null;

        // Detail is normally "alias.COLUMN" or "table.COLUMN".
        int dot = detail?.LastIndexOf('.') ?? -1;
        if (dot > 0)
        {
            var qualifier = detail[..dot];
            var table = schema.GetTable(null, null, qualifier);
            var exact = table?.Columns?.FirstOrDefault(c =>
                c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                var documented = metadata?.Tables
                    .FirstOrDefault(t => t.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase))?
                    .Columns?.FirstOrDefault(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return (documented?.DataType ?? exact.DataType, documented?.Description ?? exact.Description);
            }

            string resolvedTableName = ResolveRelationTableName(sql, qualifier);
            if (!string.IsNullOrEmpty(resolvedTableName))
            {
                var resolvedMetadata = metadata?.Tables.FirstOrDefault(t =>
                    t.Name.Equals(resolvedTableName, StringComparison.OrdinalIgnoreCase));
                var resolvedColumn = resolvedMetadata?.Columns?.FirstOrDefault(c =>
                    c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                if (resolvedColumn is not null)
                    return (resolvedColumn.DataType, resolvedColumn.Description);

                var resolvedTable = schema.GetTable(null, null, resolvedTableName);
                var schemaColumn = resolvedTable?.Columns?.FirstOrDefault(c =>
                    c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                if (schemaColumn is not null)
                {
                    var documented = resolvedMetadata?.Columns?.FirstOrDefault(c =>
                        c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                    return (documented?.DataType ?? schemaColumn.DataType, documented?.Description ?? schemaColumn.Description);
                }
            }
        }

        // Aliases do not exist in the schema snapshot. Resolve an unambiguous
        // column from loaded tables so aliases still show type and description.
        (string DataType, string Description)? match = null;
        foreach (var candidate in metadata?.Tables.SelectMany(t => t.Columns ?? []) ?? [])
        {
            if (!candidate.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (match is not null)
                return null;

            match = (candidate.DataType, candidate.Description);
        }

        return match;
    }

    private static string ResolveRelationTableName(string sql, string qualifier)
    {
        if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(qualifier))
            return null;

        string unquotedQualifier = UnquoteIdentifier(qualifier);
        foreach ((string tableName, string alias) in EnumerateRelations(sql))
        {
            if (tableName.Equals(unquotedQualifier, StringComparison.OrdinalIgnoreCase)
                || UnquoteIdentifier(alias).Equals(unquotedQualifier, StringComparison.OrdinalIgnoreCase))
                return tableName;
        }

        return null;
    }

    private static IEnumerable<(string TableName, string Alias)> EnumerateRelations(string sql)
    {
        foreach (Match match in RelationStartRegex.Matches(sql))
        {
            int index = match.Index + match.Length;
            SkipWhitespace(sql, ref index);
            if (!TryReadQualifiedRelation(sql, ref index, out string tableName))
                continue;

            SkipWhitespace(sql, ref index);
            string alias = string.Empty;
            if (TryReadIdentifier(sql, ref index, out string candidate))
            {
                if (candidate.Equals("AS", StringComparison.OrdinalIgnoreCase))
                {
                    SkipWhitespace(sql, ref index);
                    TryReadIdentifier(sql, ref index, out alias);
                }
                else if (!IsRelationBoundary(candidate))
                {
                    alias = candidate;
                }
            }

            yield return (tableName, alias);
        }
    }

    private static bool TryReadQualifiedRelation(string sql, ref int index, out string tableName)
    {
        tableName = null;
        var parts = new List<string>();
        if (!TryReadIdentifier(sql, ref index, out string firstPart))
            return false;

        parts.Add(firstPart);
        while (parts.Count < 3)
        {
            int afterPart = index;
            SkipWhitespace(sql, ref index);
            if (index >= sql.Length || sql[index] != '.')
            {
                index = afterPart;
                break;
            }

            index++;
            SkipWhitespace(sql, ref index);
            if (index < sql.Length && sql[index] == '.')
            {
                parts.Add(string.Empty);
                index++;
                SkipWhitespace(sql, ref index);
            }

            if (!TryReadIdentifier(sql, ref index, out string part))
                return false;

            parts.Add(part);
        }

        tableName = UnquoteIdentifier(parts[^1]);
        return true;
    }

    private static bool TryReadIdentifier(string sql, ref int index, out string identifier)
    {
        identifier = null;
        if (index >= sql.Length)
            return false;

        if (sql[index] == '"')
            return TryReadQuotedIdentifier(sql, ref index, out identifier);

        if (!(char.IsLetter(sql[index]) || sql[index] == '_'))
            return false;

        int start = index++;
        while (index < sql.Length
            && (char.IsLetterOrDigit(sql[index]) || sql[index] is '_' or '$'))
        {
            index++;
        }

        identifier = sql[start..index];
        return true;
    }

    private static bool TryReadQuotedIdentifier(string sql, ref int index, out string identifier)
    {
        identifier = null;
        int start = index++;
        while (index < sql.Length)
        {
            if (sql[index] != '"')
            {
                index++;
                continue;
            }

            if (index + 1 < sql.Length && sql[index + 1] == '"')
            {
                index += 2;
                continue;
            }

            index++;
            identifier = sql[start..index];
            return true;
        }

        return false;
    }

    private static void SkipWhitespace(string sql, ref int index)
    {
        while (index < sql.Length && char.IsWhiteSpace(sql[index]))
            index++;
    }

    private static bool IsRelationBoundary(string identifier)
        => identifier.Equals("JOIN", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("ON", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("WHERE", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("GROUP", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("ORDER", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("HAVING", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("LIMIT", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("UNION", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("INNER", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("LEFT", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("RIGHT", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("FULL", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("CROSS", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("OUTER", StringComparison.OrdinalIgnoreCase)
            || identifier.Equals("USING", StringComparison.OrdinalIgnoreCase);

    private static string UnquoteIdentifier(string value)
    {
        value = value?.Trim() ?? string.Empty;
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal)
            : value;
    }

    public static string QualifyLabel(string label, string fragmentText)
    {
        if (string.IsNullOrEmpty(label) || label.Contains('.'))
            return label;

        int lastDot = fragmentText.LastDot();
        if (lastDot < 0)
            return label;

        return fragmentText[..(lastDot + 1)] + label;
    }

    public static bool TryParseDatabaseDoubleDot(string fragmentText, out string database, out string tablePrefix)
    {
        database = null;
        tablePrefix = "";

        int dd = fragmentText.IndexOf("..", StringComparison.Ordinal);
        if (dd <= 0)
            return false;

        database = fragmentText[..dd];
        if (database.Contains('.'))
            return false;

        tablePrefix = fragmentText[(dd + 2)..];
        return true;
    }

    private static string TryGetDatabaseAfterDoubleDot(string fragmentText)
    {
        if (!TryParseDatabaseDoubleDot(fragmentText, out var database, out _))
            return null;

        return fragmentText.EndsWith("..", StringComparison.Ordinal) ? database : null;
    }

    private static bool TableBelongsToDatabase(ISchemaProvider schema, string tableName, string database)
    {
        var tables = schema.GetTableNames(database, null);
        if (tables is null)
            return true;

        return tables.Any(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
    }
}
