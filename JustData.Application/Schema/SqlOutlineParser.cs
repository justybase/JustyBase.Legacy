using JustyBase.NetezzaSqlParser.Ast;
using JustyBase.NetezzaSqlParser.Lexer;
using JustyBase.NetezzaSqlParser.Parser;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace JustData.Application.Schema;

/// <summary>Converts the Netezza parser AST into the source-backed Outline model.</summary>
public static class SqlOutlineParser
{
    private static readonly ConcurrentDictionary<string, SqlOutline> Cache = new(StringComparer.Ordinal);

    public static SqlOutline Parse(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return SqlOutline.Empty;
        sql ??= string.Empty;
        if (Cache.TryGetValue(sql, out SqlOutline? cached)) return cached;
        List<OutlineNode> result = [];
        try
        {
            var tokens = NzLexer.Tokenize(sql).ToArray();
            var parser = new NzSqlParser(tokens);
            int safety = 0;
            while (parser.Position < tokens.Length && safety++ < tokens.Length + 2)
            {
                int before = parser.Position;
                if (tokens[before].Span.IsAtEnd) break;
                Statement? statement = null;
                try { statement = parser.Parse(); } catch { }
                if (statement is not null)
                {
                    try { result.Add(BuildStatement(statement, sql)); }
                    catch { result.Add(new OutlineNode("Unparsed/unsupported statement", OutlineNodeKind.Warning, Absolute(statement.Position), 0, IsIncomplete: true)); }
                }
                if (parser.Position <= before)
                {
                    parser.ResetTokens(tokens[(before + 1)..]);
                    // ResetTokens starts a new token window; source positions remain absolute.
                    break;
                }
            }
            if (result.Count > 0)
            {
                SqlOutline outline = new(result.OrderBy(x => x.Position).ToArray());
                Cache[sql] = outline;
                return outline;
            }
        }
        catch { }

        SqlOutline fallback = new(Fallback(sql), true);
        Cache[sql] = fallback;
        return fallback;
    }

    private static OutlineNode BuildStatement(Statement statement, string sql)
    {
        OutlineNode node = statement switch
        {
            CreateTableStatement table => Node(table.Temporary ? OutlineNodeKind.TempTable : OutlineNodeKind.Table,
                Qualified(table.Table), table.Position, sql, table.Table, table.AsSelect is null ? [] : BuildSelect(table.AsSelect, sql)),
            CreateViewStatement view => Node(OutlineNodeKind.View, Qualified(view.View), view.Position, sql, view.View,
                BuildSelect(view.Query, sql)),
            CreateProcedureStatement procedure => Node(OutlineNodeKind.Procedure, Qualified(procedure.Procedure), procedure.Position, sql, procedure.Procedure,
                BuildProcedure(procedure.Body, sql)),
            SelectStatement select => Node(OutlineNodeKind.Select, "SELECT", select.Position, sql, children: BuildSelect(select, sql)),
            _ => new OutlineNode(statement.GetType().Name.Replace("Statement", "", StringComparison.Ordinal), OutlineNodeKind.Statement,
                statement.Position.Absolute, 0, IsIncomplete: false)
        };
        return node;
    }

    private static OutlineNode Node(OutlineNodeKind kind, string name, SourcePosition position, string sql,
        TableName? table = null, IReadOnlyList<OutlineNode>? children = null, string? alias = null, int? namePosition = null) =>
        new(name, kind, namePosition ?? FindName(sql, Absolute(position), table?.Name ?? name),
            Math.Max(0, sql.Length - Absolute(position)), alias ?? table?.Name, table?.Database, table?.Schema, Children: children);

    private static IReadOnlyList<OutlineNode> BuildSelect(SelectStatement select, string sql)
    {
        List<OutlineNode> children = [];
        if (select.With is not null)
            foreach (CteDefinition cte in select.With!.Ctes)
                children.Add(Node(OutlineNodeKind.Cte, cte.Name, cte.Position, sql,
                    children: BuildSelect(cte.Query, sql), namePosition: FindCteName(sql, cte.Name, Absolute(cte.Position))));

        foreach (TableReference from in select.From ?? [])
        {
            children.Add(BuildSource(from.Source, sql, OutlineNodeKind.Table));
            foreach (JoinClause join in from.Joins ?? [])
            {
                var joinChildren = new List<OutlineNode> { BuildSource(join.Source, sql, OutlineNodeKind.Join) };
                children.Add(new OutlineNode("JOIN", OutlineNodeKind.Join, Absolute(join.Position),
                    Math.Max(0, sql.Length - Absolute(join.Position)), Children: joinChildren));
            }
        }

        // Expression-level subqueries are added by the parser's explicit source tree;
        // avoid reflecting arbitrary expression graphs here (some parser nodes expose
        // lazy/cyclic metadata properties).
        return children.OrderBy(x => x.Position).ToArray();
    }

    private static OutlineNode BuildSource(TableSource source, string sql, OutlineNodeKind kind)
    {
        if (source.Subquery is not null)
            return new OutlineNode("SUBQUERY", OutlineNodeKind.Subquery, Absolute(source.Position),
                Math.Max(0, sql.Length - Absolute(source.Position)), source.Alias,
                Children: BuildSelect(source.Subquery, sql));
        TableName? table = source.Table;
        return Node(kind, table is null ? "SOURCE" : Qualified(table), source.Position, sql, table,
            alias: source.Alias, children: []);
    }

    private static IReadOnlyList<OutlineNode> BuildProcedure(ProcedureBody? body, string sql)
    {
        if (body is null) return [];
        List<OutlineNode> nodes = [];
        foreach (ProcedureStatement statement in body.Statements)
        {
            OutlineNodeKind kind = statement switch
            {
                ProcedureIfStatement => OutlineNodeKind.Block,
                ProcedureLoopStatement => OutlineNodeKind.Block,
                ProcedureWhileStatement => OutlineNodeKind.Block,
                ProcedureForStatement => OutlineNodeKind.Block,
                ProcedureBlockStatement => OutlineNodeKind.Block,
                _ => OutlineNodeKind.Statement
            };
            var children = statement switch
            {
                ProcedureIfStatement value => value.ThenStatements.Select(x => BuildProcedureStatement(x, sql)).ToArray(),
                ProcedureLoopStatement value => value.Statements.Select(x => BuildProcedureStatement(x, sql)).ToArray(),
                ProcedureWhileStatement value => value.Statements.Select(x => BuildProcedureStatement(x, sql)).ToArray(),
                ProcedureForStatement value => value.Statements.Select(x => BuildProcedureStatement(x, sql)).ToArray(),
                ProcedureBlockStatement value => BuildProcedure(value.Body, sql),
                _ => []
            };
            nodes.Add(new OutlineNode(statement.GetType().Name.Replace("Procedure", "").Replace("Statement", "", StringComparison.Ordinal),
                kind, Absolute(statement.Position), Math.Max(0, sql.Length - Absolute(statement.Position)), Children: children));
        }
        return nodes;
    }

    private static OutlineNode BuildProcedureStatement(ProcedureStatement statement, string sql) =>
        new(statement.GetType().Name.Replace("Procedure", "").Replace("Statement", "", StringComparison.Ordinal),
            statement is ProcedureIfStatement or ProcedureLoopStatement or ProcedureWhileStatement or ProcedureForStatement
                ? OutlineNodeKind.Block : OutlineNodeKind.Statement,
            Absolute(statement.Position), Math.Max(0, sql.Length - Absolute(statement.Position)));

    private static void AddExpressionSubqueries(object? value, string sql, ICollection<OutlineNode> output)
    {
        if (value is null) return;
        if (value is SubqueryExpression subquery)
        {
            output.Add(new OutlineNode("SUBQUERY", OutlineNodeKind.Subquery, Absolute(subquery.Position),
                Math.Max(0, sql.Length - Absolute(subquery.Position)), Children: BuildSelect(subquery.Query, sql)));
            return;
        }
        if (value is IEnumerable enumerable && value is not string)
            foreach (object? item in enumerable) AddExpressionSubqueries(item, sql, output);
        else if (value is AstNode ast)
            foreach (PropertyInfo property in ast.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (property.Name != "Position") AddExpressionSubqueries(property.GetValue(ast), sql, output);
    }

    private static string Qualified(TableName table) => string.Join(".", new[] { table.Database, table.Schema, table.Name }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static int Absolute(SourcePosition? position) => position?.Absolute ?? 0;
    private static int FindName(string sql, int start, string name)
    {
        int found = sql.IndexOf(name, Math.Max(0, start), StringComparison.OrdinalIgnoreCase);
        return found < 0 ? Math.Max(0, start) : found;
    }

    private static int FindCteName(string sql, string name, int queryPosition)
    {
        MatchCollection matches = Regex.Matches(sql,
            $@"(?<![\w$]){Regex.Escape(name)}\s*(?:\([^)]*\))?\s+AS\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Match? definition = matches.Cast<Match>().Where(match => match.Index <= queryPosition).LastOrDefault();
        return definition?.Index ?? FindName(sql, Math.Max(0, queryPosition), name);
    }

    private static IReadOnlyList<OutlineNode> Fallback(string sql)
    {
        List<OutlineNode> nodes = [];
        foreach (Match match in Regex.Matches(sql, @"(?im)\b(select|create\s+(?:temp\s+)?table|create\s+(?:or\s+replace\s+)?view|create\s+procedure)\b"))
            nodes.Add(new OutlineNode(match.Groups[1].Value.ToUpperInvariant(), OutlineNodeKind.Warning, match.Index,
                match.Length, IsIncomplete: true));
        if (nodes.Count == 0 && !string.IsNullOrWhiteSpace(sql))
            nodes.Add(new OutlineNode("Unparsed/unsupported statement", OutlineNodeKind.Warning, 0, sql.Length, IsIncomplete: true));
        return nodes;
    }
}
