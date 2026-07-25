using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Settings;
using JustyBaseLegacy.UI.Configuration;
using NSubstitute;
using System.Text.Json;

namespace JustData.Preferences.Tests;

public sealed class WinFormsApplicationSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustData-PreferencesStoreTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Store_round_trips_config_and_snippets_and_updates_live_state_only_after_save()
    {
        Directory.CreateDirectory(_directory);
        string configPath = Path.Combine(_directory, "config.json");
        var config = new ApplicationConfig();
        config.MakeChangesInWrongConfigValues();
        config.CachedDatabaseDictionary = [];
        var helpers = Substitute.For<IApplicationSettingsContext>();
        helpers.Config.Returns(config);
        helpers.ConfigDirectory.Returns(_directory);
        helpers.ConfigMainFile.Returns(configPath);
        helpers.DoSaveConfig.Returns(true);

        var store = new WinFormsApplicationSettingsStore(helpers);
        var draft = (await store.LoadAsync()).ToDraft();
        draft.Appearance.FontName = "TransactionalFont";
        draft.SqlResults.CommandTimeout = 1234;
        draft.FilesStartup.StartsFolderPaths = ["C:\\sql", "D:\\startup"];
        draft.FilesStartup.StartFilesExtra = new Dictionary<string, bool> { ["C:\\sql\\one.sql"] = true };
        draft.Snippets = new SnippetSettings { Keywords = ["keyword"], Snippets = ["SELECT"], MonkeySnippets = ["@@orders"] };

        await store.SaveAsync(draft);

        Assert.Equal("TransactionalFont", config.FontName);
        Assert.Equal(1234, config.CommandTimeout);
        Assert.Equal("keyword", AppBase.Data.DynamicCollectionForNettezaHelpers.Keywords.Single());
        Assert.Equal("TransactionalFont", JsonDocument.Parse(File.ReadAllText(configPath)).RootElement.GetProperty("FontName").GetString());
        using JsonDocument persistedConfig = JsonDocument.Parse(File.ReadAllText(configPath));
        Assert.Equal(2, persistedConfig.RootElement.GetProperty("StartsFolderPaths").GetArrayLength());
        Assert.True(persistedConfig.RootElement.GetProperty("StartFilesExtra").GetProperty("C:\\sql\\one.sql").GetBoolean());
        var persistedSnippets = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(_directory, "snipets.json")), AppBase.Common.MyJsonContextSnipets.Default.Snipets)!;
        Assert.Equal(["@@orders"], persistedSnippets.MonkeySnippets);
    }

    [Fact]
    public async Task Store_honors_DoSaveConfig_but_still_preserves_legacy_snippet_save_behavior()
    {
        Directory.CreateDirectory(_directory);
        string configPath = Path.Combine(_directory, "config.json");
        File.WriteAllText(configPath, "original");
        var config = new ApplicationConfig();
        config.MakeChangesInWrongConfigValues();
        var helpers = Substitute.For<IApplicationSettingsContext>();
        helpers.Config.Returns(config);
        helpers.ConfigDirectory.Returns(_directory);
        helpers.ConfigMainFile.Returns(configPath);
        helpers.DoSaveConfig.Returns(false);

        var draft = (await new WinFormsApplicationSettingsStore(helpers).LoadAsync()).ToDraft();
        draft.Appearance.FontName = "not persisted";
        draft.Snippets.Keywords = ["new-keyword"];

        await new WinFormsApplicationSettingsStore(helpers).SaveAsync(draft);

        Assert.Equal("original", File.ReadAllText(configPath));
        Assert.Equal("Consolas", config.FontName);
        Assert.Equal(["new-keyword"], AppBase.Data.DynamicCollectionForNettezaHelpers.Keywords);
    }

    [Fact]
    public async Task Store_failure_restores_config_and_leaves_no_temporary_files()
    {
        Directory.CreateDirectory(_directory);
        string configPath = Path.Combine(_directory, "config.json");
        File.WriteAllText(configPath, "original");
        Directory.CreateDirectory(Path.Combine(_directory, "snipets.json"));
        var config = new ApplicationConfig();
        config.MakeChangesInWrongConfigValues();
        var helpers = Substitute.For<IApplicationSettingsContext>();
        helpers.Config.Returns(config);
        helpers.ConfigDirectory.Returns(_directory);
        helpers.ConfigMainFile.Returns(configPath);
        helpers.DoSaveConfig.Returns(true);

        await Assert.ThrowsAnyAsync<Exception>(() => new WinFormsApplicationSettingsStore(helpers).SaveAsync(new ApplicationSettingsDraft()));

        Assert.Equal("original", File.ReadAllText(configPath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
