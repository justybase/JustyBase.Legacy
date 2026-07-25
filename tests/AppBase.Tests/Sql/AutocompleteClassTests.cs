using JustData.Application.Sql;

namespace AppBase.Tests.Sql;

public sealed class AutocompleteClassTests
{
    [Fact]
    public void Top_level_select_and_from_ignore_nested_subqueries()
    {
        string query = "  select outer_id from (select inner_id from detail) nested where outer_id > 0";

        int select = SqlTextCursorParser.LastSelect(ref query);
        int from = SqlTextCursorParser.FirstFrom(query["select".Length..]);

        Assert.Equal("select outer_id from (select inner_id from detail) nested where outer_id > 0", query);
        Assert.Equal(0, select);
        Assert.Equal(query["select".Length..].IndexOf(" from ", StringComparison.Ordinal), from);
    }

    [Fact]
    public void Clause_scanner_returns_the_first_top_level_clause()
    {
        const string text = "value from (select value from detail where x = 1) nested group by value limit 10";

        int clause = SqlTextCursorParser.FirstWhereGroupLimit(text);

        Assert.Equal(' ', text[clause]);
        Assert.Equal("group by", text.Substring(clause + 1, "group by".Length));
    }

    [Fact]
    public void Clause_scanner_ignores_where_inside_nested_parentheses()
    {
        const string text = "from (select value from detail where x = 1) nested order by value";

        Assert.Equal(-1, SqlTextCursorParser.FirstWhereGroupLimit(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("from_table")]
    [InlineData(" (select value from detail)")]
    public void First_from_returns_not_found_when_from_is_not_top_level(string text)
    {
        Assert.Equal(-1, SqlTextCursorParser.FirstFrom(text));
    }

    [Fact]
    public void Last_select_returns_the_last_top_level_select_and_trims_input()
    {
        string query = "  select a from (select b from detail) nested  ";

        int select = SqlTextCursorParser.LastSelect(ref query);

        Assert.Equal(0, select);
        Assert.Equal("select a from (select b from detail) nested", query);
    }

    [Fact]
    public void Between_semicolons_respects_semicolons_inside_quoted_literals()
    {
        const string sql = "select 1; select 'a;b' from detail; select 3";

        string statement = SqlTextCursorParser.BetweenSemicolons(sql.IndexOf("from", StringComparison.Ordinal), sql);

        Assert.Equal(" select 'a;b' from detail", statement);
    }

    [Fact]
    public void Between_parentheses_returns_the_current_nested_fragment()
    {
        const string sql = "select * from (select id from detail) nested; select 2";

        string fragment = SqlTextCursorParser.BetweenParenthesesOrBrackets(
            sql.IndexOf("id", StringComparison.Ordinal),
            sql);

        Assert.Equal("select id from detail", fragment);
    }

    [Fact]
    public void Find_closing_bracket_accounts_for_nested_pairs()
    {
        const string sql = "(a(b)c)";

        Assert.Equal(6, SqlTextCursorParser.FindClosingBracket(sql, start: 1));
        Assert.Equal(-1, SqlTextCursorParser.FindClosingBracket("(a(b)c", start: 1));
    }
}
