using JustData.Application.Settings;
using JustData.ViewModels.Preferences;

namespace JustData.Preferences.Tests;

public sealed class PreferencesViewModelTests
{
    [Fact]
    public async Task Load_exposes_all_sections_and_reload_discards_uncommitted_edits()
    {
        var store = new FakeStore();
        var vm = new PreferencesViewModel(store);
        await vm.LoadAsync();

        Assert.True(vm.SaveCommand.CanExecute(null));
        Assert.NotNull(vm.Appearance);
        Assert.NotNull(vm.Editor);
        Assert.NotNull(vm.SqlResults);
        Assert.NotNull(vm.ImportExport);
        Assert.NotNull(vm.FilesStartup);
        Assert.NotNull(vm.Lint);
        Assert.NotNull(vm.Terminal);

        vm.ImportExport.SepInExportedCsv = ",";
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(";", vm.ImportExport.SepInExportedCsv);
    }

    [Fact]
    public async Task Save_validates_and_saves_a_clone_without_exposing_settings_in_errors()
    {
        var store = new FakeStore();
        var vm = new PreferencesViewModel(store);
        await vm.LoadAsync();
        vm.SqlResults.CommandTimeout = 1;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.SaveCommand.CanExecute(null));
        Assert.Contains(vm.ValidationErrors.Keys, key => key.EndsWith("CommandTimeout", StringComparison.Ordinal));
        Assert.DoesNotContain("SELECT", vm.ErrorMessage ?? string.Empty);
        Assert.Equal(0, store.SaveCount);

        vm.SqlResults.CommandTimeout = 5;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1, store.SaveCount);
        Assert.True(vm.IsSaved);
    }

    [Fact]
    public async Task Cancel_reverts_preview_and_never_calls_store()
    {
        var store = new FakeStore();
        var preview = new FakePreview();
        var vm = new PreferencesViewModel(store, preview);
        await vm.LoadAsync();
        vm.Appearance.UseSpecialColoring = true;
        vm.PreviewTheme();
        vm.CancelCommand.Execute(null);

        Assert.Equal(0, store.SaveCount);
        Assert.Equal(1, preview.RevertCount);
        Assert.True(vm.IsCancelled);
    }

    [Fact]
    public async Task Store_errors_are_safe_and_dispose_reverts_preview()
    {
        var store = new FakeStore { ThrowOnSave = true };
        var preview = new FakePreview();
        var vm = new PreferencesViewModel(store, preview);
        await vm.LoadAsync();
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Unable to save preferences.", vm.ErrorMessage);
        Assert.DoesNotContain("secret", vm.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        vm.Dispose();
        Assert.Equal(1, preview.RevertCount);
    }

    [Fact]
    public async Task Every_section_vm_edits_the_same_transactional_draft()
    {
        var vm = new PreferencesViewModel(new FakeStore());
        await vm.LoadAsync();

        vm.Appearance.FontName = "SectionFont";
        vm.Editor.TypoLimit = 2;
        vm.SqlResults.CommandTimeout = 20;
        vm.ImportExport.SepInExportedCsv = "|";
        vm.FilesStartup.SimpleStartupRestore = false;
        vm.Lint.DisableLintRule("legacy-rule");
        vm.Terminal.TerminalPanelHeight = 321;

        Assert.Equal("SectionFont", vm.Draft.Appearance.FontName);
        Assert.Equal(2, vm.Draft.Editor.TypoLimit);
        Assert.Equal(20, vm.Draft.SqlResults.CommandTimeout);
        Assert.Equal("|", vm.Draft.ImportExport.SepInExportedCsv);
        Assert.False(vm.Draft.FilesStartup.SimpleStartupRestore);
        Assert.Contains("legacy-rule", vm.Draft.Lint.DisabledLintRules);
        Assert.Equal(321, vm.Draft.Terminal.TerminalPanelHeight);
        Assert.DoesNotContain("SectionFont", vm.ToString(), StringComparison.Ordinal);
    }

    private sealed class FakeStore : IApplicationSettingsStore
    {
        public int SaveCount { get; private set; }
        public bool ThrowOnSave { get; init; }
        public ApplicationSettingsDraft Current { get; } = new()
        {
            Appearance = new AppearanceSettings { FontName = "Consolas", FontSize = 10 },
            Editor = new EditorSettings { FileSearchTimeout = 10_000, TypoLimit = 1 },
            SqlResults = new SqlResultsSettings { CommandTimeout = 3600, ResultRowsLimit = 200_000, ResultRowsLimitWarning = 100_000, MaxSchemaParallelism = 16 },
            ImportExport = new ImportExportSettings { SepInExportedCsv = ";" }
        };

        public Task<ApplicationSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSettingsSnapshot(Current));
        public Task SaveAsync(ApplicationSettingsDraft draft, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave) throw new IOException("secret backend detail");
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePreview : ISettingsThemePreview
    {
        public int RevertCount { get; private set; }
        public void Preview(ApplicationSettingsDraft draft) { }
        public void Commit(ApplicationSettingsSnapshot snapshot) { }
        public void Revert() => RevertCount++;
    }
}
