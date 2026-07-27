using System.Text.Json;
using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Models;
using JustData.Application.Variables;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacySnippetsProviderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "LegacySnippetsProviderTests_" + Guid.NewGuid().ToString("N"));

    public LegacySnippetsProviderTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var settings = Substitute.For<IApplicationSettingsContext>();
        var store = Substitute.For<ISessionVariableStore>();
        var state = new NetezzaAutocompleteState();

        Assert.Throws<ArgumentNullException>(() => new LegacySnippetsProvider(null!, store, state));
        Assert.Throws<ArgumentNullException>(() => new LegacySnippetsProvider(settings, null!, state));
        Assert.Throws<ArgumentNullException>(() => new LegacySnippetsProvider(settings, store, null!));
    }

    [Fact]
    public void EnsureSnippetsLoaded_reads_snipets_json_once()
    {
        WriteSnippets(["SELECT"], ["sel"], ["@@imp"]);
        var settings = CreateSettings();
        var state = new NetezzaAutocompleteState();

        LegacySnippetsProvider.EnsureSnippetsLoaded(settings, state);
        File.WriteAllText(Path.Combine(_tempDir, "snipets.json"), "{}");
        LegacySnippetsProvider.EnsureSnippetsLoaded(settings, state);

        Assert.Equal(["SELECT"], state.Keywords);
        Assert.Equal(["sel"], state.Snippets);
        Assert.Equal(["@@imp"], state.MonkeySnippets);
    }

    [Fact]
    public void YieldPreambleItems_includes_declare_globals_and_session_variables()
    {
        WriteSnippets([], [], []);
        var settings = CreateSettings();
        var store = Substitute.For<ISessionVariableStore>();
        var state = new NetezzaAutocompleteState();
        store.GlobalVariables.Returns(new Dictionary<string, string> { ["ENV"] = "dev" });
        store.GetSessionVariables("doc1").Returns(new Dictionary<string, string> { ["@x"] = "1" });
        var sut = new LegacySnippetsProvider(settings, store, state);

        var labels = sut.YieldPreambleItems("doc1").Select(i => i.ToString()).ToArray();

        Assert.Contains("declare", labels);
        Assert.Contains("ENV", labels);
        Assert.Contains("@x", labels);
    }

    [Fact]
    public void TryYieldAtPrefixItems_handles_at_and_dot_prefixes()
    {
        WriteSnippets([], [], ["monkey"]);
        var state = new NetezzaAutocompleteState();
        state.ReplaceActualColumns(["extra!!"]);
        var sut = new LegacySnippetsProvider(CreateSettings(), Substitute.For<ISessionVariableStore>(), state);
        var aliases = new List<(string basicHint, string description)>
        {
            ("e", "alias"),
            ("e.ID", "pk id\r\nignored"),
            ("other.x", "skip")
        };

        Assert.True(sut.TryYieldAtPrefixItems("@@", aliases, out var atItems));
        var atLabels = atItems.Select(i => i.ToString()!).ToArray();
        Assert.Contains(atLabels, l => l.Contains("extra!!", StringComparison.Ordinal));
        Assert.Contains(atLabels, l => l.Contains("monkey", StringComparison.Ordinal));
        Assert.Contains(atLabels, l => l.Contains("@@e", StringComparison.Ordinal));

        Assert.False(sut.TryYieldAtPrefixItems(".", [], out var dotItems));
        Assert.Contains(dotItems, i => i.ToString()!.Contains(".ImportXlsxTxtCsv", StringComparison.Ordinal));

        Assert.False(sut.TryYieldAtPrefixItems("plain", [], out var empty));
        Assert.Empty(empty);
    }

    [Fact]
    public void YieldKeywordsAndSnippets_skips_dotted_fragments()
    {
        WriteSnippets(["JOIN"], ["snip"], []);
        var sut = new LegacySnippetsProvider(CreateSettings(), Substitute.For<ISessionVariableStore>(), new NetezzaAutocompleteState());

        Assert.Empty(sut.YieldKeywordsAndSnippets("a.b"));

        var labels = sut.YieldKeywordsAndSnippets("j").Select(i => i.ToString()).ToArray();
        Assert.Contains("JOIN", labels);
        Assert.Contains("snip", labels);
    }

    private IApplicationSettingsContext CreateSettings()
    {
        var settings = Substitute.For<IApplicationSettingsContext>();
        settings.ConfigDirectory.Returns(_tempDir);
        return settings;
    }

    private void WriteSnippets(string[] keywords, string[] snippets, string[] monkey)
    {
        var sn = new Snipets
        {
            Keywords = keywords,
            Snippets = snippets,
            MonkeySnippets = monkey
        };
        File.WriteAllText(
            Path.Combine(_tempDir, "snipets.json"),
            JsonSerializer.Serialize(sn, MyJsonContextSnipets.Default.Snipets));
    }
}
