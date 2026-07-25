using AppBase.Common;
using AppBase.Services;

namespace AppBase.Tests.Common;

public sealed class StringExtensionTests
{
    [Fact]
    public void QuoteNameIfNeeded_QuotesNamesWithSpecialCharacters()
    {
        Assert.Equal("\"col-name\"", StringExtension.QuoteNameIfNeeded("col-name"));
    }

    [Fact]
    public void QuoteNameIfNeeded_LeavesSimpleNamesUnquoted()
    {
        Assert.Equal("COLUMN_1", StringExtension.QuoteNameIfNeeded("COLUMN_1"));
    }

    [Fact]
    public void UnquoteName_RemovesSurroundingQuotes()
    {
        Assert.Equal("TABLE", StringExtension.UnquoteName("\"TABLE\""));
    }

    [Fact]
    public void SqlSplit_RespectsParentheses()
    {
        string[] parts = "a, func(b, c), d".SqlSplit();

        Assert.Equal(3, parts.Length);
        Assert.Equal("a", parts[0].Trim());
        Assert.Equal("func(b, c)", parts[1].Trim());
        Assert.Equal("d", parts[2].Trim());
    }
}
