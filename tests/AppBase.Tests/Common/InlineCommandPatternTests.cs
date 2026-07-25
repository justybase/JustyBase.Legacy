using AppBase.Common;

namespace AppBase.Tests.Common;

public sealed class InlineCommandPatternTests
{
    [Fact]
    public void Regex_matches_valid_inline_command()
    {
        var match = InlineCommandPattern.Regex().Match("___run: /usr/bin/python -> script.py");

        Assert.True(match.Success);
        Assert.Equal("/usr/bin/python", match.Groups["programPath"].Value);
        Assert.Equal("script.py", match.Groups["arguments"].Value);
    }

    [Fact]
    public void Regex_matches_command_with_spaces_in_arguments()
    {
        var match = InlineCommandPattern.Regex().Match("___run: C:\\Program Files\\app.exe -> --flag value");

        Assert.True(match.Success);
        Assert.Equal("C:\\Program Files\\app.exe", match.Groups["programPath"].Value);
        Assert.Equal("--flag value", match.Groups["arguments"].Value);
    }

    [Fact]
    public void Regex_does_not_match_random_text()
    {
        var match = InlineCommandPattern.Regex().Match("this is not a command");

        Assert.False(match.Success);
    }

    [Fact]
    public void Regex_does_not_match_empty_string()
    {
        var match = InlineCommandPattern.Regex().Match("");

        Assert.False(match.Success);
    }

    [Fact]
    public void Regex_does_not_match_missing_arguments()
    {
        var match = InlineCommandPattern.Regex().Match("___run: /usr/bin/python ->");

        Assert.False(match.Success);
    }

    [Fact]
    public void Regex_matches_command_with_newline_after()
    {
        var match = InlineCommandPattern.Regex().Match("___run: python -> script.py\nSELECT 1");

        Assert.True(match.Success);
        Assert.Equal("python", match.Groups["programPath"].Value);
        Assert.Equal("script.py", match.Groups["arguments"].Value);
    }
}
