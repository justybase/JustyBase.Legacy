using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Common.JsonContext;
using System.Text;
using System.Text.Json;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Owns persisted snippet bootstrap and deterministic tab-name selection.
/// </summary>
public sealed class LegacySnippetContext : ISnippetInitializationContext, ITabNameProvider
{
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private string[]? _specialNames;

    public LegacySnippetContext(IApplicationSettingsContext applicationSettingsContext)
    {
        _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
    }

    public void Initialize(string snippetsJson, string specialNamesJson)
    {
        string configDirectory = _applicationSettingsContext.ConfigDirectory;
        Directory.CreateDirectory(configDirectory);
        if (!File.Exists(Path.Combine(configDirectory, "snipets.json"))
            && !File.Exists(Path.Combine(configDirectory, "snipets.json.enc")))
        {
            File.WriteAllText(Path.Combine(configDirectory, "snipets.json"), snippetsJson, Encoding.UTF8);
        }

        Directory.CreateDirectory(Path.Combine(configDirectory, "backup"));
        Directory.CreateDirectory(Path.Combine(configDirectory, "data"));

        if (!_applicationSettingsContext.Config.UseSpecialTabNames)
        {
            return;
        }

        string specialNamesPath = Path.Combine(configDirectory, "special_names.json");
        if (!File.Exists(specialNamesPath))
        {
            File.WriteAllText(specialNamesPath, specialNamesJson, Encoding.UTF8);
        }

        _specialNames = JsonSerializer.Deserialize(
            File.ReadAllText(specialNamesPath),
            MyJsonContextStringArray.Default.StringArray);

        if (_specialNames is not null)
        {
            Random.Shared.Shuffle(_specialNames);
        }
    }

    public string GetNextName(HashSet<string> existingTabNames)
    {
        ArgumentNullException.ThrowIfNull(existingTabNames);

        if (_applicationSettingsContext.Config.UseSpecialTabNames && _specialNames is not null)
        {
            string? availableSpecialName = _specialNames.FirstOrDefault(name => !existingTabNames.Contains(name));
            if (availableSpecialName is not null)
            {
                return availableSpecialName;
            }
        }

        for (int i = 1; i < 100; i++)
        {
            string proposal = $"tab{i}";
            if (!existingTabNames.Contains(proposal))
            {
                return proposal;
            }
        }

        return "xyz";
    }
}
