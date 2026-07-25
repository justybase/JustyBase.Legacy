namespace JustData.Application.Settings;

/// <summary>Applies a non-persisted theme preview and can restore the prior theme.</summary>
public interface ISettingsThemePreview
{
    void Preview(ApplicationSettingsDraft draft);
    void Commit(ApplicationSettingsSnapshot snapshot);
    void Revert();
}
