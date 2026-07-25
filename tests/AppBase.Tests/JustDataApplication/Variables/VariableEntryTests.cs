using JustData.Application.Variables;

namespace AppBase.Tests.JustDataApplication.Variables;

public sealed class VariableEntryTests
{
    [Fact]
    public void VariableEntry_creates_with_session()
    {
        var entry = new VariableEntry("MY_VAR", "value", true);
        Assert.Equal("MY_VAR", entry.Name);
        Assert.Equal("value", entry.Value);
        Assert.True(entry.IsSession);
    }

    [Fact]
    public void VariableEntry_creates_without_session()
    {
        var entry = new VariableEntry("my_var", null, false);
        Assert.Equal("my_var", entry.Name);
        Assert.Null(entry.Value);
        Assert.False(entry.IsSession);
    }

    [Fact]
    public void VariableEntry_with_empty_value()
    {
        var entry = new VariableEntry("var", "", false);
        Assert.Equal("", entry.Value);
    }

    [Fact]
    public void VariableEntry_equality()
    {
        var e1 = new VariableEntry("X", "1", true);
        var e2 = new VariableEntry("X", "1", true);
        Assert.Equal(e1, e2);
    }

    [Fact]
    public void VariableEntry_inequality()
    {
        var e1 = new VariableEntry("X", "1", true);
        var e2 = new VariableEntry("Y", "1", true);
        Assert.NotEqual(e1, e2);
    }
}
