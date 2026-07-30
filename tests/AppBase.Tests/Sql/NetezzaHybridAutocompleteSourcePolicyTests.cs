using AppBase.Data.Completion;
using JustyBase.NetezzaSqlParser.Authoring;

namespace AppBase.Tests.Sql;

/// <summary>
/// Documents the size gate used by NetezzaHybridAutocompleteSource.BuildSqlCompletions
/// (must stay aligned with AutocompleteClass.AddAutocompleteForNZ).
/// </summary>
public sealed class NetezzaHybridAutocompleteSourcePolicyTests
{
    [Fact]
    public void BigSql_dimensions_skip_heavy_engine_completions()
    {
        // Measured BIG.SQL (~245 KB, ~9k lines) from typing-perf repro.
        Assert.True(SqlPerformancePolicy.ShouldSkipDeepAutocompleteScan(9_188, 244_544));
    }

    [Fact]
    public void Typical_script_still_runs_engine_completions()
    {
        Assert.False(SqlPerformancePolicy.ShouldSkipDeepAutocompleteScan(200, 20_000));
    }

    [Fact]
    public void SliceSqlForEngine_uses_trailing_statement_block_on_large_doc()
    {
        string prefix = new string('a', 200_000) + ";";
        const string tail = "SELECT * FROM JUST_DATA..DIMDATE D WHERE D.";
        string sql = prefix + tail;

        (string window, int windowCursor) = NetezzaHybridAutocompleteSource.SliceSqlForEngine(
            sql, sql.Length, largeDoc: true, forcedAutocomplete: false);

        Assert.Equal(tail, window);
        Assert.Equal(tail.Length, windowCursor);
    }

    [Fact]
    public void SliceSqlForEngine_uses_lookback_window_on_large_doc()
    {
        string sql = new string('a', 200_000) + "SELECT * FROM t";
        int cursor = sql.Length;

        (string window, int windowCursor) = NetezzaHybridAutocompleteSource.SliceSqlForEngine(
            sql, cursor, largeDoc: true, forcedAutocomplete: true);

        Assert.True(window.Length < sql.Length);
        Assert.Equal(window.Length, windowCursor);
        Assert.EndsWith("SELECT * FROM t", window);
    }

    [Fact]
    public void Passive_timer_skips_engine_on_large_doc_forced_ctrl_space_runs()
    {
        Assert.True(SqlPerformancePolicy.ShouldSkipDeepAutocompleteScan(9_188, 244_544));
        // BuildSqlCompletions: engine only when LastAutocompleteForced; tested via policy + manual BIG.SQL.
    }
}
