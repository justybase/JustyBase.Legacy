namespace AppBase.Common.Interfaces;

/// <summary>
/// Explicit persistence actions used by the process shell and Preferences host.
/// </summary>
public interface IApplicationSettingsPersistence
{
    void SaveConfig();
    void SaveRecentFiles();
}
