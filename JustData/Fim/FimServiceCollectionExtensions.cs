using AppBase.Common.Interfaces;
using JustyBase.Ai.Embedded.Abstractions;
using JustyBase.Ai.Embedded.Download;
using JustyBase.Ai.Embedded.Prompting;
using JustyBase.Ai.Embedded.Server;
using JustyBase.Ai.Embedded.Settings;
using JustyBase.Ai.Git;
using JustyBase.Core.Git;
using Microsoft.Extensions.DependencyInjection;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>
/// Registers the embedded FIM completion pipeline for the WinForms host. The model
/// runtime is the shared llama.cpp llama-server (same binary/subprocess layer as the
/// embedded AI chat backend); the FCTB editor glue stays host-side.
/// </summary>
public static class FimServiceCollectionExtensions
{
    public const string FimStoreKey = "fim";

    public static IServiceCollection AddEmbeddedFimCompletion(this IServiceCollection collection)
    {
        // Apple Silicon runs the native MLX backend (uv + mlx_lm.server); everything else uses
        // the bundled llama.cpp llama-server with GGUF models.
        var useMlx = AppleSiliconRuntime.IsSupported;

        // Shared FIM settings port.
        collection.AddSingleton<IFimSettingsStore, LegacyFimSettingsStore>();

        // Catalog + store (MLX snapshot on Apple Silicon, GGUF otherwise).
        collection.AddSingleton<FimModelCatalog>();
        collection.AddSingleton<IModelCatalog>(sp => sp.GetRequiredService<FimModelCatalog>());
        collection.AddKeyedSingleton<IModelStore>(FimStoreKey, (sp, _) =>
        {
            var catalog = sp.GetRequiredService<FimModelCatalog>();
            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return useMlx
                ? (IModelStore)new HuggingFaceMlxRepoStore(catalog, () => settings.Settings.FimModelId)
                : new HuggingFaceModelStore(catalog, () => settings.Settings.FimModelId);
        });

        // Runtime + subprocess manager (shared with the AI chat backend).
        if (useMlx)
        {
            collection.AddSingleton<MlxServerRuntime>();
        }

        collection.AddSingleton(sp =>
        {
            if (useMlx)
            {
                return new LlamaServerManager(sp.GetRequiredService<MlxServerRuntime>());
            }

            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return new LlamaServerManager(new LlamaServerBinaryManager(() => settings.Settings.FimPreferVulkan));
        });

        // FIM provider (MLX /v1/completions on Apple Silicon, llama.cpp native FIM otherwise).
        collection.AddSingleton<ICompletionProvider>(sp =>
        {
            var manager = sp.GetRequiredService<LlamaServerManager>();
            var store = sp.GetRequiredKeyedService<IModelStore>(FimStoreKey);
            if (useMlx)
            {
                return new MlxFimProvider(manager, store);
            }

            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return new LlamaServerFimProvider(
                manager,
                store,
                getGpuLayers: () => ResolveGpuLayers(settings.Settings),
                getContextSize: () => (uint)Math.Clamp(
                    settings.Settings.FimCtxSize > 0 ? settings.Settings.FimCtxSize : 4096, 512, 131_072));
        });

        // Editor bridge (FCTB host).
        collection.AddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<ICompletionProvider>();
            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return new FimInlineCompletionBridge(
                provider,
                () => settings.Settings.EnableFimAi,
                () => new FimPromptBudget(
                    settings.Settings.FimMaxPromptTokens,
                    settings.Settings.FimPrefixPercentage,
                    settings.Settings.FimSuffixPercentage,
                    settings.Settings.FimMaxTokens));
        });

        // Model bootstrap (download / delete / reload / speed test over the server).
        collection.AddSingleton<IFimModelBootstrapService>(sp =>
        {
            var provider = sp.GetRequiredService<ICompletionProvider>();
            var store = sp.GetRequiredKeyedService<IModelStore>(FimStoreKey);
            var manager = sp.GetRequiredService<LlamaServerManager>();
            return new LlamaServerFimBootstrapService(provider, store, manager);
        });

        // FCTB editor host + shared llama-server git commit message AI.
        collection.AddSingleton<FimEditorHost>();
        collection.AddSingleton<IGitCommitMessageAiService>(sp =>
        {
            var settings = sp.GetRequiredService<IFimSettingsStore>();
            if (!settings.Settings.EnableFimAi)
            {
                return new UnavailableGitCommitMessageAiService();
            }

            var store = sp.GetRequiredKeyedService<IModelStore>(FimStoreKey);
            return new LlamaServerGitCommitMessageAiService(
                sp.GetRequiredService<LlamaServerManager>(),
                store,
                settings);
        });

        return collection;
    }

    private static int ResolveGpuLayers(FimSettings settings)
    {
        if (!settings.FimPreferVulkan)
            return 0;

        // Negative = auto: llama-server offloads as many layers as fit in VRAM.
        var layers = settings.FimGpuLayers;
        if (layers < 0)
            return -1;

        return Math.Clamp(layers, 0, 999);
    }
}
