using AppBase.Common.Configuration;
using AppBase.Common.JsonContext;
using AppBase.Common.Interfaces;
using AppBase.Common;
using JustData.Application.Files;
using System.Text.Json;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Owns process settings and recent-file persistence. It is deliberately
/// independent from the schema, session-variable, watcher and editor state
/// that used to live in <c>BaseWindowHelpers</c>.
/// </summary>
public sealed class LegacyApplicationSettingsContext :
    IApplicationSettingsContext,
    IApplicationSettingsBootstrapContext,
    IApplicationSettingsPersistence,
    IRecentFileRuntimeContext,
    IRecentFileStore
{
    private IApplicationConfig _config = new ApplicationConfig();

    public IApplicationConfig Config => _config;
    public string ConfigDirectory { get; set; } = string.Empty;
    public string ConfigMainFile { get; set; } = string.Empty;
    public bool DoSaveConfig { get; set; } = true;

    public List<string> RecentFiles { get; } = [];
    public List<string> RecentManySqlFiles { get; } = [];

    public void Initialize()
    {
        ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JustyBaseLegacy");
        ConfigMainFile = Path.Combine(ConfigDirectory, "config.json");
        ReadConfig();
    }

    public void ReadConfig()
    {
        if (!Directory.Exists(ConfigDirectory))
            Directory.CreateDirectory(ConfigDirectory);

        try
        {
            if (!string.IsNullOrWhiteSpace(ConfigMainFile) && File.Exists(ConfigMainFile))
            {
                try
                {
                    _config = JsonSerializer.Deserialize(
                        File.ReadAllText(ConfigMainFile),
                        MyJsonContextApplicationConfig.Default.ApplicationConfig)
                        ?? new ApplicationConfig();
                }
                catch (Exception exception)
                {
                    BackupCorruptConfig();
                    MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _config = new ApplicationConfig();
                }
            }
            else
            {
                _config = new ApplicationConfig();
            }
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            MessageBox.Show("Wrong password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _config = new ApplicationConfig();
        }
        catch (Exception exception)
        {
            BackupCorruptConfig();
            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _config = new ApplicationConfig();
        }

        _config.MakeChangesInWrongConfigValues();
        NormalizeConfigValues();
    }

    public void SaveConfig()
    {
        if (!DoSaveConfig)
            return;

        string json = JsonSerializer.Serialize(
            _config,
            MyJsonContextApplicationConfig.Default.ApplicationConfig);
        SaveTextFileEncodedOrNot(ConfigMainFile, json);
    }

    public void SaveRecentFiles()
    {
        if (RecentFiles.Count > 0)
        {
            string path = Path.Combine(ConfigDirectory, "recent.json");
            string content = JsonSerializer.Serialize(
                RecentFiles,
                MyJsonContextStringList.Default.ListString);
            SaveTextFileEncodedOrNot(path, content);
        }

        if (RecentManySqlFiles.Count > 0)
        {
            string path = Path.Combine(ConfigDirectory, "recentMany.json");
            string content = JsonSerializer.Serialize(
                RecentManySqlFiles,
                MyJsonContextStringList.Default.ListString);
            SaveTextFileEncodedOrNot(path, content);
        }
    }

    Task<IReadOnlyList<string>> IRecentFileStore.LoadAsync(
        RecentFileKind kind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> paths = (kind == RecentFileKind.ManySql
            ? RecentManySqlFiles
            : RecentFiles).ToArray();
        return Task.FromResult(paths);
    }

    Task IRecentFileStore.SaveAsync(
        RecentFileKind kind,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<string> target = kind == RecentFileKind.ManySql
            ? RecentManySqlFiles
            : RecentFiles;
        target.Clear();
        target.AddRange(paths.Where(path => !string.IsNullOrWhiteSpace(path)));
        SaveRecentFiles();
        return Task.CompletedTask;
    }

    private void BackupCorruptConfig()
    {
        if (string.IsNullOrWhiteSpace(ConfigMainFile) || !File.Exists(ConfigMainFile))
            return;

        try
        {
            string backupPath = $"{ConfigMainFile}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak";
            File.Move(ConfigMainFile, backupPath, false);
        }
        catch
        {
            // A locked config should not prevent startup with defaults.
        }
    }

    private void NormalizeConfigValues()
    {
        _config.MaxRecentFilesCount = Math.Max(1, _config.MaxRecentFilesCount);
        _config.ResultRowsLimit = Math.Max(1, _config.ResultRowsLimit);
        _config.ResultRowsLimitWarning = Math.Max(1, _config.ResultRowsLimitWarning);
        _config.ConnectionTimeout = Math.Max(1, _config.ConnectionTimeout);
        _config.CommandTimeout = Math.Max(1, _config.CommandTimeout);
        _config.CommandDistTimeout = Math.Max(1, _config.CommandDistTimeout);
        _config.FileSearchTimeout = Math.Max(1, _config.FileSearchTimeout);
        _config.MaxSchemaParallelism = Math.Clamp(_config.MaxSchemaParallelism, 1, 128);
        _config.TerminalPanelHeight = Math.Clamp(_config.TerminalPanelHeight, 100, 2_000);
    }

    private static void SaveTextFileEncodedOrNot(string path, string content) =>
        File.WriteAllText(path, content);
}
