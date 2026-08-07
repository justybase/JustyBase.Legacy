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
        // Shared FIM settings port.
        collection.AddSingleton<IFimSettingsStore, LegacyFimSettingsStore>();

        // GGUF catalog + store (shared llama-server model layer).
        collection.AddSingleton<FimModelCatalog>();
        collection.AddSingleton<IModelCatalog>(sp => sp.GetRequiredService<FimModelCatalog>());
        collection.AddKeyedSingleton<IModelStore>(FimStoreKey, (sp, _) =>
        {
            var catalog = sp.GetRequiredService<FimModelCatalog>();
            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return new HuggingFaceModelStore(catalog, () => settings.Settings.FimModelId);
        });

        // llama-server binary + subprocess manager (shared with the AI chat backend).
        collection.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IFimSettingsStore>();
            return new LlamaServerBinaryManager(() => settings.Settings.FimPreferVulkan);
        });
        collection.AddSingleton<LlamaServerManager>();

        // FIM provider (llama.cpp native FIM templates).
        collection.AddSingleton<ICompletionProvider>(sp =>
        {
            var manager = sp.GetRequiredService<LlamaServerManager>();
            var store = sp.GetRequiredKeyedService<IModelStore>(FimStoreKey);
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

        var layers = settings.FimGpuLayers;
        if (layers < 0)
            return 99;

        return Math.Clamp(layers, 0, 999);
    }
}
