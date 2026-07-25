namespace JustData.Application.Settings;

public interface IApplicationSettingsStore
{
    Task<ApplicationSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ApplicationSettingsDraft draft, CancellationToken cancellationToken = default);
}
