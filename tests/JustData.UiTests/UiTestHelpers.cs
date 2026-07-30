using AppBase.Common;
using AppBase.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace JustData.UiTests;

/// <summary>
/// Shared helpers for FlaUI-based UI tests. Provides process cleanup,
/// a standard launch-and-login workflow, and common assertion/polling utilities.
/// </summary>
internal static class UiTestHelpers
{
    private const string MainWindowId = "_addedFastColored";
    private const string ExeName = "JustyBaseLegacy.exe";
    private const string ConnectionName = "NPS_144";

    /// <summary>
    /// Kills any existing instances of JustyBaseLegacy.exe so tests don't
    /// fail with "Already running" mutex errors. Safe to call multiple times.
    /// </summary>
    internal static void KillExistingInstances()
    {
        foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName)))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5_000);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited between the check and Kill.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Access denied (e.g. system process or different session).
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    /// <summary>
    /// Establishes a predictable desktop before a FlaUI session starts.
    /// This is the equivalent of pressing Win+D on the interactive workstation.
    /// </summary>
    internal static void MinimizeAllWindows()
    {
        using (Keyboard.Pressing(VirtualKeyShort.LWIN))
        {
            Keyboard.Press(VirtualKeyShort.KEY_D);
        }
        Thread.Sleep(200);
    }

    /// <summary>
    /// Launches the app and waits for the login window without connecting.
    /// </summary>
    internal static LoginUiSession LaunchToLoginScreen(string? exePath = null)
    {
        MinimizeAllWindows();
        KillExistingInstances();

        exePath ??= Path.Combine(AppContext.BaseDirectory, ExeName);
        var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false
        });
        var automation = new UIA3Automation();
        var process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window login = WaitFor(
                () => TryFindLoginDialog(application, automation),
                "the Login window");
            login.Focus();
            return new LoginUiSession(application, automation, process, login);
        }
        catch
        {
            automation.Dispose();
            if (!process.HasExited)
                application.Kill();
            application.Dispose();
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resolves <c>docs/images</c> from the repository root (walks up from the test output directory).
    /// </summary>
    internal static string GetDocumentationImagesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string docs = Path.Combine(directory.FullName, "docs", "images");
            if (Directory.Exists(docs))
            {
                return docs;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate docs/images in the repository.");
    }

    internal static void SaveWindowScreenshot(Window window, string filePath)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Focus();
        Thread.Sleep(300);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        Capture.Element(window).ToFile(filePath);
    }

    /// <summary>
    /// Opens Preferences via the real top-level menu (docked document tab in the main window).
    /// </summary>
    internal static void OpenPreferences(Window mainWindow)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        // Preferences is a top-level menu item (not under Settings). WinForms ToolStripMenuItem
        // does not reliably expose Name as UIA AutomationId; UIA Name = "Preferences" works.
        mainWindow.Focus();
        AutomationElement? preferencesItem = mainWindow.FindFirstDescendant(
            cf => cf.ByName("Preferences"));
        Assert.NotNull(preferencesItem);
        preferencesItem.Click();
        Thread.Sleep(500);

        WaitFor(
            () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("modernPreferencesRoot"))
                ?? mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("cancelPreferencesButton")),
            "the Preferences document tab",
            timeout: TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Closes transient Error / message dialogs that would otherwise appear in documentation screenshots.
    /// </summary>
    internal static void DismissBlockingDialogs(UiSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Window? dialog = session.Application.GetAllTopLevelWindows(session.Automation)
                .FirstOrDefault(window =>
                    !ReferenceEquals(window, session.MainWindow)
                    && IsBlockingDocumentationDialog(window));

            if (dialog is null)
            {
                break;
            }

            AutomationElement? closeButton =
                dialog.FindFirstDescendant(cf => cf.ByName("OK"))
                ?? dialog.FindFirstDescendant(cf => cf.ByName("Ok"))
                ?? dialog.FindFirstDescendant(cf => cf.ByAutomationId("Close"))
                ?? dialog.FindFirstDescendant(cf => cf.ByName("Close"));

            if (closeButton is not null)
            {
                closeButton.AsButton()?.Invoke();
            }
            else
            {
                try
                {
                    dialog.Close();
                }
                catch (Exception)
                {
                    dialog.Focus();
                    Keyboard.Press(VirtualKeyShort.ESCAPE);
                }
            }

            Thread.Sleep(250);
        }

        session.MainWindow.Focus();
        Thread.Sleep(200);
    }

    private static bool IsBlockingDocumentationDialog(Window window)
    {
        try
        {
            string title = window.Title ?? string.Empty;
            if (title.Equals("Error", StringComparison.OrdinalIgnoreCase)
                || title.Equals("Exception", StringComparison.OrdinalIgnoreCase)
                || title.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string text = GetAccessibleText(window);
            return text.Contains("communication error", StringComparison.OrdinalIgnoreCase)
                || text.Contains("One or more errors occurred", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Unable to read data", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Próba połączenia", StringComparison.OrdinalIgnoreCase)
                || text.Contains("transport connection", StringComparison.OrdinalIgnoreCase);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

    internal static void SetSqlEditorText(AutomationElement editor, string sql)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentException.ThrowIfNullOrEmpty(sql);
        editor.Click();
        Thread.Sleep(250);
        SetClipboardText(sql);
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
            FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
            FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
        Thread.Sleep(200);
    }

    /// <summary>
    /// Copies the whole SQL editor content through the same WinForms clipboard
    /// path a user would use. This makes selection-regression checks possible
    /// even though FastColoredTextBox does not expose a stable ValuePattern.
    /// </summary>
    internal static string CopySqlEditorText(AutomationElement editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        editor.Focus();
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
            FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
            FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_C);
        Thread.Sleep(150);
        return ReadClipboardText();
    }

    private static readonly string FlaUiStepLogPath = Path.Combine(
        Path.GetTempPath(),
        "justybase-flaui-steps.log");

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    internal static void FlaUiStep(string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        Trace.WriteLine("[FlaUI] " + line);
        try
        {
            File.AppendAllText(FlaUiStepLogPath, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// After login: schema refresh + startup file restore (BIG.SQL) should settle.
    /// </summary>
    internal static void WaitForPostLoginIdle(Window main, TimeSpan? schemaTimeout = null)
    {
        FlaUiStep("WaitForPostLoginIdle: start");
        TimeSpan timeout = schemaTimeout ?? TimeSpan.FromSeconds(90);
        try
        {
            WaitFor(
                () =>
                {
                    var status = main.FindFirstDescendant(cf => cf.ByAutomationId("statusTextBox"));
                    string? text = status?.AsTextBox()?.Text;
                    return text?.Contains("Schema downloaded", StringComparison.OrdinalIgnoreCase) == true
                        ? status
                        : null;
                },
                "schema downloaded (status bar)",
                timeout: timeout);
        }
        catch (TimeoutException)
        {
            FlaUiStep("WaitForPostLoginIdle: schema status not seen; continuing after fixed delay");
        }

        Thread.Sleep(2_000);
        FlaUiStep("WaitForPostLoginIdle: done (+2s after schema)");
    }

    internal static void BringSessionToForeground(UiSession session)
    {
        FlaUiStep("BringSessionToForeground");
        try
        {
            if (!session.Process.HasExited && session.Process.MainWindowHandle != IntPtr.Zero)
                SetForegroundWindow(session.Process.MainWindowHandle);
        }
        catch (Exception ex)
        {
            FlaUiStep("SetForegroundWindow failed: " + ex.GetType().Name);
        }

        try
        {
            session.MainWindow.Focus();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
        }

        Thread.Sleep(300);
    }

    internal static AutomationElement FindSqlEditor(Window main) =>
        WaitFor(
            () =>
            {
                AutomationElement? editor = main.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored"))
                    ?? main.FindFirstDescendant(cf => cf.ByAutomationId("_addedFastColored"));
                if (editor is null)
                    return null;
                var rect = editor.BoundingRectangle;
                if (rect.Width < 20 || rect.Height < 20)
                    return null;
                return editor;
            },
            "the visible SQL editor",
            timeout: TimeSpan.FromSeconds(120));

    /// <summary>
    /// Mouse focus on FCTB then Ctrl+End (FCTB GoLastLine — scroll + caret to last line).
    /// </summary>
    internal static void FocusSqlEditorAtDocumentEnd(Window main, UiSession session)
    {
        FlaUiStep("FocusSqlEditorAtDocumentEnd: start");
        BringSessionToForeground(session);

        Window window = WaitFor(
            () => TryFindMainWindow(session.Application, session.Automation),
            "main window (refreshed)",
            timeout: TimeSpan.FromSeconds(30));
        AutomationElement editor = FindSqlEditor(window);

        Keyboard.Press(VirtualKeyShort.ESCAPE);
        Thread.Sleep(200);

        var rect = editor.BoundingRectangle;
        FlaUiStep($"editor rect=({rect.X},{rect.Y},{rect.Width}x{rect.Height})");

        // Click lower area so caret lands near visible bottom before GoLastLine.
        int x = (int)(rect.X + Math.Max(40, rect.Width / 2));
        int y = (int)(rect.Y + rect.Height - 40);
        Mouse.MoveTo(x, y);
        Thread.Sleep(100);
        Mouse.Click(MouseButton.Left);
        Thread.Sleep(600);

        FlaUiStep("sending Ctrl+End (GoLastLine)");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.END);
        Thread.Sleep(2_000);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.END);
        Thread.Sleep(1_000);
        FlaUiStep("FocusSqlEditorAtDocumentEnd: done");
    }

    internal static string ReadClipboardText()
    {
        string? result = null;
        Exception? exception = null;
        Thread thread = new(() =>
        {
            try
            {
                result = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("Timed out while reading SQL from the clipboard.");
        if (exception is not null)
            ExceptionDispatchInfo.Capture(exception).Throw();
        return result ?? string.Empty;
    }

    private static void SetClipboardText(string text)
    {
        Exception? exception = null;
        Thread thread = new(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Timed out while writing SQL to the clipboard.");
        }

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    internal static Window? TryFindLoginDialog(FlaUI.Core.Application application, UIA3Automation automation)
    {
        List<Window> candidates = application.GetAllTopLevelWindows(automation)
            .Where(window => window.FindFirstDescendant(
                cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null)
            .ToList();

        Window? titled = candidates.FirstOrDefault(window =>
            string.Equals(window.Title, "Login", StringComparison.OrdinalIgnoreCase));
        if (titled is not null)
        {
            return titled;
        }

        return candidates
            .OrderBy(window => window.BoundingRectangle.Width * window.BoundingRectangle.Height)
            .FirstOrDefault();
    }

    internal static Window? TryFindMainWindow(FlaUI.Core.Application application, UIA3Automation automation)
    {
        Window[] windows = application.GetAllTopLevelWindows(automation);

        foreach (Window window in windows)
        {
            try
            {
                if (window.FindFirstDescendant(cf => cf.ByAutomationId(MainWindowId)) is not null)
                    return window;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // UIA can expose a stale window while WinForms is rebuilding
                // the top-level tree. Try the next window on this poll.
            }
        }

        foreach (Window window in windows)
        {
            try
            {
                if (window.Title.StartsWith("JustyBaseLegacy", StringComparison.OrdinalIgnoreCase))
                    return window;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // The next poll obtains fresh UIA wrappers.
            }
        }

        return null;
    }

    /// <summary>
    /// Launches the standard JustyBaseLegacy.exe, logs in with the default
    /// profile, and returns a session handle. Any pre-existing instances
    /// are killed first.
    /// </summary>
    internal static UiSession LaunchAndLogin(
        string? exePath = null,
        bool useDarkTheme = false,
        bool navigateDocumentationDimDate = false,
        bool documentationShowcaseLayout = false,
        string? openSqlFilePath = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        MinimizeAllWindows();
        KillExistingInstances();

        exePath ??= Path.Combine(AppContext.BaseDirectory, ExeName);
        var launchArguments = new List<string>();
        if (useDarkTheme)
        {
            launchArguments.Add("--dark");
        }

        if (navigateDocumentationDimDate)
        {
            launchArguments.Add("--ui-test-navigate-dimdate");
        }

        if (documentationShowcaseLayout)
        {
            launchArguments.Add("--ui-test-showcase-layout");
        }

        if (!string.IsNullOrWhiteSpace(openSqlFilePath))
        {
            launchArguments.Add("--ui-test-open-file=" + openSqlFilePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false
        };
        foreach (string argument in launchArguments)
            startInfo.ArgumentList.Add(argument);
        if (environment is not null)
        {
            foreach (var pair in environment)
                startInfo.Environment[pair.Key] = pair.Value;
        }

        var application = FlaUI.Core.Application.Launch(startInfo);
        var automation = new UIA3Automation();
        var process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window login = WaitFor(
                () => TryFindLoginDialog(application, automation),
                "the Login window");
            WaitFor(
                () => login.FindFirstDescendant(
                    cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button").Invoke();

            Window main = WaitFor(
                () => TryFindMainWindow(application, automation),
                "the main JustData window");
            return new UiSession(application, automation, process, main);
        }
        catch
        {
            automation.Dispose();
            if (!process.HasExited)
                application.Kill();
            application.Dispose();
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Polls until <paramref name="read"/> returns a non-null value
    /// (optionally matching <paramref name="condition"/>) or the timeout elapses.
    /// </summary>
    internal static T WaitFor<T>(
        Func<T?> read,
        string description,
        Func<T, bool>? condition = null,
        TimeSpan? timeout = null)
        where T : class
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        do
        {
            T? value = read();
            if (value is not null && (condition is null || condition(value)))
                return value;
            Thread.Sleep(250);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for {description}.");
    }

    /// <summary>
    /// Verifies that the "NPS_144" profile exists in the local credentials file.
    /// </summary>
    internal static void EnsureTestoweProfile()
    {
        string credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JustyBaseLegacy",
            "credentials.json.enc");

        Assert.True(File.Exists(credentialsPath),
            $"The real local credentials file was not found: {credentialsPath}");

        CredentialStoreReadResult credentials = new CredentialStore().Read(credentialsPath);
        List<LoginData> profiles = JsonSerializer.Deserialize<List<LoginData>>(credentials.Content) ?? [];
        Assert.NotEmpty(profiles);
        int defaultIndex = Math.Clamp(profiles[0].DefaultIndex, 0, profiles.Count - 1);
        Assert.Equal(ConnectionName, profiles[defaultIndex].Name);
    }

    /// <summary>
    /// Returns the number of data rows in a DataGridView, using the UIA
    /// GridPattern when available and falling back to DataItem count.
    /// </summary>
    internal static int GetRowCount(FlaUI.Core.AutomationElements.DataGridView grid)
    {
        return grid.Patterns.Grid.TryGetPattern(out var pattern)
            ? pattern.RowCount
            : grid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem)).Length;
    }

    /// <summary>
    /// Returns the UIA accessible text of all descendants of <paramref name="element"/>.
    /// </summary>
    internal static string GetAccessibleText(AutomationElement element)
    {
        return string.Join("|", element.FindAllDescendants()
            .Select(descendant => descendant.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)));
    }
}

/// <summary>
/// Manages the lifetime of a FlaUI-automated JustyBaseLegacy session.
/// Kills the process, disposes the automation and application on Dispose().
/// </summary>
internal sealed class UiSession : IDisposable
{
    public UiSession(
        FlaUI.Core.Application application,
        UIA3Automation automation,
        Process process,
        Window mainWindow)
    {
        Application = application;
        Automation = automation;
        Process = process;
        MainWindow = mainWindow;
    }

    public FlaUI.Core.Application Application { get; }
    public UIA3Automation Automation { get; }
    public Process Process { get; }
    public Window MainWindow { get; }

    public void Dispose()
    {
        if (!Process.HasExited)
        {
            try { Application.Kill(); }
            catch (InvalidOperationException) { }
            Process.WaitForExit(10_000);
        }
        Automation.Dispose();
        Application.Dispose();
        Process.Dispose();
    }
}

/// <summary>
/// Login window only — dispose kills the process without connecting to a database.
/// </summary>
internal sealed class LoginUiSession : IDisposable
{
    public LoginUiSession(
        FlaUI.Core.Application application,
        UIA3Automation automation,
        Process process,
        Window loginWindow)
    {
        Application = application;
        Automation = automation;
        Process = process;
        LoginWindow = loginWindow;
    }

    public FlaUI.Core.Application Application { get; }
    public UIA3Automation Automation { get; }
    public Process Process { get; }
    public Window LoginWindow { get; }

    public void Dispose()
    {
        if (!Process.HasExited)
        {
            try { Application.Kill(); }
            catch (InvalidOperationException) { }
            Process.WaitForExit(10_000);
        }
        Automation.Dispose();
        Application.Dispose();
        Process.Dispose();
    }
}
