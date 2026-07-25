namespace AppBase.Common.Interfaces;

/// <summary>
/// Process-startup surface for selecting and loading the application settings file.
/// </summary>
public interface IApplicationSettingsBootstrapContext : IApplicationSettingsContext
{
    new string ConfigDirectory { get; set; }
    new string ConfigMainFile { get; set; }
    void Initialize();
    void ReadConfig();
}
