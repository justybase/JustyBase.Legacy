using AppBase.Services.Utilities;

namespace AppBase.Tests.Utilities;

public sealed class FilesPanelCharacterizationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));

    public FilesPanelCharacterizationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Legacy_search_honors_cancellation_and_returns_partial_outcome()
    {
        string path = Path.Combine(_directory, "large.sql");
        await File.WriteAllTextAsync(path, string.Join(Environment.NewLine, Enumerable.Repeat("select customer_id", 50_000)));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        FileSearchOutcome outcome = await FileSearchEngine.SearchAsync(
            [path],
            new FileSearchOptions { Query = "select", ExtensionPatterns = [".sql"] },
            cancellation.Token);

        Assert.True(outcome.WasCancelled);
    }

    [Fact]
    public async Task Legacy_search_caps_matches_per_file_and_preserves_path_order()
    {
        string first = Path.Combine(_directory, "a.sql");
        string second = Path.Combine(_directory, "b.sql");
        await File.WriteAllTextAsync(first, "select\nselect\nselect");
        await File.WriteAllTextAsync(second, "select\nselect");

        FileSearchOutcome outcome = await FileSearchEngine.SearchAsync(
            [second, first],
            new FileSearchOptions
            {
                Query = "select",
                ExtensionPatterns = [".sql"],
                MaxMatchesPerFile = 2
            });

        Assert.Equal([first, second], outcome.Files.Select(file => file.Path));
        Assert.All(outcome.Files, file => Assert.Equal(2, file.Matches.Count));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
