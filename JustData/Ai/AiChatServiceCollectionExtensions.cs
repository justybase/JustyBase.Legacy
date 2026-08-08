using JustyBase.Ai.Chat;
using JustyBase.Ai.Embedded.Abstractions;
using JustyBase.Ai.Embedded.Download;
using JustyBase.Ai.Embedded.Server;
using JustyBase.Ai.Ports;
using JustyBase.Ai.Services;
using AppBase.Common;
using JustData.ViewModels.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace JustyBaseLegacy.UI.Ai;

/// <summary>
/// Registers the shared AI chat pipeline for the WinForms host: backend clients,
/// the chat service, tool executor/state provider, embedded llama-server management
/// and the host port adapters.
/// </summary>
public static class AiChatServiceCollectionExtensions
{
    public static IServiceCollection AddAiChatServices(this IServiceCollection collection)
    {
        // Host port adapters (scoped to the login shell — the dispatcher needs the main window).
        collection.AddScoped<IChatSettingsStore, LegacyChatSettingsStore>();
        collection.AddScoped<IUiDispatcher>(sp =>
            new LazyWinFormsUiDispatcher(() => sp.GetRequiredService<BaseWindow>()));
        collection.AddScoped<IChatDatabaseAccessProvider, LegacyChatDatabaseAccessProvider>();
        collection.AddScoped<ISqlDiagnosticsProvider, LegacySqlDiagnosticsProvider>();
        collection.AddScoped<ISimpleLogger>(sp => new LegacyChatLogger(sp.GetRequiredService<ILogger>()));
        collection.AddScoped<IChatEnvironment, LegacyChatEnvironment>();

        // Embedded llama-server (chat backend). The runtime/subprocess manager is registered by
        // AddEmbeddedFimCompletion (shared with the FIM pipeline) and reads the same host Vulkan
        // preference; on Apple Silicon that manager runs the MLX backend instead.
        collection.AddSingleton<EmbeddedChatModelCatalog>();
        collection.AddKeyedScoped<IModelStore>(EmbeddedChatBackend.ChatModelStoreKey, (sp, _) =>
        {
            var settings = sp.GetRequiredService<IChatSettingsStore>();
            var catalog = sp.GetRequiredService<EmbeddedChatModelCatalog>();
            return AppleSiliconRuntime.IsSupported
                ? (IModelStore)new HuggingFaceMlxRepoStore(catalog, () => settings.Settings.EmbeddedChatModelId)
                : new HuggingFaceModelStore(catalog, () => settings.Settings.EmbeddedChatModelId);
        });

        // Backends.
        collection.AddScoped<OpenAiCompatibleChatBackend>();
        collection.AddScoped<ILocalChatBackend>(sp => sp.GetRequiredService<OpenAiCompatibleChatBackend>());
        collection.AddScoped<EmbeddedChatBackend>();
        collection.AddScoped<ILocalChatBackend>(sp => sp.GetRequiredService<EmbeddedChatBackend>());
        collection.AddScoped<LocalChatClientFactory>();
        collection.AddScoped<LocalStateProvider>();
        collection.AddScoped<ILocalStateProvider>(sp => sp.GetRequiredService<LocalStateProvider>());
        collection.AddScoped<LocalModelConfigurationService>();
        collection.AddScoped<ILocalModelConfigurationService>(sp => sp.GetRequiredService<LocalModelConfigurationService>());
        collection.AddScoped<SqlExecutionErrorStore>();
        collection.AddScoped<CodexAppServerClient>();
        collection.AddScoped<LocalChatService>();
        collection.AddScoped<ICopilotChatService>(sp => sp.GetRequiredService<LocalChatService>());

        // Shared session orchestration + view model host.
        collection.AddScoped<ChatSessionController>();
        collection.AddScoped<ChatViewModel>();

        return collection;
    }
}
