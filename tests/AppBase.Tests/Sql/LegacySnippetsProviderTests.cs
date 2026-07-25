using System.Reflection;
using System.Text.Json;
using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using JustData.Application.Variables;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacySnippetsProviderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "LegacySnippetsProviderTests_" + Guid.NewGuid().ToString("N"));
    private readonly string[] _previousKeywords;
    private readonly string[] _previousSnippets;
    private readonly string[] _previousMonkey;
    private readonly string? _previousExtra;

    public LegacySnippetsProviderTests()
    {
        Directory.CreateDirectory(_tempDir);
        _previousKeywords = DynamicCollectionForNettezaHelpers.Keywords;
        _previousSnippets = DynamicCollectionForNettezaHelpers.Snippets;
        _previousMonkey = DynamicCollectionForNettezaHelpers.MonkeySnippets;
        _previousExtra = DynamicCollectionForNettezaHelpers.ExtraSnippet;
        ResetSnippetsLoaded();
    }

    public void Dispose()
    {
        DynamicCollectionForNettezaHelpers.Keywords = _previousKeywords;
        DynamicCollectionForNettezaHelpers.Snippets = _previousSnippets;
        DynamicCollectionForNettezaHelpers.MonkeySnippets = _previousMonkey;
        DynamicCollectionForNettezaHelpers.ExtraSnippet = _previousExtra!;
        ResetSnippetsLoaded();
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

        Assert.Throws<ArgumentNullException>(() => new LegacySnippetsProvider(null!, store));
        Assert.Throws<ArgumentNullException>(() => new LegacySnippetsProvider(settings, null!));
    }

    [Fact]
    public void EnsureSnippetsLoaded_reads_snipets_json_once()
    {
        WriteSnippets(["SELECT"], ["sel"], ["@@imp"]);
        var settings = CreateSettings();

        LegacySnippetsProvider.EnsureSnippetsLoaded(settings);
        File.WriteAllText(Path.Combine(_tempDir, "snipets.json"), "{}"); // would fail if re-read
        LegacySnippetsProvider.EnsureSnippetsLoaded(settings);

        Assert.Equal(["SELECT"], DynamicCollectionForNettezaHelpers.Keywords);
        Assert.Equal(["sel"], DynamicCollectionForNettezaHelpers.Snippets);
        Assert.Equal(["@@imp"], DynamicCollectionForNettezaHelpers.MonkeySnippets);
    }

    [Fact]
    public void YieldPreambleItems_includes_declare_globals_and_session_variables()
    {
        WriteSnippets([], [], []);
        var settings = CreateSettings();
        var store = Substitute.For<ISessionVariableStore>();
        store.GlobalVariables.Returns(new Dictionary<string, string> { ["ENV"] = "dev" });
        store.GetSessionVariables("doc1").Returns(new Dictionary<string, string> { ["@x"] = "1" });
        var sut = new LegacySnippetsProvider(settings, store);

        var labels = sut.YieldPreambleItems("doc1").Select(i => i.ToString()).ToArray();

        Assert.Contains("declare", labels);
        Assert.Contains("ENV", labels);
        Assert.Contains("@x", labels);
    }

    [Fact]
    public void TryYieldAtPrefixItems_handles_at_and_dot_prefixes()
    {
        WriteSnippets([], [], ["monkey"]);
        DynamicCollectionForNettezaHelpers.ExtraSnippet = "extra!!";
        var sut = new LegacySnippetsProvider(CreateSettings(), Substitute.For<ISessionVariableStore>());
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
        var sut = new LegacySnippetsProvider(CreateSettings(), Substitute.For<ISessionVariableStore>());

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
        ResetSnippetsLoaded();
    }

    private static void ResetSnippetsLoaded()
    {
        typeof(LegacySnippetsProvider)
            .GetField("_snippetsLoaded", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, false);
    }
}
