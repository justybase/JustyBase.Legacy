using AppBase.Common.Configuration;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Narrow configuration persistence surface used by settings adapters.
/// </summary>
public interface IApplicationSettingsContext
{
    IApplicationConfig Config { get; }
    string ConfigDirectory { get; }
    string ConfigMainFile { get; }
    bool DoSaveConfig { get; }
}
