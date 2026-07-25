using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;

namespace JustData.UiTests;

public sealed class LoginUiTests
{
    [Fact]
    [Trait("Category", "UI")]
    public void Login_preserves_stable_AutomationIds_and_Enter_accepts()
    {
        using var app = Launch();
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(app.ProcessId);
        try
        {
            var login = WaitForLogin(app, automation);
            foreach (var id in new[]
            {
                "userNameTextBox", "passwordTextBox", "serverTextBox", "connectionSelectorComboBox",
                "selectDatabaseButton", "saveBt", "addNewButton", "deleteButton", "nameTextBox",
                "checkBoxFastLogin", "xButton", "btReorder", "checkBox1", "databaseComboBox",
                "DriverComboBox", "rememberAsDefaultCheckBox"
            })
            {
                Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId(id)));
            }

            login.FindFirstDescendant(cf => cf.ByAutomationId("passwordTextBox"))!.Focus();
            Keyboard.Press(VirtualKeyShort.ENTER);
            Assert.True(SpinWait.SpinUntil(
                () => app.GetAllTopLevelWindows(automation).Any(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null),
                TimeSpan.FromSeconds(30)),
                $"Enter did not open the main window. Process exited: {app.HasExited}. Visible windows: {string.Join(", ", app.GetAllTopLevelWindows(automation).Select(window => window.Title))}");
        }
        finally { Stop(app, process); }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Login_Escape_cancels_the_modal_login()
    {
        using var app = Launch();
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(app.ProcessId);
        try
        {
            var login = WaitForLogin(app, automation);
            login.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox"))!.Focus();
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.True(SpinWait.SpinUntil(() => app.HasExited, TimeSpan.FromSeconds(10)), "Escape should cancel login and end startup without opening the main window.");
        }
        finally { Stop(app, process); }
    }

    private static FlaUI.Core.Application Launch()
    {
        UiTestHelpers.KillExistingInstances();
        return FlaUI.Core.Application.Launch(new ProcessStartInfo { FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"), UseShellExecute = false });
    }
    private static Window WaitForLogin(FlaUI.Core.Application app, UIA3Automation automation) => WaitFor(() => app.GetAllTopLevelWindows(automation).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login window");
    private static T WaitFor<T>(Func<T?> read, string description) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var value = read();
            if (value is not null) return value;
            Thread.Sleep(200);
        }
        throw new TimeoutException($"Timed out waiting for {description}.");
    }
    private static void Stop(FlaUI.Core.Application app, Process process)
    {
        if (process.HasExited) return;
        try { app.Kill(); } catch (InvalidOperationException) { }
        process.WaitForExit(10_000);
    }
}
