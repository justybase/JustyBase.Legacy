using JustData.Application.Settings;

namespace JustData.Preferences.Tests;

public sealed class ApplicationSettingsTests
{
    // ── RgbaColor ──

    [Fact]
    public void RgbaColor_FromLegacy_creates_from_four_bytes()
    {
        var color = RgbaColor.FromLegacy([10, 20, 30, 40]);

        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
        Assert.Equal(40, color.A);
    }

    [Fact]
    public void RgbaColor_FromLegacy_default_when_fewer_than_four_bytes()
    {
        var color = RgbaColor.FromLegacy([1, 2]);

        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void RgbaColor_FromLegacy_empty_list()
    {
        var color = RgbaColor.FromLegacy([]);

        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void RgbaColor_ToLegacy_roundtrips()
    {
        var original = new RgbaColor(100, 150, 200, 255);
        byte[] legacy = original.ToLegacy();
        var restored = RgbaColor.FromLegacy(legacy);

        Assert.Equal(original, restored);
    }

    // ── SnippetSettings ──

    [Fact]
    public void SnippetSettings_Clone_creates_deep_copy()
    {
        var original = new SnippetSettings
        {
            Keywords = ["SELECT", "FROM"],
            Snippets = ["sel", "ins"],
            MonkeySnippets = ["m1"]
        };

        var clone = original.Clone();

        Assert.Equal(original.Keywords, clone.Keywords);
        Assert.Equal(original.Snippets, clone.Snippets);
        Assert.Equal(original.MonkeySnippets, clone.MonkeySnippets);

        // Modify original - clone should be independent
        original.Keywords.Add("WHERE");
        Assert.Equal(2, clone.Keywords.Count);
    }

    [Fact]
    public void SnippetSettings_default_values_are_empty()
    {
        var settings = new SnippetSettings();

        Assert.Empty(settings.Keywords);
        Assert.Empty(settings.Snippets);
        Assert.Empty(settings.MonkeySnippets);
    }

    // ── ApplicationSettingsDraft ──

    [Fact]
    public void ApplicationSettingsDraft_Clone_creates_deep_copy()
    {
        var original = new ApplicationSettingsDraft
        {
            Appearance = new AppearanceSettings { FontName = "Consolas", FontSize = 12f },
            Editor = new EditorSettings { FileSearchTimeout = 5 },
            SqlResults = new SqlResultsSettings { CommandTimeout = 30 },
        };

        var clone = original.Clone();

        Assert.Equal("Consolas", clone.Appearance.FontName);
        Assert.Equal(12f, clone.Appearance.FontSize);
        Assert.Equal(5, clone.Editor.FileSearchTimeout);
        Assert.Equal(30, clone.SqlResults.CommandTimeout);

        // Modify original - clone should be independent
        original.Appearance.FontName = "Courier";
        Assert.Equal("Consolas", clone.Appearance.FontName);
    }

    [Fact]
    public void ApplicationSettingsDraft_default_values()
    {
        var draft = new ApplicationSettingsDraft();

        Assert.NotNull(draft.Appearance);
        Assert.NotNull(draft.Editor);
        Assert.NotNull(draft.SqlResults);
        Assert.NotNull(draft.ImportExport);
        Assert.NotNull(draft.FilesStartup);
        Assert.NotNull(draft.Lint);
        Assert.NotNull(draft.Terminal);
        Assert.NotNull(draft.Snippets);
    }

    // ── ApplicationSettingsSnapshot ──

    [Fact]
    public void ApplicationSettingsSnapshot_clones_on_construction()
    {
        var draft = new ApplicationSettingsDraft
        {
            Appearance = new AppearanceSettings { FontName = "Test" }
        };

        var snapshot = new ApplicationSettingsSnapshot(draft);

        Assert.Equal("Test", snapshot.Values.Appearance.FontName);

        // Modify original - snapshot should be independent
        draft.Appearance.FontName = "Changed";
        Assert.Equal("Test", snapshot.Values.Appearance.FontName);
    }

    [Fact]
    public void ApplicationSettingsSnapshot_throws_on_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationSettingsSnapshot(null!));
    }

    [Fact]
    public void ApplicationSettingsSnapshot_ToDraft_returns_independent_copy()
    {
        var draft = new ApplicationSettingsDraft
        {
            Appearance = new AppearanceSettings { FontName = "Original" }
        };

        var snapshot = new ApplicationSettingsSnapshot(draft);
        var restored = snapshot.ToDraft();

        restored.Appearance.FontName = "Restored";
        Assert.Equal("Original", snapshot.Values.Appearance.FontName);
    }

    // ── DatabaseInfoSnapshot ──

    [Fact]
    public void DatabaseInfoSnapshot_stores_all_fields()
    {
        var snapshot = new DatabaseInfoSnapshot(42, "sales", "admin", "public");

        Assert.Equal(42, snapshot.SchemaId);
        Assert.Equal("sales", snapshot.DatabaseName);
        Assert.Equal("admin", snapshot.DatabaseOwner);
        Assert.Equal("public", snapshot.SchemaName);
    }

    // ── AppearanceSettings defaults ──

    [Fact]
    public void AppearanceSettings_defaults()
    {
        var settings = new AppearanceSettings();

        Assert.False(settings.AlternatingRows);
        Assert.False(settings.DoLegend);
        Assert.False(settings.UseSpecialColoring);
        Assert.Equal(string.Empty, settings.FontName);
        Assert.Equal(0, settings.FontSize);
    }

    // ── EditorSettings defaults ──

    [Fact]
    public void EditorSettings_defaults()
    {
        var settings = new EditorSettings();

        Assert.False(settings.TypoCorrect);
        Assert.False(settings.AutoCompleteBrackets);
        Assert.False(settings.BracketFolding);
        Assert.False(settings.DontUseIndent);
        Assert.Empty(settings.QuickSnippets);
        Assert.Empty(settings.KeyWordsListForColoring1);
    }

    // ── SqlResultsSettings defaults ──

    [Fact]
    public void SqlResultsSettings_defaults()
    {
        var settings = new SqlResultsSettings();

        Assert.False(settings.FastLogin);
        Assert.Equal(1, settings.RefreshMode);
        Assert.Empty(settings.DateTimeFormat);
    }

    // ── ImportExportSettings defaults ──

    [Fact]
    public void ImportExportSettings_defaults()
    {
        var settings = new ImportExportSettings();

        Assert.False(settings.ImportExisting);
        Assert.False(settings.UseXlsb);
        Assert.Empty(settings.SepInExportedCsv);
        Assert.Empty(settings.EncondingName);
    }

    // ── FilesStartupSettings defaults ──

    [Fact]
    public void FilesStartupSettings_defaults()
    {
        var settings = new FilesStartupSettings();

        Assert.False(settings.NotFirstLaunch);
        Assert.False(settings.SimpleStartupRestore);
        Assert.Empty(settings.StartsFolderPaths);
    }

    // ── LintSettings defaults ──

    [Fact]
    public void LintSettings_defaults()
    {
        var settings = new LintSettings();
        Assert.Empty(settings.DisabledLintRules);
    }

    // ── TerminalSettings defaults ──

    [Fact]
    public void TerminalSettings_defaults()
    {
        var settings = new TerminalSettings();

        Assert.False(settings.TerminalPanelVisible);
        Assert.Equal(0, settings.TerminalPanelHeight);
        Assert.Equal(0, settings.TerminalShell);
    }
}
