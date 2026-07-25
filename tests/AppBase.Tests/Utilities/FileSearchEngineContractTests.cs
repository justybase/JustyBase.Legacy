using AppBase.Services.Utilities;

namespace AppBase.Tests.Utilities;

public sealed class FileSearchEngineContractTests
{
    private readonly IFileSearchEngine _sut = new FileSearchEngine();

    [Fact]
    public void Implements_IFileSearchEngine()
    {
        Assert.IsAssignableFrom<IFileSearchEngine>(_sut);
    }

    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(FileSearchEngine.Default, FileSearchEngine.Default);
    }

    [Fact]
    public void Default_implements_interface()
    {
        Assert.IsAssignableFrom<IFileSearchEngine>(FileSearchEngine.Default);
    }

    [Fact]
    public void Static_methods_delegate_to_default()
    {
        // Static calls should produce same result as instance calls
        var staticResult = FileSearchEngine.NormalizeExtensionPatterns("*.sql, cs");
        var instanceResult = FileSearchEngine.Default.DoNormalizeExtensionPatterns("*.sql, cs");
        Assert.Equal(staticResult, instanceResult);
    }

    [Fact]
    public void Interface_methods_delegate_correctly()
    {
        var interfaceResult = _sut.NormalizeExtensionPatterns("*.sql, cs");
        var instanceResult = FileSearchEngine.Default.DoNormalizeExtensionPatterns("*.sql, cs");
        Assert.Equal(interfaceResult, instanceResult);
    }

    [Fact]
    public void GetDefaultExtensionPatterns_returns_expected_list()
    {
        var patterns = _sut.GetDefaultExtensionPatterns();
        Assert.Contains(".sql", patterns);
        Assert.Contains(".cs", patterns);
        Assert.Contains(".txt", patterns);
        Assert.Contains(".json", patterns);
    }

    [Fact]
    public void GetDefaultExtensionPatterns_through_interface()
    {
        var patterns = _sut.GetDefaultExtensionPatterns();
        Assert.NotEmpty(patterns);
    }

    [Fact]
    public void NormalizeExtensionPatterns_null_returns_default()
    {
        var result = _sut.NormalizeExtensionPatterns(null);
        Assert.NotEmpty(result);
        Assert.Contains(".sql", result);
    }

    [Fact]
    public void NormalizeExtensionPatterns_empty_returns_default()
    {
        var result = _sut.NormalizeExtensionPatterns("");
        Assert.NotEmpty(result);
        Assert.Contains(".sql", result);
    }

    [Fact]
    public void NormalizeExtensionPatterns_whitespace_returns_default()
    {
        var result = _sut.NormalizeExtensionPatterns("   ");
        Assert.NotEmpty(result);
        Assert.Contains(".sql", result);
    }

    [Theory]
    [InlineData("*.sql, cs;json", new[] { ".sql", ".cs", ".json" })]
    [InlineData(".sql,.cs,.py", new[] { ".sql", ".cs", ".py" })]
    [InlineData("sql", new[] { ".sql" })]
    [InlineData("*.sql", new[] { ".sql" })]
    public void NormalizeExtensionPatterns_various_inputs(string input, string[] expected)
    {
        var result = _sut.NormalizeExtensionPatterns(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeExtensionPatterns_deduplicates()
    {
        var result = _sut.NormalizeExtensionPatterns(".sql, .SQL, sql");
        Assert.Single(result);
        Assert.Equal(".sql", result[0]);
    }

    [Fact]
    public void NormalizeExtensionPatterns_through_interface()
    {
        var result = _sut.NormalizeExtensionPatterns("*.sql");
        Assert.Equal(new[] { ".sql" }, result);
    }
}
