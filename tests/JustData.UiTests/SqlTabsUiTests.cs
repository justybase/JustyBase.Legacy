using AppBase.Common;
using AppBase.Services;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Text.Json;

namespace JustData.UiTests;

public sealed class SqlTabsUiTests
{
    private const string ConnectionName = "test_nz_connection";
    private const string FirstSql = "SELECT 1 AS TAB_ONE";
    private const string SecondSql = "SELECT 2 AS TAB_TWO";

    [Fact]
    [Trait("Category", "UI")]
    public void SqlTabs_SwitchingTabsAlsoSwitchesTheirResults()
    {
        string credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JustyBaseLegacy",
            "credentials.json.enc");

        Assert.True(File.Exists(credentialsPath), $"The real local credentials file was not found: {credentialsPath}");
        CredentialStoreReadResult credentials = new CredentialStore().Read(credentialsPath);
        List<LoginData> profiles = JsonSerializer.Deserialize<List<LoginData>>(credentials.Content) ?? [];
        Assert.NotEmpty(profiles);
        int defaultIndex = Math.Clamp(profiles[0].DefaultIndex, 0, profiles.Count - 1);
        Assert.Equal(ConnectionName, profiles[defaultIndex].Name);

        UiTestHelpers.KillExistingInstances();
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using UIA3Automation automation = new();
        using Process process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window loginWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window");

            WaitFor(
                () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button")
                .Invoke();

            Window mainWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null),
                "the main JustData window");

            AutomationElement firstEditor = WaitFor(
                () => FindEditors(mainWindow).FirstOrDefault(),
                "the first SQL editor");
            SetEditorText(firstEditor, FirstSql);
            Keyboard.Press(VirtualKeyShort.F5);

            AutomationElement firstResult = WaitFor(
                () => FindVisibleResultGridContaining(mainWindow, "TAB_ONE"),
                "the TAB_ONE result grid");

            // Verify the application shortcut while the result grid owns focus.
            // BaseWindow must route Ctrl+N to the main SQL document tab view.
            firstResult.Click();
            Thread.Sleep(250);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);

            AutomationElement secondEditor = WaitFor(
                () => FindEditors(mainWindow).FirstOrDefault(),
                "the active editor in the new SQL tab");
            SetEditorText(secondEditor, SecondSql);
            Keyboard.Press(VirtualKeyShort.F5);

            WaitFor(
                () => FindVisibleResultGridContaining(mainWindow, "TAB_TWO"),
                "the TAB_TWO result grid");

            secondEditor.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.TAB);

            WaitFor(
                () => FindVisibleResultGridContaining(mainWindow, "TAB_ONE"),
                "the TAB_ONE result after switching SQL tabs");
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    application.Kill();
                }
                catch (InvalidOperationException)
                {
                    // The application can already have terminated after a UI failure.
                }

                process.WaitForExit(10_000);
            }
        }
    }

    private static AutomationElement[] FindEditors(Window mainWindow) =>
        mainWindow.FindAllDescendants(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored"));

    private static AutomationElement? FindVisibleResultGridContaining(Window mainWindow, string marker) =>
        mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid))
            .FirstOrDefault(grid => grid.AutomationId == "dataGridView1"
                && !grid.IsOffscreen && GetAccessibleText(grid)
                .Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string GetAccessibleText(AutomationElement element) =>
        string.Join("|", element.FindAllDescendants()
            .Select(descendant => descendant.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)));

    private static void SetEditorText(AutomationElement editor, string sql)
    {
        editor.Click();
        Thread.Sleep(500);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(sql);
    }

    private static T WaitFor<T>(
        Func<T?> read,
        string description,
        Func<T?, bool>? condition = null,
        TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        T? value;
        do
        {
            value = read();
            if (value is not null && (condition is null || condition(value)))
            {
                return value;
            }

            Thread.Sleep(250);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
