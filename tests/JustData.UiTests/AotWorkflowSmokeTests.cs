using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;

namespace JustData.UiTests;

/// <summary>
/// AOT-specific smoke tests that launch the native-compiled executable,
/// log in with the default profile, and exercise key panels that are most
/// vulnerable to trimming/PInvoke issues: the SQL editor, files/variables
/// panels, database explorer tree, object explorer grid, and diagnostics.
/// </summary>
public sealed class AotWorkflowSmokeTests
{
    private const string MainWindowId = "NetezzaSQL_addedFastColored";

    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string AotExePath = Path.Combine(
        RepoRoot,
        "JustData",
        "bin",
        "Release",
        "net10.0-windows10.0.22000.0",
        "win-x64",
        "publish",
        "JustyBaseLegacy.exe");

    // ──────────────────────────────────────────────
    //  SQL editor
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_SQL_editor_is_present_and_enabled()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(MainWindowId)),
            "the SQL editor in the AOT-compiled app");

        Assert.NotNull(editor);
        Assert.True(editor.IsEnabled,
            "The SQL editor should be enabled after login.");
    }

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_SQL_editor_accepts_typing()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(MainWindowId)),
            "the SQL editor in the AOT-compiled app");

        Assert.NotNull(editor);

        // Focus the editor and type a simple SELECT statement
        session.MainWindow.Focus();
        editor.Focus();
        Thread.Sleep(500);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type("SELECT 1 AS AOT_TEST");

        // Give the editor time to process the input
        Thread.Sleep(500);

        // Verify the text was accepted (read via clipboard)
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(300);

        string? clipboardText = null;
        var clipboardThread = new Thread(() =>
        {
            try { clipboardText = System.Windows.Forms.Clipboard.GetText(); }
            catch { }
        });
        clipboardThread.SetApartmentState(ApartmentState.STA);
        clipboardThread.Start();
        Assert.True(clipboardThread.Join(TimeSpan.FromSeconds(5)),
            "Timed out reading clipboard for editor text.");

        Assert.Contains("AOT_TEST", clipboardText ?? string.Empty);
    }

    // ──────────────────────────────────────────────
    //  Files & variables panels
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_Files_and_variables_panels_have_automation_ids()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("dgvVariables")));
        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("filesTreeView")));
        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("textBoxFileSearch")));
    }

    // ──────────────────────────────────────────────
    //  Database Explorer (MVVM)
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_Database_explorer_mvvm_controls_are_present()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        var dbExplorer = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
        Assert.NotNull(dbExplorer);

        Assert.NotNull(dbExplorer.FindFirstDescendant(
            cf => cf.ByAutomationId("cbWhatDb")));
        Assert.NotNull(dbExplorer.FindFirstDescendant(
            cf => cf.ByAutomationId("tbFastSchemaSearch")));
        Assert.NotNull(dbExplorer.FindFirstDescendant(
            cf => cf.ByAutomationId("dgvFastDbBrowser")));
        Assert.NotNull(dbExplorer.FindFirstDescendant(
            cf => cf.ByAutomationId("databaseTreeView")));

        // Verify the explorer is tall enough to be useful (not clipped/zero-height)
        Assert.True(dbExplorer.BoundingRectangle.Height >= 100,
            $"The database explorer is clipped to {dbExplorer.BoundingRectangle.Height}px in the AOT build.");
    }

    // ──────────────────────────────────────────────
    //  Object Explorer (MVVM DataGridView)
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_Object_explorer_grid_is_present()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        var objExplorer = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("mvvmObjectExplorerControl"));
        Assert.NotNull(objExplorer);

        // The explorer hosts a ThemedDataGridView — verify at least the grid itself
        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("dgvObjectExplorer")));
    }

    // ──────────────────────────────────────────────
    //  Diagnostics panel
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_Diagnostics_panel_elements_are_present()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("diagnosticsGrid")));
        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("diagnosticsSearchBox")));
        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("diagnosticsSeverityFilter")));
    }

    // ──────────────────────────────────────────────
    //  DockPanel Suite stability
    // ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_DockPanel_stable_after_login()
    {
        AssertSkipIfAotExeMissing();
        using var session = LaunchAndLogin();

        AutomationElement? dockPanel = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("dockPanel"));
        Assert.NotNull(dockPanel);
    }

    // ──────────────────────────────────────────────
    //  Helper methods
    // ──────────────────────────────────────────────

    private static void AssertSkipIfAotExeMissing()
    {
        if (!File.Exists(AotExePath))
        {
            Assert.Fail($"AOT-published executable not found at: {AotExePath}{Environment.NewLine}" +
                        "Run the AOT publish first:" +
                        "  dotnet publish JustData/JustData.csproj -p:UseAOT=true -p:SelfContained=true -c Release");
        }
    }

    private static UiSession LaunchAndLogin()
    {
        UiTestHelpers.KillExistingInstances();
        var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = AotExePath,
            UseShellExecute = false
        });
        var automation = new UIA3Automation();
        var process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window login = UiTestHelpers.WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window from the AOT-compiled exe");

            // Press the Save & Select button to log in with the default profile
            UiTestHelpers.WaitFor(
                () => login.FindFirstDescendant(
                    cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button in the AOT login window")
                .Invoke();

            Window main = UiTestHelpers.WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId(MainWindowId)) is not null),
                "the main JustData window from the AOT-compiled exe",
                timeout: TimeSpan.FromSeconds(45));

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

    private sealed class UiSession : IDisposable
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
}
