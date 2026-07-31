using AppBase.Common;
using FastColoredTextBoxNS;
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
    private static readonly Regex RelationAliasRegex = new(
        @"\b(?:FROM|JOIN)\s+(?<relation>[A-Za-z_][\w$]*(?:\s*\.\s*(?:\.\s*)?[A-Za-z_][\w$]*){0,2})(?:\s+(?:AS\s+)?(?<alias>[A-Za-z_][\w$]*))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        foreach (Match match in RelationAliasRegex.Matches(sql))
        {
            string relation = NormalizeQualifiedIdentifier(match.Groups["relation"].Value);
            string alias = match.Groups["alias"].Value;
            if (string.IsNullOrEmpty(relation))
                continue;

            string tableName = relation[(relation.LastIndexOf('.') + 1)..];
            if (tableName.Equals(qualifier, StringComparison.OrdinalIgnoreCase)
                || alias.Equals(qualifier, StringComparison.OrdinalIgnoreCase))
                return tableName;
        }

        return null;
    }

    private static string NormalizeQualifiedIdentifier(string relation)
    {
        return string.Join(
            ".",
            relation.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim()));
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
