using AppBase.Data;

namespace AppBase.Tests.Sql;

public sealed class DynamicCollectionForNettezaHelpersTests
{
    [Theory]
    [InlineData("col=1", "col", "=", "1")]
    [InlineData("a.b>='x'", "a.b", ">=", "'x'")]
    [InlineData("flag!=true", "flag", "!=", "true")]
    public void RegexSpace3_captures_lhs_operator_and_rhs(
        string input,
        string expectedLhs,
        string expectedOp,
        string expectedRhs)
    {
        var match = DynamicCollectionForNettezaHelpers.RegexSpace3().Match(input);

        Assert.True(match.Success);
        Assert.Equal(expectedLhs, match.Groups[1].Value);
        Assert.Equal(expectedOp, match.Groups[2].Value);
        Assert.Equal(expectedRhs, match.Groups[3].Value);
    }

    [Fact]
    public void RegexSpace3_fails_for_plain_identifier()
    {
        Assert.DoesNotMatch(DynamicCollectionForNettezaHelpers.RegexSpace3(), "EMPLOYEES");
    }

    [Fact]
    public void SortMethodAliases_prioritizes_low_rank_hints_before_alphabetical()
    {
        var ranks = new Dictionary<string, int>
        {
            ["zeta"] = 1,
            ["alpha"] = 2,
            ["beta"] = 10,
            ["gamma"] = 11
        };
        var comparer = DynamicCollectionForNettezaHelpers.SortMethodAliases(ranks);
        var items = new List<(string, string)>
        {
            ("gamma", "g"),
            ("beta", "b"),
            ("zeta", "z"),
            ("alpha", "a")
        };

        items.Sort(comparer);

        Assert.Equal(["zeta", "alpha", "beta", "gamma"], items.Select(i => i.Item1).ToArray());
    }

    [Fact]
    public void ResetCache_clears_both_cache_lists()
    {
        DynamicCollectionForNettezaHelpers.CacheList1.Add(("x", "y"));
        DynamicCollectionForNettezaHelpers.CacheList2.Add(("a", "b"));

        DynamicCollectionForNettezaHelpers.ResetCache();

        Assert.Empty(DynamicCollectionForNettezaHelpers.CacheList1);
        Assert.Empty(DynamicCollectionForNettezaHelpers.CacheList2);
    }
}
