namespace JustData.Application.Settings;

/// <summary>
/// Optional host-side persistence boundary for the last valid model selected for
/// each AI chat provider.
/// </summary>
public interface IChatProviderModelSettingsStore
{
    string? GetModelForProvider(string providerId);

    void SetModelForProvider(string providerId, string modelId);
}
