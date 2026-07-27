using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Common.WindowManagement;
using AppBase.Services;
using AppBase.Data.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.Login;
using JustData.Application.Startup;
using JustyBaseLegacy.UI.Configuration;
using JustyBaseLegacy.UI.Login;
using JustyBaseLegacy.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

/// <summary>
/// Owns the process-level startup sequence and the lifetime of the scoped shell.
/// </summary>
internal sealed class AppBootstrapper
{
    private const string AppGuid = "56349a5c-66cf-4611-b886-f85772f9ea77";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWindowManagementService _windowManagementService;
    private readonly IApplicationSettingsBootstrapContext _settingsBootstrapContext;
    private readonly ISnippetInitializationContext _snippetInitializationContext;
    private readonly IApplicationSettingsPersistence _settingsPersistence;
    private readonly IUiHelperService _uiHelperService;
    private readonly IApplicationSession _applicationSession;
    private readonly GeneralDbSessionAdapter _sessionAdapter;
    private readonly LoginFormFactory _loginFormFactory;
    private readonly INetezzaAutocompleteState _netezzaAutocompleteState;

    public AppBootstrapper(
        IServiceScopeFactory scopeFactory,
        IWindowManagementService windowManagementService,
        IApplicationSettingsBootstrapContext settingsBootstrapContext,
        ISnippetInitializationContext snippetInitializationContext,
        IApplicationSettingsPersistence settingsPersistence,
        IUiHelperService uiHelperService,
        IApplicationSession applicationSession,
        GeneralDbSessionAdapter sessionAdapter,
        LoginFormFactory loginFormFactory,
        INetezzaAutocompleteState netezzaAutocompleteState)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _windowManagementService = windowManagementService ?? throw new ArgumentNullException(nameof(windowManagementService));
        _settingsBootstrapContext = settingsBootstrapContext ?? throw new ArgumentNullException(nameof(settingsBootstrapContext));
        _snippetInitializationContext = snippetInitializationContext ?? throw new ArgumentNullException(nameof(snippetInitializationContext));
        _settingsPersistence = settingsPersistence ?? throw new ArgumentNullException(nameof(settingsPersistence));
        _uiHelperService = uiHelperService ?? throw new ArgumentNullException(nameof(uiHelperService));
        _applicationSession = applicationSession ?? throw new ArgumentNullException(nameof(applicationSession));
        _sessionAdapter = sessionAdapter ?? throw new ArgumentNullException(nameof(sessionAdapter));
        _loginFormFactory = loginFormFactory ?? throw new ArgumentNullException(nameof(loginFormFactory));
        _netezzaAutocompleteState = netezzaAutocompleteState ?? throw new ArgumentNullException(nameof(netezzaAutocompleteState));
    }

    public int Run(string[] args)
    {
        // ── Early-exit checks before any Windows/Dispatch initialization ──
        // Smoke test must run first so it never touches the mutex, even when
        // a previous process was killed and left an abandoned mutex handle.
        if (StartupArguments.IsSmokeTest(args))
        {
            // Resolving these services is the historical smoke check. Constructor injection
            // additionally verifies the process-level services used by the normal startup path.
            _ = _settingsBootstrapContext;
            _ = _applicationSession;
            return 0;
        }

        if (TryRunLoginScreenshotUiTest(args))
        {
            return 0;
        }

        if (TryRunPreferencesUiTest(args))
        {
            return 0;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using Mutex mutex = new(false, "Local\\" + AppGuid);
        bool ownsMutex;
        try
        {
            ownsMutex = mutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            // A previous process was killed (e.g. by KillExistingInstances)
            // without releasing the mutex. The current thread still acquired
            // ownership — treat as "owns mutex" and proceed.
            ownsMutex = true;
        }
        if (StartupArguments.ShouldForwardToExistingInstance(ownsMutex, args))
        {
            Process _ = _windowManagementService.SendMessageToAnotherInstances(args);
            return 0;
        }

        if (StartupArguments.ShouldShowAlreadyRunning(ownsMutex, args))
        {
            _ = MessageBox.Show(
                "JustyBaseLegacy is already running.",
                "Already running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        if (!ownsMutex)
        {
            // silent/script invocations intentionally exit without a dialog.
            return 0;
        }

        _settingsBootstrapContext.Initialize();
        if (_settingsBootstrapContext.Config is null)
        {
            return 0;
        }

        ApplyDocumentationDarkThemeIfRequested(args);

        Application.SetColorMode(
            _settingsBootstrapContext.Config.UseSpecialColoring
                ? SystemColorMode.Dark
                : SystemColorMode.Classic);

        if (!StartupArguments.ShouldRunLogin(args))
        {
            return 0;
        }

        using LoginForm loginForm = _loginFormFactory.Create();
        Program.LogStartup("Login form created");

        if (_settingsBootstrapContext.Config.FastLogin)
        {
            loginForm.ChoseFirst();
            Program.LogStartup($"Fast login completed; selection available: {loginForm.Result is not null}");
        }
        else
        {
            DialogResult loginDialogResult = loginForm.ShowDialog();
            Program.LogStartup($"Login dialog completed with result {loginDialogResult}; selection available: {loginForm.Result is not null}");
            if (loginDialogResult != DialogResult.OK)
            {
                return 0;
            }
        }

        LoginSelection selection = loginForm.Result
            ?? throw new InvalidOperationException("A login selection is required before opening the main window.");
        _applicationSession.SetLogin(selection, loginForm.Profiles);
        _sessionAdapter.Apply(_applicationSession);

        using IServiceScope scope = _scopeFactory.CreateScope();
        Program.LogStartup("Resolving main window");
        BaseWindow mainWindow = scope.ServiceProvider.GetRequiredService<BaseWindow>();
        mainWindow.FormClosed += (_, args) =>
            Program.LogStartup($"Main window closed with reason {args.CloseReason}");
        Program.LogStartup($"Starting main message loop; disposed: {mainWindow.IsDisposed}");
        Application.Run(mainWindow);
        Program.LogStartup("Main message loop completed");
        return 0;
    }

    private bool TryRunLoginScreenshotUiTest(string[] args)
    {
        if (!StartupArguments.IsLoginScreenshotUiTest(args))
        {
            return false;
        }

        string outputPath = Path.GetFullPath(args[1]);

        _settingsBootstrapContext.Initialize();
        if (_settingsBootstrapContext.Config is null)
        {
            return true;
        }

        ApplyDocumentationDarkThemeIfRequested(args);

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetColorMode(
            _settingsBootstrapContext.Config.UseSpecialColoring
                ? SystemColorMode.Dark
                : SystemColorMode.Classic);

        using LoginForm loginForm = _loginFormFactory.Create();
        loginForm.SuppressBlurOverlay = true;
        loginForm.DocumentationScreenshotPath = outputPath;
        loginForm.FormClosed += (_, _) => Application.ExitThread();
        loginForm.Show();
        Application.Run();
        return true;
    }

    private bool TryRunPreferencesUiTest(string[] args)
    {
        if (!StartupArguments.IsPreferencesUiTest(args))
        {
            return false;
        }

        string configDirectory = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(configDirectory);

        _settingsBootstrapContext.ConfigDirectory = configDirectory;
        _settingsBootstrapContext.ConfigMainFile = Path.Combine(configDirectory, "config.json");
        _settingsBootstrapContext.ReadConfig();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using PreferencesForm preferences = new(
            repaintApplication: static () => { },
            saveManySqlToDisk: static () => { },
            _settingsBootstrapContext,
            _snippetInitializationContext,
            _settingsPersistence.SaveConfig,
            _settingsPersistence.SaveRecentFiles,
            _uiHelperService,
            new ColorTheme(_settingsBootstrapContext.Config),
            _netezzaAutocompleteState);
        preferences.ShowInTaskbar = true;
        preferences.FormClosed += (_, _) => Application.ExitThread();
        preferences.Show();
        Application.Run();
        return true;
    }

    private void ApplyDocumentationDarkThemeIfRequested(string[] args)
    {
        if (!StartupArguments.IsDocumentationDarkTheme(args))
        {
            return;
        }

        if (_settingsBootstrapContext.Config is ApplicationConfig config)
        {
            config.UseSpecialColoring = true;
        }
    }
}
