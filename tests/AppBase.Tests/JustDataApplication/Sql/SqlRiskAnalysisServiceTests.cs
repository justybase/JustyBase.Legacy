using JustData.Application.Sql;

namespace AppBase.Tests.JustDataApplication.Sql;

public sealed class SqlRiskAnalysisServiceTests
{
    private static readonly SqlRiskAnalysisService Service = new();

    // ──────────────────────────────────────────────
    // Edge: null, empty, whitespace
    // ──────────────────────────────────────────────

    [Fact]
    public void Analyze_null_returns_empty()
    {
        var risks = Service.Analyze(null!);
        Assert.Empty(risks);
    }

    [Fact]
    public void Analyze_empty_returns_empty()
    {
        var risks = Service.Analyze("");
        Assert.Empty(risks);
    }

    [Fact]
    public void Analyze_whitespace_returns_empty()
    {
        var risks = Service.Analyze("   ");
        Assert.Empty(risks);
    }

    // ──────────────────────────────────────────────
    // UnsafeUpdateDelete
    // ──────────────────────────────────────────────

    [Fact]
    public void Analyze_UPDATE_without_WHERE_detects_risk()
    {
        var risks = Service.Analyze("UPDATE users SET name = 'test'");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Fact]
    public void Analyze_DELETE_without_WHERE_detects_risk()
    {
        var risks = Service.Analyze("DELETE FROM users");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Theory]
    [InlineData("UPDATE users SET name = 'test' WHERE id = 1")]
    [InlineData("DELETE FROM users WHERE id = 1")]
    [InlineData("UPDATE users SET name = 'test' WHERE id IN (1, 2, 3)")]
    [InlineData("DELETE FROM users\nWHERE id = 1")]
    public void Analyze_sql_with_WHERE_returns_no_UnsafeUpdateDelete(string sql)
    {
        var risks = Service.Analyze(sql);
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Fact]
    public void Analyze_unrelated_statement_returns_no_UnsafeUpdateDelete()
    {
        var risks = Service.Analyze("SELECT * FROM users");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Fact]
    public void Analyze_UPDATE_is_case_insensitive()
    {
        var risks = Service.Analyze("update users set name = 'test'");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Fact]
    public void Analyze_DELETE_is_case_insensitive()
    {
        var risks = Service.Analyze("delete from users");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    [Fact]
    public void Analyze_safe_UPDATE_has_correct_message()
    {
        // UPDATE without WHERE — confirm the risk message
        var risks = Service.Analyze("UPDATE users SET name = 'test'");
        var risk = Assert.Single(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
        Assert.Contains("WHERE", risk.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(risk.IsBlocking);
    }

    [Fact]
    public void Analyze_UPDATE_statement_starting_with_whitespace_is_detected()
    {
        // Shared Core trims / masks before matching — leading whitespace must not bypass.
        var risks = Service.Analyze("  UPDATE users SET name = 'test'");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
    }

    // ──────────────────────────────────────────────
    // MissingDistribute (NetezzaSQL only)
    // ──────────────────────────────────────────────

    [Fact]
    public void Analyze_CREATE_TABLE_without_DISTRIBUTE_Netezza_detects_risk()
    {
        var risks = Service.Analyze("CREATE TABLE my_table (id INT)", "NetezzaSQL");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TEMP_TABLE_without_DISTRIBUTE_Netezza_detects_risk()
    {
        var risks = Service.Analyze("CREATE TEMP TABLE temp_data (id INT)", "NetezzaSQL");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TABLE_with_DISTRIBUTE_Netezza_returns_no_MissingDistribute()
    {
        var risks = Service.Analyze(
            "CREATE TABLE my_table (id INT) DISTRIBUTE ON (id)", "NetezzaSQL");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TABLE_with_DISTRIBUTE_different_case_Netezza()
    {
        var risks = Service.Analyze(
            "create table my_table (id int) distribute on (id)", "NetezzaSQL");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TABLE_without_DISTRIBUTE_other_driver_returns_no_MissingDistribute()
    {
        var risks = Service.Analyze("CREATE TABLE my_table (id INT)", "PostgreSQL");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TABLE_without_DISTRIBUTE_null_driver_returns_no_MissingDistribute()
    {
        var risks = Service.Analyze("CREATE TABLE my_table (id INT)");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_CREATE_TABLE_without_DISTRIBUTE_empty_string_driver()
    {
        var risks = Service.Analyze("CREATE TABLE my_table (id INT)", "");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
    }

    [Fact]
    public void Analyze_MissingDistribute_has_correct_message()
    {
        var risks = Service.Analyze("CREATE TABLE my_table (id INT)", "NetezzaSQL");
        var risk = Assert.Single(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
        Assert.Contains("DISTRIBUTE", risk.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(risk.IsBlocking);
    }

    // ──────────────────────────────────────────────
    // SelectInto
    // ──────────────────────────────────────────────

    [Fact]
    public void Analyze_SELECT_INTO_detects_risk()
    {
        var risks = Service.Analyze("SELECT * INTO backup_table FROM source");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_SELECT_INTO_without_FROM_detects_risk()
    {
        // The regex SELECT\s+.*\s+INTO\s+\w+\s+($|FROM) requires \s+ before $/FROM
        var risks = Service.Analyze("SELECT a INTO temp FROM t");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_SELECT_INTO_without_FROM_still_detects()
    {
        // Shared Core matches SELECT … INTO <ident> without requiring FROM.
        var risks = Service.Analyze("SELECT a INTO temp");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_SELECT_INTO_is_case_insensitive()
    {
        var risks = Service.Analyze("select * into backup_table from source");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_SELECT_INTO_detects_across_all_drivers()
    {
        // Should fire regardless of driver
        var risksNoDriver = Service.Analyze("SELECT * INTO t FROM s");
        var risksNetezza = Service.Analyze("SELECT * INTO t FROM s", "NetezzaSQL");
        var risksPostgres = Service.Analyze("SELECT * INTO t FROM s", "PostgreSQL");

        Assert.Contains(risksNoDriver, r => r.Kind == SqlRiskKind.SelectInto);
        Assert.Contains(risksNetezza, r => r.Kind == SqlRiskKind.SelectInto);
        Assert.Contains(risksPostgres, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_SelectInto_has_correct_message()
    {
        var risks = Service.Analyze("SELECT * INTO t FROM s");
        var risk = Assert.Single(risks, r => r.Kind == SqlRiskKind.SelectInto);
        Assert.Contains("SELECT INTO", risk.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(risk.IsBlocking);
    }

    [Fact]
    public void Analyze_SELECT_without_INTO_returns_no_SelectInto()
    {
        var risks = Service.Analyze("SELECT * FROM users");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_INTO_without_SELECT_returns_no_SelectInto()
    {
        // The regex requires SELECT before INTO, so bare INTO should not match
        var risks = Service.Analyze("INSERT INTO t VALUES (1)");
        Assert.DoesNotContain(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    // ──────────────────────────────────────────────
    // Multiple risks at once
    // ──────────────────────────────────────────────

    [Fact]
    public void Analyze_multiple_risks_detected_together()
    {
        // UPDATE without WHERE + SELECT INTO = 2 risks
        var risks = Service.Analyze("UPDATE users SET name = 'test'; SELECT * INTO t FROM s", "NetezzaSQL");
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    [Fact]
    public void Analyze_all_three_risks_together()
    {
        // Shared Core splits on ';' and analyzes each statement.
        var sql = """
                  UPDATE users SET name = 'test';
                  CREATE TABLE t (id INT);
                  SELECT * INTO backup FROM source;
                  """;
        var risks = Service.Analyze(sql, "NetezzaSQL");

        Assert.Contains(risks, r => r.Kind == SqlRiskKind.UnsafeUpdateDelete);
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.MissingDistribute);
        Assert.Contains(risks, r => r.Kind == SqlRiskKind.SelectInto);
    }

    // ──────────────────────────────────────────────
    // SqlRisk record and SqlRiskKind enum
    // ──────────────────────────────────────────────

    [Fact]
    public void SqlRisk_record_properties()
    {
        var risk = new SqlRisk(SqlRiskKind.UnsafeUpdateDelete, "Test message", IsBlocking: true);
        Assert.Equal(SqlRiskKind.UnsafeUpdateDelete, risk.Kind);
        Assert.Equal("Test message", risk.Message);
        Assert.True(risk.IsBlocking);
    }

    [Fact]
    public void SqlRisk_record_equality()
    {
        var r1 = new SqlRisk(SqlRiskKind.UnsafeUpdateDelete, "msg", false);
        var r2 = new SqlRisk(SqlRiskKind.UnsafeUpdateDelete, "msg", false);
        Assert.Equal(r1, r2);
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
    }

    [Fact]
    public void SqlRisk_record_inequality()
    {
        var r1 = new SqlRisk(SqlRiskKind.UnsafeUpdateDelete, "msg", false);
        var r2 = new SqlRisk(SqlRiskKind.MissingDistribute, "msg", false);
        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public void SqlRiskKind_values()
    {
        Assert.Equal(0, (int)SqlRiskKind.UnsafeUpdateDelete);
        Assert.Equal(1, (int)SqlRiskKind.MissingDistribute);
        Assert.Equal(2, (int)SqlRiskKind.SelectInto);
    }

    // ──────────────────────────────────────────────
    // ISqlRiskAnalysisService contract
    // ──────────────────────────────────────────────

    [Fact]
    public void Service_implements_interface()
    {
        Assert.IsAssignableFrom<ISqlRiskAnalysisService>(Service);
    }

    [Fact]
    public void Analyze_returns_IReadOnlyList()
    {
        var risks = Service.Analyze("SELECT 1");
        Assert.IsAssignableFrom<IReadOnlyList<SqlRisk>>(risks);
    }
}
