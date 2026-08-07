using AppBase.Common.Interfaces;
using JustyBase.Ai.Embedded.Settings;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>
/// Maps the host <see cref="IApplicationConfig"/> EmbeddedFim* settings onto the
/// shared <see cref="FimSettings"/> port.
/// </summary>
public sealed class LegacyFimSettingsStore : IFimSettingsStore
{
    private readonly IApplicationSettingsContext _settings;

    public LegacyFimSettingsStore(IApplicationSettingsContext settings)
    {
        _settings = settings;
    }

    public FimSettings Settings => Map(_settings.Config);

    public void Update(Action<FimSettings> mutate)
    {
        var copy = Map(_settings.Config);
        mutate(copy);
        Apply(_settings.Config, copy);
        (_settings as IApplicationSettingsPersistence)?.SaveConfig();
    }

    private static FimSettings Map(AppBase.Common.Configuration.IApplicationConfig config)
    {
        return new FimSettings
        {
            EnableFimAi = config.EnableEmbeddedFimAi,
            FimModelId = config.EmbeddedFimModelId,
            FimDebounceMs = config.EmbeddedFimDebounceMs,
            FimMaxTokens = config.EmbeddedFimMaxTokens,
            FimMaxPromptTokens = config.EmbeddedFimMaxPromptTokens,
            FimPrefixPercentage = config.EmbeddedFimPrefixPercentage,
            FimSuffixPercentage = config.EmbeddedFimSuffixPercentage,
            FimPreset = config.EmbeddedFimPreset,
            FimGpuLayers = config.EmbeddedFimGpuLayers,
            FimCtxSize = config.EmbeddedFimCtxSize,
            FimPreferVulkan = config.LlamaServerPreferVulkan
        };
    }

    private static void Apply(AppBase.Common.Configuration.IApplicationConfig config, FimSettings settings)
    {
        config.EnableEmbeddedFimAi = settings.EnableFimAi;
        config.EmbeddedFimModelId = settings.FimModelId;
        config.EmbeddedFimDebounceMs = settings.FimDebounceMs;
        config.EmbeddedFimMaxTokens = settings.FimMaxTokens;
        config.EmbeddedFimMaxPromptTokens = settings.FimMaxPromptTokens;
        config.EmbeddedFimPrefixPercentage = settings.FimPrefixPercentage;
        config.EmbeddedFimSuffixPercentage = settings.FimSuffixPercentage;
        config.EmbeddedFimPreset = settings.FimPreset;
        config.EmbeddedFimGpuLayers = settings.FimGpuLayers;
        config.EmbeddedFimCtxSize = settings.FimCtxSize;
        // The shared llama-server binary variant must be driven by the same preference the
        // embedded chat backend uses, otherwise chat GPU-layer requests can target an avx2
        // binary (or a Vulkan binary with 0 GPU layers).
        config.LlamaServerPreferVulkan = settings.FimPreferVulkan;
    }
}
