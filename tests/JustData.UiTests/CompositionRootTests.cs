using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.Application.Login;
using JustData.Application.Variables;
using JustData.ViewModels.Editor;
using JustData.ViewModels.Explorer;
using JustData.ViewModels.Files;
using JustData.ViewModels.Variables;
using JustyBaseLegacy.UI;
using JustyBaseLegacy.UI.ImportExport;
using JustyBaseLegacy.UI.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JustData.UiTests;

public sealed class CompositionRootTests
{
    [Fact]
    public void Window_graph_is_scoped_and_application_session_is_shared()
    {
        var services = new ServiceCollection();
        Program.ConfigureServices(services);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = false
        });
        IApplicationSession session = provider.GetRequiredService<IApplicationSession>();
        var settings = provider.GetRequiredService<IApplicationSettingsContext>();
        var settingsBootstrap = provider.GetRequiredService<IApplicationSettingsBootstrapContext>();
        var settingsPersistence = provider.GetRequiredService<IApplicationSettingsPersistence>();
        var recentRuntime = provider.GetRequiredService<IRecentFileRuntimeContext>();
        var sessionStore = provider.GetRequiredService<ISessionVariableStore>();
        var sessionRuntime = provider.GetRequiredService<ISessionVariableRuntimeContext>();

        using IServiceScope firstScope = provider.CreateScope();
        var firstDispatcher = firstScope.ServiceProvider.GetRequiredService<IUiDispatcher>();
        var firstSession = firstScope.ServiceProvider.GetRequiredService<IApplicationSession>();
        var firstFileWatch = firstScope.ServiceProvider.GetRequiredService<IFileWatchService>();
        var firstEditorFileWatch = firstScope.ServiceProvider.GetRequiredService<IEditorFileWatchService>();
        var firstFiles = firstScope.ServiceProvider.GetRequiredService<FilesViewModel>();
        var firstWorkspace = firstScope.ServiceProvider.GetRequiredService<EditorWorkspaceViewModel>();
        var firstDatabaseExplorer = firstScope.ServiceProvider.GetRequiredService<DatabaseExplorerViewModel>();
        var firstObjectExplorer = firstScope.ServiceProvider.GetRequiredService<ObjectExplorerViewModel>();
        var firstVariables = firstScope.ServiceProvider.GetRequiredService<VariablesViewModel>();
        var firstImportOperation = firstScope.ServiceProvider.GetRequiredService<IImportOperationService>();
        var firstTabManager = firstScope.ServiceProvider.GetRequiredService<ITabManager>();
        var firstDatabaseRuntime = firstScope.ServiceProvider.GetRequiredService<IDatabaseRuntimeContext>();
        var firstCompletionContext = firstScope.ServiceProvider.GetRequiredService<INetezzaCompletionContext>();
        var firstCompletionRuntimeContext = firstScope.ServiceProvider.GetRequiredService<INetezzaCompletionRuntimeContext>();
        var firstSchemaTables = firstScope.ServiceProvider.GetRequiredService<AppBase.Data.Core.Interfaces.INetezzaSchemaTableCatalog>();
        var firstSnippetContext = firstScope.ServiceProvider.GetRequiredService<ISnippetInitializationContext>();
        var firstTabNameProvider = firstScope.ServiceProvider.GetRequiredService<ITabNameProvider>();
        var firstDdlProvider = firstScope.ServiceProvider.GetRequiredService<INetezzaDdlCodeProvider>();
        var firstChatViewModel = firstScope.ServiceProvider.GetRequiredService<JustData.ViewModels.Ai.ChatViewModel>();
        var firstChatService = firstScope.ServiceProvider.GetRequiredService<JustyBase.Ai.Services.ICopilotChatService>();
        var firstChatSettings = firstScope.ServiceProvider.GetRequiredService<JustyBase.Ai.Ports.IChatSettingsStore>();

        firstScope.Dispose();
        Assert.Throws<ObjectDisposedException>(() => firstFileWatch.Watch([], _ => { }));
        Assert.Throws<ObjectDisposedException>(() => firstEditorFileWatch.Watch("disposed.sql", _ => { }));

        using IServiceScope secondScope = provider.CreateScope();
        var secondDispatcher = secondScope.ServiceProvider.GetRequiredService<IUiDispatcher>();
        var secondFiles = secondScope.ServiceProvider.GetRequiredService<FilesViewModel>();
        var secondWorkspace = secondScope.ServiceProvider.GetRequiredService<EditorWorkspaceViewModel>();
        var secondTabManager = secondScope.ServiceProvider.GetRequiredService<ITabManager>();

        Assert.Same(session, firstSession);
        Assert.Same(settings, settingsBootstrap);
        Assert.Same(settings, settingsPersistence);
        Assert.Same(settings, recentRuntime);
        Assert.Same(sessionStore, sessionRuntime);
        Assert.Same(settings.Config, firstDatabaseRuntime.Config);
        Assert.Same(firstDatabaseRuntime, firstCompletionContext);
        Assert.Same(firstDatabaseRuntime, firstCompletionRuntimeContext);
        Assert.Same(firstDatabaseRuntime, firstSchemaTables);
        Assert.Same(firstSnippetContext, firstTabNameProvider);
        Assert.IsType<LegacyNetezzaDdlCodeProvider>(firstDdlProvider);
        Assert.NotSame(firstDispatcher, secondDispatcher);
        Assert.NotSame(firstFiles, secondFiles);
        Assert.NotSame(firstWorkspace, secondWorkspace);
        Assert.NotSame(firstTabManager, secondTabManager);
        Assert.NotNull(firstDatabaseExplorer);
        Assert.NotNull(firstObjectExplorer);
        Assert.NotNull(firstVariables);
        Assert.NotNull(firstImportOperation);
        Assert.NotNull(firstChatViewModel);
        Assert.NotNull(firstChatService);
        Assert.NotNull(firstChatSettings);

        using IServiceScope windowScope = provider.CreateScope();
        // BaseWindow takes the ChatViewModel as a constructor dependency; the
        // chat dispatcher resolves the window lazily, so this must not cycle.
        // A headless run may fail inside WinForms InitializeComponent — but the
        // DI graph itself (all chat services + the window parameters) must resolve.
        try
        {
            _ = windowScope.ServiceProvider.GetRequiredService<BaseWindow>();
        }
        catch (Exception resolutionFailure)
        {
            Assert.DoesNotContain(
                "circular dependency",
                resolutionFailure.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
