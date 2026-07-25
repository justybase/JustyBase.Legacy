using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Settings;
using AppBase.Data;
using System.Text;
using System.Text.Json;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>Legacy file/config adapter for the transactional Preferences VM.</summary>
/// <summary>WinForms configuration-format adapter for transactional settings drafts.</summary>
public sealed class WinFormsApplicationSettingsStore(IApplicationSettingsContext applicationSettingsContext) : IApplicationSettingsStore
{
    private readonly IApplicationSettingsContext _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));

    public Task<ApplicationSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SnippetSettings snippets = ReadSnippets();
        return Task.FromResult(LegacyApplicationSettingsMapper.ToSnapshot(_applicationSettingsContext.Config, snippets));
    }

    public async Task SaveAsync(ApplicationSettingsDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationConfig candidate = LegacyApplicationSettingsMapper.ToLegacy(draft);
        SnippetSettings snippets = draft.Snippets.Clone();
        string configPath = _applicationSettingsContext.ConfigMainFile;
        string snippetsPath = Path.Combine(_applicationSettingsContext.ConfigDirectory, "snipets.json");
        Directory.CreateDirectory(_applicationSettingsContext.ConfigDirectory);

        string? configTemp = null;
        string? snippetsTemp = null;
        byte[]? oldConfig = File.Exists(configPath) ? await File.ReadAllBytesAsync(configPath, cancellationToken).ConfigureAwait(false) : null;
        byte[]? oldSnippets = File.Exists(snippetsPath) ? await File.ReadAllBytesAsync(snippetsPath, cancellationToken).ConfigureAwait(false) : null;
        try
        {
            if (_applicationSettingsContext.DoSaveConfig)
            {
                configTemp = configPath + ".tmp-" + Guid.NewGuid().ToString("N");
                string configJson = JsonSerializer.Serialize(candidate, MyJsonContextApplicationConfig.Default.ApplicationConfig);
                await File.WriteAllTextAsync(configTemp, configJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }

            snippetsTemp = snippetsPath + ".tmp-" + Guid.NewGuid().ToString("N");
            var legacySnippets = new Snipets
            {
                Keywords = snippets.Keywords.ToArray(),
                Snippets = snippets.Snippets.ToArray(),
                MonkeySnippets = snippets.MonkeySnippets.ToArray()
            };
            string snippetsJson = JsonSerializer.Serialize(legacySnippets, MyJsonContextSnipets.Default.Snipets);
            await File.WriteAllTextAsync(snippetsTemp, snippetsJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            if (configTemp is not null)
            {
                File.Move(configTemp, configPath, true);
                configTemp = null;
            }

            File.Move(snippetsTemp, snippetsPath, true);
            snippetsTemp = null;

            if (_applicationSettingsContext.DoSaveConfig)
            {
                LegacyApplicationSettingsMapper.ApplyToLegacy(draft, _applicationSettingsContext.Config);
            }

            DynamicCollectionForNettezaHelpers.Keywords = snippets.Keywords.ToArray();
            DynamicCollectionForNettezaHelpers.Snippets = snippets.Snippets.ToArray();
            DynamicCollectionForNettezaHelpers.MonkeySnippets = snippets.MonkeySnippets.ToArray();
        }
        catch
        {
            RestoreFile(configPath, oldConfig);
            RestoreFile(snippetsPath, oldSnippets);
            throw;
        }
        finally
        {
            DeleteIfPresent(configTemp);
            DeleteIfPresent(snippetsTemp);
        }
    }

    private SnippetSettings ReadSnippets()
    {
        string path = Path.Combine(_applicationSettingsContext.ConfigDirectory, "snipets.json");
        if (!File.Exists(path))
        {
            return new SnippetSettings
            {
                Keywords = DynamicCollectionForNettezaHelpers.Keywords?.ToList() ?? [],
                Snippets = DynamicCollectionForNettezaHelpers.Snippets?.ToList() ?? [],
                MonkeySnippets = DynamicCollectionForNettezaHelpers.MonkeySnippets?.ToList() ?? []
            };
        }

        string json = File.ReadAllText(path);
        Snipets? legacy = JsonSerializer.Deserialize(json, MyJsonContextSnipets.Default.Snipets);
        return new SnippetSettings
        {
            Keywords = legacy?.Keywords?.ToList() ?? [],
            Snippets = legacy?.Snippets?.ToList() ?? [],
            MonkeySnippets = legacy?.MonkeySnippets?.ToList() ?? []
        };
    }

    private static void RestoreFile(string path, byte[]? original)
    {
        if (original is null)
        {
            DeleteIfPresent(path);
            return;
        }

        File.WriteAllBytes(path, original);
    }

    private static void DeleteIfPresent(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
