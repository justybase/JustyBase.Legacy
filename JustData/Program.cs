using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Services;
using AppBase.Services.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.Communication;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.Application.Login;
using JustData.Application.Variables;
using JustData.ViewModels.Files;
using JustData.ViewModels.Editor;
using JustData.ViewModels;
using JustData.ViewModels.Variables;
using JustData.Application.Schema;
using JustData.Application.Sql;
using JustData.Application.ImportExport;
using JustyBaseLegacy.UI.ImportExport;
using JustData.ViewModels.ImportExport;
using JustData.ViewModels.Explorer;
using JustData.Mvvm;
using JustyBaseLegacy.UI.Schema;
using JustyBaseLegacy.UI.Login;
using JustyBaseLegacy.UI.Files;
using JustyBaseLegacy.UI.Editor;
using JustyBaseLegacy.UI.Windowing;
using JustyBaseLegacy.UI.Sql;
using JustData.Application.History;
using JustData.Application.Git;
using JustData.ViewModels.Git;
using JustyBaseLegacy.UI.Git;
using JustyBaseLegacy.UI.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public static class Program
{
    internal static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JustyBaseLegacy");

    internal static readonly string ErrorLogPath = Path.Combine(LogDirectory, "errors.log");

    internal static readonly string StartupLogPath = Path.Combine(LogDirectory, "startup.log");

    private static void LogToFile(string path, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    internal static void LogStartup(string message)
    {
        FileDiagnosticLog.Write(DiagnosticLogLevel.Info, $"[startup] {message}");
        LogToFile(StartupLogPath, message);
    }

    /// <summary>
    /// The process entry point. Application startup is delegated to AppBootstrapper.
    /// </summary>
    [STAThread]
    static void Main(params string[] args)
    {
        LogToFile(StartupLogPath, "Starting Main");
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            RegisterComWrappers();

            ServiceCollection services = new();
            ConfigureServices(services);
            using ServiceProvider provider = services.BuildServiceProvider();
            LogToFile(StartupLogPath, "Services built, starting AppBootstrapper");
            provider.GetRequiredService<AppBootstrapper>().Run(args);
            LogToFile(StartupLogPath, "Main completed successfully");
        }
        catch (Exception ex)
        {
            LogToFile(ErrorLogPath, $"FATAL: {SanitizeException(ex)}");
            LogToFile(StartupLogPath, $"FATAL: {ex.GetType().FullName}: {ex.Message}");
            Environment.ExitCode = 1;
            try
            {
                MessageBox.Show(
                    $"JustyBaseLegacy could not start. Details were saved to:{Environment.NewLine}{ErrorLogPath}",
                    "JustyBaseLegacy startup error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // A message box may be unavailable during very early startup;
                // the sanitized error is already persisted in errors.log.
            }
        }
    }

    private static void RegisterComWrappers()
    {
    }

    /// <summary>
    /// Registers the application graph. Keeping registration in one callable
    /// composition root makes scope/lifetime tests possible without starting
    /// WinForms.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWindowManagementService, WindowManagementService>();
        services.AddScoped<IUiDispatcher, WindowsFormsUiDispatcher>();
        services.AddSingleton<ILogger, LoggerLoud>();
        services.AddSingleton<IColorTheme>(sp =>
            new ColorTheme(sp.GetRequiredService<IApplicationSettingsContext>().Config));
        services.AddSingleton<LegacyDatabaseRuntimeContext>();
        services.AddSingleton<IDatabaseRuntimeContext>(sp => sp.GetRequiredService<LegacyDatabaseRuntimeContext>());
        services.AddSingleton<INetezzaCompletionContext>(sp => sp.GetRequiredService<LegacyDatabaseRuntimeContext>());
        services.AddSingleton<INetezzaCompletionRuntimeContext>(sp => sp.GetRequiredService<LegacyDatabaseRuntimeContext>());
        services.AddSingleton<INetezzaDdlCodeProvider, LegacyNetezzaDdlCodeProvider>();
        services.AddSingleton<LegacySnippetContext>();
        services.AddSingleton<ISnippetInitializationContext>(sp => sp.GetRequiredService<LegacySnippetContext>());
        services.AddSingleton<IImportExportTasks, ImportExportTasks>();
        services.AddTransient<IFormatterService, FormatterService>();
        services.AddSingleton<IDatabaseProviderFactory, DatabaseProviderFactory>();
        services.AddSingleton<IGeneralDbService, GeneralDbService>();
        services.AddSingleton<IConnectionSessionRegistry>(_ => IGeneralDbService.ConnectionSessions);
        services.AddSingleton<IUiHelperService, UiHelperService>();
        services.AddSingleton<ICredentialStore, CredentialStore>();
        services.AddSingleton<IApplicationSession, ApplicationSession>();
        services.AddSingleton<LoginFormFactory>();
        services.AddSingleton<LegacyApplicationSettingsContext>();
        services.AddSingleton<IApplicationSettingsContext>(sp =>
            sp.GetRequiredService<LegacyApplicationSettingsContext>());
        services.AddSingleton<IApplicationSettingsBootstrapContext>(sp =>
            sp.GetRequiredService<LegacyApplicationSettingsContext>());
        services.AddSingleton<IApplicationSettingsPersistence>(sp =>
            sp.GetRequiredService<LegacyApplicationSettingsContext>());
        services.AddSingleton<IRecentFileRuntimeContext>(sp =>
            sp.GetRequiredService<LegacyApplicationSettingsContext>());
        services.AddSingleton<IRecentFileStore>(sp =>
            sp.GetRequiredService<LegacyApplicationSettingsContext>());
        services.AddSingleton<LegacySessionVariableContext>();
        services.AddSingleton<ISessionVariableStore>(sp => sp.GetRequiredService<LegacySessionVariableContext>());
        services.AddSingleton<ISessionVariableRuntimeContext>(sp => sp.GetRequiredService<LegacySessionVariableContext>());
        services.AddSingleton<INumberFormattingContext, LegacyNumberFormattingContext>();
        services.AddSingleton<ITabNameProvider>(sp => sp.GetRequiredService<LegacySnippetContext>());
        services.AddSingleton<ITextFileContentReader, LegacyTextFileContentReader>();
        services.AddSingleton<IInlineCommandRunner, LegacyInlineCommandRunner>();
        services.AddScoped<VariablesViewModel>();
        services.AddSingleton<IDocumentFileService, WinFormsDocumentFileService>();
        services.AddScoped<IFileWatchService, WinFormsFileWatchService>();
        services.AddSingleton<IEditorFileService, WinFormsEditorFileService>();
        services.AddScoped<IEditorFileWatchService, WinFormsEditorFileWatchService>();
        services.AddSingleton<IManySqlBundleService, WinFormsManySqlBundleService>();
        services.AddSingleton<IEditorDialogService, WinFormsEditorDialogService>();
        services.AddSingleton<IFilePickerService, WinFormsFilePickerService>();
        services.AddScoped<FilesViewModel>();
        services.AddSingleton<IGitService, SystemGitService>();
        services.AddScoped<GitViewModel>();
        services.AddScoped<SqlExecutionEngineContext>();
        services.AddScoped<ISqlExecutionSessionRegistry, SqlExecutionSessionRegistry>();
        // General DB is now provider/event-stream based; its WinForms result
        // rendering is a document presenter rather than a BaseWindow callback.
        services.AddScoped<ISqlExecutionEngine>(sp => new GeneralSqlExecutionEngine(
            sp.GetRequiredService<ISqlExecutionSessionRegistry>(),
            sp.GetRequiredService<IImportExportTasks>(),
            sp.GetRequiredService<IConnectionSessionRegistry>()));
        // Keep the Netezza provider on the transitional presenter until its
        // async reader/export semantics match the clean ADO engine. The
        // production switch is intentionally gated by the real-provider UI
        // tests; the migration constructor remains covered at the contract
        // level while this compatibility path is active.
        services.AddScoped<ISqlExecutionEngine>(sp => new NetezzaSqlExecutionEngine(
            sp.GetRequiredService<SqlExecutionEngineContext>()));
        services.AddScoped<SqlExecutionRouter>();
        services.AddScoped<ISqlExecutionUseCase>(sp => sp.GetRequiredService<SqlExecutionRouter>());
        services.AddSingleton<AppBase.Data.Completion.NetezzaSqlAuthoringUseCase>();
        services.AddSingleton<ISqlAuthoringUseCase, NetezzaSqlAuthoringUseCaseAdapter>();
        services.AddScoped<IImportOperationService, WinFormsImportOperationService>();
        services.AddScoped<WinFormsImportUseCase>();
        services.AddScoped<IImportUseCase>(sp => sp.GetRequiredService<WinFormsImportUseCase>());
        services.AddScoped<WinFormsResultExportUseCase>();
        services.AddScoped<IResultExportUseCase>(sp => sp.GetRequiredService<WinFormsResultExportUseCase>());
        services.AddScoped<IDocumentResultGridRegistry, DocumentResultGridRegistry>();
        services.AddScoped<ImportExportViewModelFactory>();
        services.AddScoped<EditorWorkspaceViewModel>();
        services.AddSingleton<ISchemaRepository, LegacySchemaRepository>();
        services.AddScoped<ISchemaRefreshCoordinator, SchemaRefreshCoordinator>();
        services.AddSingleton<ISchemaDdlService, LegacySchemaDdlService>();
        services.AddScoped<DatabaseExplorerViewModel>();
        services.AddScoped<ObjectExplorerViewModel>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        services.AddSingleton<GeneralDbSessionAdapter>();
        services.AddScoped<ExternalOpenRequestRouter>();
        services.AddScoped<IExternalOpenRequestRouter>(sp =>
            sp.GetRequiredService<ExternalOpenRequestRouter>());
        services.AddScoped<ShellViewModel>();
        services.AddSingleton<AppBootstrapper>();

        services.AddSingleton<INetezzaHelperService, NetezzaHelperService>();
        services.AddSingleton<IDataFuncService, DataFuncService>();
        services.AddSingleton<IFileSearchEngine, FileSearchEngine>();
        services.AddSingleton<ILoginDataValidator, LoginDataValidator>();
        services.AddSingleton<ICodeActionProvider, CodeActionProvider>();
        services.AddSingleton<ISqlPreprocessingService, SqlPreprocessingService>();
        services.AddSingleton<ISpecialCommandService, SpecialCommandService>();
        services.AddSingleton<ISqlRiskAnalysisService, SqlRiskAnalysisService>();
        services.AddSingleton<AppBase.Data.Completion.NetezzaSqlCompletionServices>();
        services.AddSingleton<AppBase.Data.Completion.LegacySqlAuthoringServices>();

        services.AddScoped<ITabManager, DockSuiteTabManager>();
        services.AddSingleton<IHistoryStore, HistoryFileStore>();
        services.AddSingleton<JustData.Application.QueryWatch.IQueryWatchService, JustyBaseLegacy.UI.Services.LegacyQueryWatchService>();
        services.AddScoped<BaseWindow>();
    }

    private static int _handlingUnhandledException;

    private static void OnThreadException(object sender, ThreadExceptionEventArgs args)
    {
        ReportUnhandledException(args.Exception, showMessage: true);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            ReportUnhandledException(exception, showMessage: false);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (args.Exception?.InnerException is Exception inner)
        {
            LogToFile(ErrorLogPath, $"UNOBSERVED TASK: {SanitizeException(inner)}");
            FileDiagnosticLog.WriteError("Unobserved task exception", inner);
        }
    }

    private static void ReportUnhandledException(Exception exception, bool showMessage)
    {
        if (Interlocked.Exchange(ref _handlingUnhandledException, 1) != 0)
        {
            return;
        }

        string safeException = SanitizeException(exception);
        LogToFile(ErrorLogPath, safeException);
        FileDiagnosticLog.WriteError("Unhandled exception", exception);

        if (showMessage)
        {
            MessageBox.Show(
                "JustyBaseLegacy encountered an unexpected error and will close. The error was saved to the application log.",
                "Unexpected error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.ExitThread();
        }
    }

    private static string SanitizeException(Exception exception)
    {
        return SensitiveDataRedactor.RedactException(exception);
    }
}
