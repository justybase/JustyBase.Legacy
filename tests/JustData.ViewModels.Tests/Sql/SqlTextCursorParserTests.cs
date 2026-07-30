using JustData.Application.Sql;

namespace JustData.ViewModels.Tests.Sql;

public sealed class SqlTextCursorParserTests
{
    [Fact]
    public void BetweenSemicolons_returns_empty_for_position_minus_one()
    {
        Assert.Equal(string.Empty, SqlTextCursorParser.BetweenSemicolons(-1, "select 1;"));
    }

    [Fact]
    public void BetweenSemicolons_ignores_semicolon_inside_single_quotes()
    {
        const string sql = "select 'a;b' from t; select 2;";
        int position = sql.IndexOf("from", StringComparison.Ordinal);

        string statement = SqlTextCursorParser.BetweenSemicolons(position, sql);

        Assert.Contains("'a;b'", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("select 2", statement, StringComparison.Ordinal);
    }

    [Fact]
    public void BetweenSemicolons_ignores_semicolon_inside_double_quotes()
    {
        const string sql = @"select ""c;d"" from t; select 2;";
        int position = sql.IndexOf("from", StringComparison.Ordinal);

        string statement = SqlTextCursorParser.BetweenSemicolons(position, sql);

        Assert.Contains(@"""c;d""", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("select 2", statement, StringComparison.Ordinal);
    }

    [Fact]
    public void BetweenSemicolons_clamps_position_past_end()
    {
        const string sql = "select 1";
        string statement = SqlTextCursorParser.BetweenSemicolons(sql.Length + 5, sql);

        Assert.Equal(sql, statement);
    }

    [Fact]
    public void BetweenParenthesesOrBrackets_extracts_nested_fragment()
    {
        const string sql = "select * from t where id in (select x from (select 1 as x) s);";
        int position = sql.IndexOf("as x", StringComparison.Ordinal);

        string fragment = SqlTextCursorParser.BetweenParenthesesOrBrackets(position, sql);

        Assert.Contains("select 1 as x", fragment, StringComparison.Ordinal);
    }

    [Fact]
    public void BetweenParenthesesOrBrackets_returns_empty_for_position_minus_one()
    {
        Assert.Equal(string.Empty, SqlTextCursorParser.BetweenParenthesesOrBrackets(-1, "(a)"));
    }

    [Theory]
    [InlineData("(a(b)c)", 1, 6)]
    [InlineData("(a(b)c", 1, -1)]
    [InlineData("a)", 0, 1)]
    public void FindClosingBracket_tracks_balance(string sql, int start, int expected)
    {
        Assert.Equal(expected, SqlTextCursorParser.FindClosingBracket(sql, start));
    }

    [Fact]
    public void LastSelect_finds_outer_select_ignoring_subquery()
    {
        string query = "insert into t select * from (select 1) s";
        int index = SqlTextCursorParser.LastSelect(ref query);

        Assert.True(index >= 0);
        Assert.StartsWith("select * from", query[index..].TrimStart(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LastSelect_trims_whitespace_when_requested()
    {
        string query = "  select 1  ";
        int index = SqlTextCursorParser.LastSelect(ref query);

        Assert.Equal(0, index);
        Assert.Equal("select 1", query);
    }

    [Fact]
    public void FirstFrom_skips_from_inside_parentheses()
    {
        const string afterSelect = " (select 1 from dual) x from employees";
        int index = SqlTextCursorParser.FirstFrom(afterSelect);

        Assert.True(index >= 0);
        Assert.StartsWith(" from employees", afterSelect[index..], StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("a where b = 1", "where")]
    [InlineData("a group by b", "group by")]
    [InlineData("a limit 10", "limit")]
    public void FirstWhereGroupLimit_finds_clause_keywords(string text, string expectedKeyword)
    {
        int index = SqlTextCursorParser.FirstWhereGroupLimit(text);

        Assert.True(index >= 0);
        Assert.Contains(expectedKeyword, text[index..], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FirstWhereGroupLimit_skips_keyword_inside_parentheses()
    {
        const string text = "a (where x = 1) b";
        Assert.Equal(-1, SqlTextCursorParser.FirstWhereGroupLimit(text));
    }

    [Fact]
    public void GetStatementBounds_finds_trailing_statement_after_huge_prefix()
    {
        string prefix = new string('x', 200_000) + ";";
        const string tail = "SELECT * FROM t WHERE d.";
        string sql = prefix + tail;
        int cursor = sql.Length;

        (int start, int end) = SqlTextCursorParser.GetStatementBounds(cursor - 1, sql);

        Assert.Equal(prefix.Length, start);
        Assert.Equal(sql.Length, end);
        Assert.Equal(tail, sql[start..end]);
    }

    [Fact]
    public void BetweenSemicolons_throws_for_null_text()
    {
        Assert.Throws<ArgumentNullException>(() => SqlTextCursorParser.BetweenSemicolons(0, null!));
    }
}
