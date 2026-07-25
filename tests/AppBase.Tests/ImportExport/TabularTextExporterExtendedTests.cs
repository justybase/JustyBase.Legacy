using AppBase.Services;

namespace AppBase.Tests.ImportExport;

public sealed class TabularTextExporterExtendedTests
{
    [Theory]
    [InlineData("plain", ",", "plain")]
    [InlineData("", ",", "")]
    [InlineData(null, ",", "")]
    public void EscapeCsvField_handles_basic_values(string? value, string delimiter, string expected)
    {
        string result = TabularTextExporter.EscapeCsvField(value, delimiter[0]);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("has,comma", ",", "\"has,comma\"")]
    [InlineData("has\"quote", ",", "\"has\"\"quote\"")]
    [InlineData("has\nnewline", ",", "\"has\nnewline\"")]
    [InlineData("has;semicolon", ";", "\"has;semicolon\"")]
    public void EscapeCsvField_escapes_special_characters(string value, string delimiter, string expected)
    {
        string result = TabularTextExporter.EscapeCsvField(value, delimiter[0]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeCsvField_with_pipe_delimiter()
    {
        string result = TabularTextExporter.EscapeCsvField("value|with|pipes", '|');
        Assert.Equal("\"value|with|pipes\"", result);
    }

    [Fact]
    public void EscapeCsvField_double_quote_is_escaped()
    {
        string result = TabularTextExporter.EscapeCsvField("say \"hello\"", ';');
        Assert.Equal("\"say \"\"hello\"\"\"", result);
    }

    [Fact]
    public void EscapeCsvField_tab_delimiter()
    {
        string result = TabularTextExporter.EscapeCsvField("value\twith\ttabs", '\t');
        Assert.Equal("\"value\twith\ttabs\"", result);
    }
}
