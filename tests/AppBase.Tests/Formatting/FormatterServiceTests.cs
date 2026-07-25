using AppBase.Services;

namespace AppBase.Tests.Formatting;

public sealed class FormatterServiceTests
{
    private readonly FormatterService _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Format_ReturnsInput_WhenNullOrWhitespace(string? sql)
    {
        Assert.Equal(sql, _sut.Format(sql!));
    }

    [Fact]
    public void Format_FormatsSimpleSelect()
    {
        const string input = "select id,name from users where id=1";
        string result = _sut.Format(input);

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_PreservesNonEmptySql_OnFallback()
    {
        const string input = "SELECT 1";
        string result = _sut.Format(input);

        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", result);
    }
}
