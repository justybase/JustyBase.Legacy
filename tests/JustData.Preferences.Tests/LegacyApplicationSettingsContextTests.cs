using JustData.Application.Files;
using JustyBaseLegacy.UI.Configuration;

namespace JustData.Preferences.Tests;

public sealed class LegacyApplicationSettingsContextTests
{
    [Fact]
    public void Settings_round_trip_normalizes_unsafe_limits()
    {
        string directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var writer = CreateContext(directory);
            writer.Config.MaxRecentFilesCount = 0;
            writer.Config.ResultRowsLimit = 0;
            writer.Config.ConnectionTimeout = 0;
            writer.Config.MaxSchemaParallelism = 500;
            writer.SaveConfig();

            var reader = CreateContext(directory);
            reader.ReadConfig();

            Assert.Equal(1, reader.Config.MaxRecentFilesCount);
            Assert.Equal(1, reader.Config.ResultRowsLimit);
            Assert.Equal(1, reader.Config.ConnectionTimeout);
            Assert.Equal(128, reader.Config.MaxSchemaParallelism);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Recent_file_store_updates_runtime_lists_and_persists_them()
    {
        string directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var context = CreateContext(directory);
            var store = (IRecentFileStore)context;

            await store.SaveAsync(RecentFileKind.Single, ["one.sql", "", "two.sql"]);

            Assert.Equal(["one.sql", "two.sql"], context.RecentFiles);
            Assert.True(File.Exists(Path.Combine(directory, "recent.json")));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static LegacyApplicationSettingsContext CreateContext(string directory) => new()
    {
        ConfigDirectory = directory,
        ConfigMainFile = Path.Combine(directory, "config.json")
    };
}
