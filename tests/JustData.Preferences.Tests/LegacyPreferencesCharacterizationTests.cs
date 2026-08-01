using AppBase.Common;
using AppBase.Common.Configuration;
using JustData.Application.Settings;
using JustyBaseLegacy.UI.Configuration;
using System.Text.Json;

namespace JustData.Preferences.Tests;

public sealed class LegacyPreferencesCharacterizationTests
{
    [Fact]
    public void CopyEmbeddedFimSettings_preserves_panel_changes_in_transaction_buffer()
    {
        var live = new ApplicationConfig();
        live.MakeChangesInWrongConfigValues();
        live.EnableEmbeddedFimAi = true;
        live.EmbeddedFimModelId = "qwen2.5-coder-7b";
        live.EmbeddedFimPreset = "Large";
        live.EmbeddedFimAcceptedLicenseModelIds = ["qwen2.5-coder-7b"];

        var buffer = new ApplicationConfig();
        LegacyApplicationSettingsMapper.CopyEmbeddedFimSettings(live, buffer);

        Assert.True(buffer.EnableEmbeddedFimAi);
        Assert.Equal("qwen2.5-coder-7b", buffer.EmbeddedFimModelId);
        Assert.Equal("Large", buffer.EmbeddedFimPreset);
        Assert.Equal(["qwen2.5-coder-7b"], buffer.EmbeddedFimAcceptedLicenseModelIds);
    }

    [Fact]
    public void Legacy_config_json_preserves_rgba_lists_timeout_units_and_startup_paths()
    {
        var config = new ApplicationConfig
        {
            BackgroundFastColored = [11, 22, 33, 44],
            SelectionColorFastColored = [55, 66, 77, 88],
            LongQueryWarning = 36_000,
            EstimatedWarning = 600_000,
            EstimatedWarningInterval = 120_000,
            CommandTimeout = 3_600,
            FileSearchTimeout = 10_000,
            StartsFolderPaths = ["C:\\sql", "D:\\startup"],
            StartFilesExtra = new Dictionary<string, bool>
            {
                ["C:\\sql\\one.sql"] = true,
                ["D:\\startup\\two.manysql"] = false
            }
        };

        string json = JsonSerializer.Serialize(config, MyJsonContextApplicationConfig.Default.ApplicationConfig);
        var roundTrip = JsonSerializer.Deserialize(json, MyJsonContextApplicationConfig.Default.ApplicationConfig)!;

        Assert.Equal([11, 22, 33, 44], roundTrip.BackgroundFastColored);
        Assert.Equal([55, 66, 77, 88], roundTrip.SelectionColorFastColored);
        Assert.Equal(36_000, roundTrip.LongQueryWarning);
        Assert.Equal(600_000, roundTrip.EstimatedWarning);
        Assert.Equal(120_000, roundTrip.EstimatedWarningInterval);
        Assert.Equal(3_600, roundTrip.CommandTimeout);
        Assert.Equal(10_000, roundTrip.FileSearchTimeout);
        Assert.Equal(["C:\\sql", "D:\\startup"], roundTrip.StartsFolderPaths);
        Assert.Equal(config.StartFilesExtra, roundTrip.StartFilesExtra);
    }

    [Fact]
    public void Legacy_snipets_json_shape_preserves_keyword_snippet_and_monkey_collections()
    {
        var snippets = new Snipets
        {
            Keywords = ["select", "from"],
            Snippets = ["SELECT * FROM", "WHERE"],
            MonkeySnippets = ["@@orders select * from orders"]
        };

        string json = JsonSerializer.Serialize(snippets, MyJsonContextSnipets.Default.Snipets);
        var roundTrip = JsonSerializer.Deserialize(json, MyJsonContextSnipets.Default.Snipets)!;

        Assert.Equal(snippets.Keywords, roundTrip.Keywords);
        Assert.Equal(snippets.Snippets, roundTrip.Snippets);
        Assert.Equal(snippets.MonkeySnippets, roundTrip.MonkeySnippets);
        Assert.Contains("Keywords", json, StringComparison.Ordinal);
        Assert.Contains("MonkeySnippets", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_config_preserves_schema_cache_and_fixed_refresh_mode()
    {
        var config = new ApplicationConfig
        {
            CachedDatabaseDictionary = new Dictionary<string, Dictionary<int, DatabaseInfo>>
            {
                ["connection"] = new Dictionary<int, DatabaseInfo>
                {
                    [7] = new DatabaseInfo(7, "SYSTEM", "ADMIN", "PUBLIC")
                }
            }
        };

        string json = JsonSerializer.Serialize(config, MyJsonContextApplicationConfig.Default.ApplicationConfig);
        var roundTrip = JsonSerializer.Deserialize(json, MyJsonContextApplicationConfig.Default.ApplicationConfig)!;

        Assert.Equal(1, roundTrip.RefreshMode);
        Assert.Equal("SYSTEM", roundTrip.CachedDatabaseDictionary["connection"][7].DatabaseName);
        Assert.Equal("ADMIN", roundTrip.CachedDatabaseDictionary["connection"][7].DatabaseOwner);
    }

    [Fact]
    public void New_snapshot_mapper_round_trips_the_full_legacy_config_shape()
    {
        var config = new ApplicationConfig();
        config.MakeChangesInWrongConfigValues();
        config.CachedDatabaseDictionary = new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            ["connection"] = new() { [7] = new DatabaseInfo(7, "SYSTEM", "ADMIN", "PUBLIC") }
        };
        config.LongQueryWarning = 36_000;
        config.EstimatedWarning = 600_000;
        config.EstimatedWarningInterval = 120_000;
        config.BackgroundFastColored = [1, 2, 3, 4];
        config.FontName = "MapperFont";
        config.QuickSnippets["SX"] = "SELECT";

        var snapshot = LegacyApplicationSettingsMapper.ToSnapshot(config, new SnippetSettings
        {
            Keywords = ["kw"],
            Snippets = ["snippet"],
            MonkeySnippets = ["@@x"]
        });
        var mapped = LegacyApplicationSettingsMapper.ToLegacy(snapshot.ToDraft());

        string expected = JsonSerializer.Serialize(config, MyJsonContextApplicationConfig.Default.ApplicationConfig);
        string actual = JsonSerializer.Serialize(mapped, MyJsonContextApplicationConfig.Default.ApplicationConfig);
        using JsonDocument expectedDocument = JsonDocument.Parse(expected);
        using JsonDocument actualDocument = JsonDocument.Parse(actual);

        Assert.Equal(expectedDocument.RootElement.GetRawText(), actualDocument.RootElement.GetRawText());
        Assert.Equal([1, 2, 3, 4], snapshot.Values.Appearance.BackgroundFastColored.ToLegacy());
        Assert.Equal(["kw"], snapshot.Values.Snippets.Keywords);
        Assert.Equal(36_000, snapshot.Values.SqlResults.LongQueryWarning);
        Assert.Equal(600_000, snapshot.Values.SqlResults.EstimatedWarning);
        Assert.Equal(120_000, snapshot.Values.SqlResults.EstimatedWarningInterval);
    }
}
