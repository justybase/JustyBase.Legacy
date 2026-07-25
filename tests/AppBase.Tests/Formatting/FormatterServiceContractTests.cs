using AppBase.Common;
using AppBase.Services;

namespace AppBase.Tests.Formatting;

public sealed class FormatterServiceContractTests
{
    private readonly FormatterService _sut = new();

    [Fact]
    public void Implements_IFormatterService()
    {
        Assert.IsAssignableFrom<IFormatterService>(_sut);
    }

    [Fact]
    public void Instance_delegates_to_Format()
    {
        var result = _sut.Format("select 1");
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_very_long_sql()
    {
        // Long SQL with many columns
        var columns = string.Join(", ", Enumerable.Range(1, 100).Select(i => $"col{i}"));
        var sql = $"select {columns} from large_table";
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_nested_subqueries()
    {
        const string sql = """
                           select * from (
                               select id, name from (
                                   select id, name from users where active = 1
                               ) inner_q
                           ) outer_q where name like '%test%'
                           """;
        var result = _sut.Format(sql);
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_multiple_join_types()
    {
        const string sql = """
                           select a.id, b.name, c.value
                           from table_a a
                           left join table_b b on a.id = b.a_id
                           right join table_c c on b.id = c.b_id
                           full outer join table_d d on c.id = d.c_id
                           cross join table_e e
                           """;
        var result = _sut.Format(sql);
        Assert.Contains("JOIN", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXCEPTION", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_aggregate_and_group_by()
    {
        const string sql = "select dept, count(*) as cnt, avg(salary) as avg_sal from employees group by dept having count(*) > 5 order by cnt desc";
        var result = _sut.Format(sql);
        Assert.Contains("GROUP", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HAVING", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_set_operations()
    {
        const string sql = "select id from users union all select id from archived_users except select id from deleted_users intersect select id from active_users";
        var result = _sut.Format(sql);
        Assert.Contains("UNION", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXCEPT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INTERSECT", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_with_merge_and_upsert_syntax()
    {
        const string sql = "merge into target t using source s on t.id = s.id when matched then update set t.name = s.name when not matched then insert (id, name) values (s.id, s.name)";
        var result = _sut.Format(sql);
        Assert.Contains("MERGE", result, StringComparison.OrdinalIgnoreCase);
    }
}
