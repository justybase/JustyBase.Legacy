using JustData.Application.Settings;
using JustData.ViewModels.Preferences;

namespace JustData.ViewModels.Tests;

public sealed class SettingsSectionViewModelsTests
{
    // ── AppearanceSettingsViewModel ──

    [Fact]
    public void Appearance_settings_exposes_values_and_fires_changed()
    {
        var values = new AppearanceSettings { FontName = "Consolas", FontSize = 12f };
        var vm = new AppearanceSettingsViewModel(values);

        Assert.Equal("Consolas", vm.FontName);
        Assert.Equal(12f, vm.FontSize);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.FontName = "Courier";
        Assert.Equal("Courier", vm.FontName);
        Assert.Contains(nameof(AppearanceSettingsViewModel.FontName), changed);
    }

    [Fact]
    public void Appearance_settings_same_value_does_not_fire_changed()
    {
        var values = new AppearanceSettings { FontName = "Consolas" };
        var vm = new AppearanceSettingsViewModel(values);
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.FontName = "Consolas";

        Assert.DoesNotContain(nameof(AppearanceSettingsViewModel.FontName), changed);
    }

    [Fact]
    public void Appearance_settings_ReplaceValues_refreshes_all_properties()
    {
        var original = new AppearanceSettings { FontName = "A", FontSize = 10f };
        var replacement = new AppearanceSettings { FontName = "B", FontSize = 20f };
        var vm = new AppearanceSettingsViewModel(original);
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ReplaceValues(replacement);

        Assert.Equal("B", vm.FontName);
        Assert.Equal(20f, vm.FontSize);
        Assert.Contains(nameof(AppearanceSettingsViewModel.Values), changed);
    }

    // ── EditorSettingsViewModel ──

    [Fact]
    public void Editor_settings_exposes_values_and_fires_changed()
    {
        var values = new EditorSettings { FileSearchTimeout = 5, AutoCompleteBrackets = true };
        var vm = new EditorSettingsViewModel(values);

        Assert.Equal(5, vm.FileSearchTimeout);
        Assert.True(vm.AutoCompleteBrackets);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.FileSearchTimeout = 10;
        Assert.Equal(10, vm.FileSearchTimeout);
        Assert.Contains(nameof(EditorSettingsViewModel.FileSearchTimeout), changed);
    }

    [Fact]
    public void Editor_settings_ReplaceValues_refreshes_all_properties()
    {
        var original = new EditorSettings { TypoCorrect = true };
        var replacement = new EditorSettings { TypoCorrect = false };
        var vm = new EditorSettingsViewModel(original);

        vm.ReplaceValues(replacement);

        Assert.False(vm.TypoCorrect);
    }

    [Fact]
    public void Editor_settings_exposes_a_snapshot_of_quick_snippets()
    {
        var values = new EditorSettings { QuickSnippets = new Dictionary<string, string> { ["S"] = "select 1" } };
        var vm = new EditorSettingsViewModel(values);

        IReadOnlyDictionary<string, string> snapshot = vm.QuickSnippets;
        values.QuickSnippets["S"] = "select 2";

        Assert.Equal("select 1", snapshot["S"]);
        Assert.Equal("select 2", vm.QuickSnippets["S"]);
    }

    // ── SqlResultsSettingsViewModel ──

    [Fact]
    public void Sql_results_settings_exposes_values_and_fires_changed()
    {
        var values = new SqlResultsSettings { CommandTimeout = 30, ResultRowsLimit = 1000 };
        var vm = new SqlResultsSettingsViewModel(values);

        Assert.Equal(30, vm.CommandTimeout);
        Assert.Equal(1000, vm.ResultRowsLimit);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.CommandTimeout = 60;
        Assert.Equal(60, vm.CommandTimeout);
        Assert.Contains(nameof(SqlResultsSettingsViewModel.CommandTimeout), changed);
    }

    // ── ImportExportSettingsViewModel ──

    [Fact]
    public void Import_export_settings_exposes_values_and_fires_changed()
    {
        var values = new ImportExportSettings { SepInExportedCsv = ";", EncondingName = "UTF-8" };
        var vm = new ImportExportSettingsViewModel(values);

        Assert.Equal(";", vm.SepInExportedCsv);
        Assert.Equal("UTF-8", vm.EncondingName);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.SepInExportedCsv = ",";
        Assert.Equal(",", vm.SepInExportedCsv);
        Assert.Contains(nameof(ImportExportSettingsViewModel.SepInExportedCsv), changed);
    }

    // ── FilesStartupSettingsViewModel ──

    [Fact]
    public void Files_startup_settings_exposes_values_and_fires_changed()
    {
        var values = new FilesStartupSettings { SimpleStartupRestore = true, MaxRecentFilesCount = 10 };
        var vm = new FilesStartupSettingsViewModel(values);

        Assert.True(vm.SimpleStartupRestore);
        Assert.Equal(10, vm.MaxRecentFilesCount);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.SimpleStartupRestore = false;
        Assert.False(vm.SimpleStartupRestore);
        Assert.Contains(nameof(FilesStartupSettingsViewModel.SimpleStartupRestore), changed);
    }

    [Fact]
    public void Files_startup_settings_exposes_a_snapshot_of_extra_files()
    {
        var values = new FilesStartupSettings { StartFilesExtra = new Dictionary<string, bool> { ["a.sql"] = true } };
        var vm = new FilesStartupSettingsViewModel(values);

        IReadOnlyDictionary<string, bool> snapshot = vm.StartFilesExtra;
        values.StartFilesExtra["a.sql"] = false;

        Assert.True(snapshot["a.sql"]);
        Assert.False(vm.StartFilesExtra["a.sql"]);
    }

    // ── LintSettingsViewModel ──

    [Fact]
    public void Lint_settings_exposes_disabled_rules()
    {
        var values = new LintSettings { DisabledLintRules = ["rule1", "rule2"] };
        var vm = new LintSettingsViewModel(values);

        Assert.Equal(["rule1", "rule2"], vm.DisabledLintRules);
    }

    // ── TerminalSettingsViewModel ──

    [Fact]
    public void Terminal_settings_exposes_values_and_fires_changed()
    {
        var values = new TerminalSettings { TerminalPanelVisible = true, TerminalPanelHeight = 200, TerminalShell = 1 };
        var vm = new TerminalSettingsViewModel(values);

        Assert.True(vm.TerminalPanelVisible);
        Assert.Equal(200, vm.TerminalPanelHeight);
        Assert.Equal(1, vm.TerminalShell);

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.TerminalPanelVisible = false;
        Assert.False(vm.TerminalPanelVisible);
        Assert.Contains(nameof(TerminalSettingsViewModel.TerminalPanelVisible), changed);
    }

    // ── ReplaceValues null guards ──

    [Fact]
    public void ReplaceValues_throws_on_null()
    {
        var vm = new AppearanceSettingsViewModel(new AppearanceSettings());
        Assert.Throws<ArgumentNullException>(() => vm.ReplaceValues(null!));
    }

    [Fact]
    public void Constructor_throws_on_null()
    {
        Assert.Throws<ArgumentNullException>(() => new AppearanceSettingsViewModel(null!));
    }
}
