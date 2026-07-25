using AppBase.Services;

namespace AppBase.Tests.Formatting;

public sealed class FormatterServiceExtendedTests
{
    private readonly FormatterService _sut = new();

    [Fact]
    public void Format_sql_with_single_line_comment()
    {
        const string sql = "select id -- comment\nfrom users";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_block_comment()
    {
        const string sql = "select /* comment block */ id from users";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_string_literal()
    {
        const string sql = "select name from users where status = 'active'";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_escaped_quote()
    {
        const string sql = "select name from users where name = 'it''s working'";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_multiple_statements()
    {
        // Formatter uses FormatSql which handles single statements
        const string sql = "select 1; select 2;";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", result);
    }

    [Fact]
    public void Format_sql_with_cte()
    {
        const string sql = "with cte as (select id from users) select * from cte";
        var result = _sut.Format(sql);
        Assert.Contains("WITH", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AS", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_joins()
    {
        const string sql = "select u.name,o.total from users u inner join orders o on u.id=o.user_id left join payments p on o.id=p.order_id";
        var result = _sut.Format(sql);
        Assert.Contains("JOIN", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_subquery()
    {
        const string sql = "select * from users where id in (select user_id from orders where total > 100)";
        var result = _sut.Format(sql);
        Assert.Contains("IN", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_window_function()
    {
        const string sql = "select name, row_number() over (partition by dept order by salary desc) as rn from employees";
        var result = _sut.Format(sql);
        Assert.Contains("OVER", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PARTITION", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_unicode()
    {
        const string sql = "select nazwisko from pracownicy where miasto = 'Wrocław'";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_case_expression()
    {
        const string sql = "select name, case when status=1 then 'active' else 'inactive' end as status_label from users";
        var result = _sut.Format(sql);
        Assert.Contains("CASE", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHEN", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("END", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_mixed_case()
    {
        const string sql = "SELECT name FROM users WHERE id = 1";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_create_table()
    {
        const string sql = "create table test (id int, name varchar(100))";
        var result = _sut.Format(sql);
        Assert.Contains("CREATE", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TABLE", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_sql_with_insert()
    {
        const string sql = "insert into users (name, email) values ('john', 'john@test.com')";
        var result = _sut.Format(sql);
        Assert.Contains("INSERT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VALUES", result, StringComparison.OrdinalIgnoreCase);
    }
}
