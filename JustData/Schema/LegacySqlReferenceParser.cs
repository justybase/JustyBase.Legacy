using JustData.Application.Schema;
using System.Text.RegularExpressions;

namespace JustyBaseLegacy.UI.Schema;

public static partial class LegacySqlReferenceParser
{
    public static IReadOnlyList<SchemaReference> Parse(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return [];
        List<SchemaReference> references = [];
        Add(references, RegexTempTable(), sql, SchemaNodeKind.Table, "tableAlias", IsCommented);
        Add(references, RegexWith(), sql, SchemaNodeKind.Alias, "tableAlias", IsCommented);
        Add(references, RegexInsert(), sql, SchemaNodeKind.Table, "table_name", IsCommented);
        Add(references, RegexDelete(), sql, SchemaNodeKind.Table, "table_name", IsCommented);
        Add(references, RegexDrop(), sql, SchemaNodeKind.Table, "table_name", IsCommented);
        Add(references, RegexView(), sql, SchemaNodeKind.View, "name", IsCommented);
        Add(references, RegexProcedure(), sql, SchemaNodeKind.Procedure, "name", IsCommented);

        foreach (Match match in RegexSelect().Matches(sql))
            AddKeyword(references, sql, match, "Select", SchemaNodeKind.Unknown);
        foreach (Match match in RegexFrom().Matches(sql))
            AddKeyword(references, sql, match, "From", SchemaNodeKind.Unknown);
        foreach (Match match in RegexWhereGroupLimit().Matches(sql))
        {
            string keyword = match.Value.Trim();
            AddKeyword(references, sql, match, keyword.Equals("GROUP BY", StringComparison.OrdinalIgnoreCase) ? "Group By" : keyword, SchemaNodeKind.Unknown);
        }

        return references.OrderBy(reference => reference.Position).ToArray();
    }

    /// <summary>
    /// Outline definitions used by F4 / Ctrl+click navigation (temp tables, CTEs, DROP TABLE).
    /// Excludes INSERT/DELETE targets so catalog lookup remains the fallback for those names.
    /// </summary>
    public static IReadOnlyList<SchemaReference> ParseNavigableDefinitions(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return [];
        List<SchemaReference> references = [];
        Add(references, RegexTempTable(), sql, SchemaNodeKind.Table, "tableAlias", IsCommented);
        Add(references, RegexWith(), sql, SchemaNodeKind.Alias, "tableAlias", IsCommented);
        Add(references, RegexDrop(), sql, SchemaNodeKind.Table, "table_name", IsCommented);
        return references.OrderBy(reference => reference.Position).ToArray();
    }

    private static void Add(
        ICollection<SchemaReference> references,
        Regex regex,
        string sql,
        SchemaNodeKind kind,
        string group,
        Func<string, int, bool> include)
    {
        foreach (Match match in regex.Matches(sql))
        {
            Group value = match.Groups[group];
            if (!value.Success || include(sql, value.Index)) continue;
            references.Add(new SchemaReference(value.Value, kind, value.Index));
        }
    }

    private static void AddKeyword(ICollection<SchemaReference> references, string sql, Match match, string title, SchemaNodeKind kind)
    {
        if (!IsCommented(sql, match.Index))
            references.Add(new SchemaReference(title, kind, match.Index));
    }

    private static bool IsCommented(string sql, int position)
    {
        int lineStart = sql.LastIndexOf('\n', Math.Max(0, position - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        return sql.IndexOf("--", lineStart, Math.Max(0, position - lineStart), StringComparison.Ordinal) >= 0;
    }

    [GeneratedRegex("\\b(create\\s+temp\\s+table|create\\s+table)\\s+(?<tableAlias>(\\w|\\.)+?)\\b\\s*as\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexTempTable();
    [GeneratedRegex("(,|with\\s)\\s*(?<tableAlias>\\w+)\\s+as\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexWith();
    [GeneratedRegex("CREATE\\s+(OR\\s+REPLACE\\s+)?VIEW\\s+(?<name>\\w+)\\s+AS", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexView();
    [GeneratedRegex("CREATE\\s+(OR\\s+REPLACE\\s+)?PROCEDURE\\s+(?<name>\\w+)\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexProcedure();
    [GeneratedRegex("\\bdelete\\s+from\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexDelete();
    [GeneratedRegex("\\bdrop\\s+table\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexDrop();
    [GeneratedRegex("\\binsert\\s+into\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexInsert();
    [GeneratedRegex("\\bselect\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexSelect();
    [GeneratedRegex("\\bfrom\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexFrom();
    [GeneratedRegex("\\b(where|group\\s+by|limit)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegexWhereGroupLimit();
}
