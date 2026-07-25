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

public sealed class SqlExecutionUiTests
{
    private const string ConnectionName = "test_nz_connection";
    private const string Sql = "SELECT * FROM JUST_DATA..DIMDATE";

    [Fact]
    [Trait("Category", "UI")]
    public void TestoweConnection_ExecutesDimDateQuery()
    {
        string credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JustyBaseLegacy",
            "credentials.json.enc");

        Assert.True(
            File.Exists(credentialsPath),
            $"The real local credentials file was not found: {credentialsPath}");

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
                "the Login window")
                ?? throw new TimeoutException("The Login window was not found.");

            WaitFor(
                () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox"))?.AsComboBox(),
                "the connection selector");
            // The real local file contains test_nz_connection as its default profile. WinForms exposes
            // the combo-box items through a popup UIA tree, so the validated default profile
            // is used directly.

            FlaUI.Core.AutomationElements.Button select = WaitFor(
                () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button");
            select.Invoke();

            Window mainWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null),
                "the main JustData window");

            AutomationElement editor = WaitFor(
                () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")),
                "the SQL editor");
            editor.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(Sql);
            Keyboard.Press(VirtualKeyShort.F5);

            AutomationElement resultGrid = WaitFor(
                () => mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid)),
                "the SQL result grid",
                timeout: TimeSpan.FromMinutes(2));

            AutomationElement[] rows = WaitFor(
                () => resultGrid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem)),
                "at least one row in the SQL result grid",
                value => value is { Length: > 0 },
                timeout: TimeSpan.FromMinutes(2));

            Assert.NotEmpty(rows);
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
