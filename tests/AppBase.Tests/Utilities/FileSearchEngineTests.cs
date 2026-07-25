using AppBase.Services.Utilities;

namespace AppBase.Tests.Utilities;

public sealed class FileSearchEngineTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));

    public FileSearchEngineTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void NormalizeExtensionPatterns_AcceptsDotsGlobsAndMultipleSeparators()
    {
        var result = FileSearchEngine.NormalizeExtensionPatterns("*.sql, cs;json");

        Assert.Equal([".sql", ".cs", ".json"], result);
    }

    [Fact]
    public async Task SearchAsync_ReturnsLineNumbersAndCaseInsensitiveFragments()
    {
        string path = Write("query.sql", "select customer_id\nSELECT order_id\nother");

        var result = await FileSearchEngine.SearchAsync([path], new FileSearchOptions
        {
            Query = "select",
            ExtensionPatterns = [".sql"]
        });

        Assert.Single(result.Files);
        Assert.Equal([1, 2], result.Files[0].Matches.Select(match => match.LineNumber));
    }

    [Fact]
    public async Task SearchAsync_WholeWordDoesNotMatchPartOfWord()
    {
        string path = Write("query.sql", "select selected select");

        var result = await FileSearchEngine.SearchAsync([path], new FileSearchOptions
        {
            Query = "select",
            ExtensionPatterns = [".sql"],
            MatchWholeWord = true
        });

        Assert.Equal(2, result.MatchCount);
    }

    [Fact]
    public async Task SearchAsync_RegexAndMatchCaseAreApplied()
    {
        string path = Write("query.cs", "Value42\nvalue7");

        var result = await FileSearchEngine.SearchAsync([path], new FileSearchOptions
        {
            Query = "Value\\d+",
            ExtensionPatterns = [".cs"],
            UseRegex = true,
            MatchCase = true
        });

        Assert.Single(result.Files[0].Matches);
        Assert.Equal(1, result.Files[0].Matches[0].LineNumber);
    }

    [Fact]
    public async Task SearchAsync_IgnoresFilesOutsideExtensionFilter()
    {
        string path = Write("notes.md", "select");

        var result = await FileSearchEngine.SearchAsync([path], new FileSearchOptions
        {
            Query = "select",
            ExtensionPatterns = [".sql"]
        });

        Assert.Empty(result.Files);
    }

    private string Write(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
