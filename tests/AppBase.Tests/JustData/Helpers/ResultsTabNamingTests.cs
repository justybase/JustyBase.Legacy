using JustyBaseLegacy.UI.Helpers;

namespace AppBase.Tests.JustData.Helpers;

public sealed class ResultsTabNamingTests
{
    // ── ParseResultNumber ──

    [Theory]
    [InlineData("Result 1", 1)]
    [InlineData("Result 42", 42)]
    [InlineData("result 5", 5)]
    [InlineData("RESULT 999", 999)]
    [InlineData("Result 0", 0)]
    public void ParseResultNumber_valid_returns_number(string title, int expected)
    {
        Assert.Equal(expected, ResultsTabNaming.ParseResultNumber(title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Result")]
    [InlineData("Result ")]
    [InlineData("Log")]
    [InlineData("Log 1")]
    [InlineData("Tab1")]
    [InlineData("Result A")]
    [InlineData("Result 1 extra")]
    [InlineData("Result 1 ")]
    public void ParseResultNumber_invalid_returns_null(string? title)
    {
        Assert.Null(ResultsTabNaming.ParseResultNumber(title));
    }

    // ── NextResultTitle ──

    [Fact]
    public void NextResultTitle_empty_list_returns_Result_1()
    {
        Assert.Equal("Result 1", ResultsTabNaming.NextResultTitle([]));
    }

    [Fact]
    public void NextResultTitle_no_matches_returns_Result_1()
    {
        Assert.Equal("Result 1", ResultsTabNaming.NextResultTitle(["Log", "Tab1"]));
    }

    [Fact]
    public void NextResultTitle_finds_max_and_increments()
    {
        var titles = new[] { "Result 1", "Result 5", "Result 3" };
        Assert.Equal("Result 6", ResultsTabNaming.NextResultTitle(titles));
    }

    [Fact]
    public void NextResultTitle_ignores_non_matching_titles()
    {
        var titles = new[] { "Log", "Result 10", "Tab1", "Result 2" };
        Assert.Equal("Result 11", ResultsTabNaming.NextResultTitle(titles));
    }

    [Fact]
    public void NextResultTitle_single_match()
    {
        Assert.Equal("Result 2", ResultsTabNaming.NextResultTitle(["Result 1"]));
    }

    // ── NextLogTitle ──

    [Fact]
    public void NextLogTitle_empty_list_returns_Log()
    {
        Assert.Equal("Log", ResultsTabNaming.NextLogTitle([]));
    }

    [Fact]
    public void NextLogTitle_no_matches_returns_Log()
    {
        Assert.Equal("Log", ResultsTabNaming.NextLogTitle(["Result 1", "Tab1"]));
    }

    [Fact]
    public void NextLogTitle_one_log_returns_Log_2()
    {
        Assert.Equal("Log 2", ResultsTabNaming.NextLogTitle(["Log"]));
    }

    [Fact]
    public void NextLogTitle_counts_all_logs()
    {
        var titles = new[] { "Log", "Result 1", "Log 2", "Log 3" };
        Assert.Equal("Log 4", ResultsTabNaming.NextLogTitle(titles));
    }

    [Fact]
    public void NextLogTitle_max()
    {
        var titles = new[] { "Log", "Log 5", "Log 3" };
        Assert.Equal("Log 4", ResultsTabNaming.NextLogTitle(titles));
    }

    // ── IResultsTabNaming interface ──

    [Fact]
    public void Default_implements_interface()
    {
        Assert.IsAssignableFrom<IResultsTabNaming>(ResultsTabNaming.Default);
    }

    [Fact]
    public void Interface_delegates_correctly()
    {
        IResultsTabNaming naming = ResultsTabNaming.Default;
        Assert.Equal("Result 1", naming.NextResultTitle([]));
        Assert.Equal("Log", naming.NextLogTitle([]));
    }
}
