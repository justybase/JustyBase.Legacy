using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace JustData.UiTests;

public sealed class Phase9RegressionUiTests
{
    private const string MainWindowId = "_addedFastColored";

    [Fact]
    [Trait("Category", "Regression")]
    public void Sequential_operations_do_not_deadlock()
    {
        using var session = UiTestHelpers.LaunchAndLogin();

        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(MainWindowId)),
            "the SQL editor");
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type("SELECT 1");
        Keyboard.Press(VirtualKeyShort.F5);

        AutomationElement resultGrid = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByControlType(ControlType.DataGrid)),
            "the SQL result grid",
            timeout: TimeSpan.FromMinutes(2));
        Assert.NotNull(resultGrid);

        UiTestHelpers.OpenPreferences(session.MainWindow);

        AutomationElement? cancelButton = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("cancelPreferencesButton"));
        Assert.NotNull(cancelButton);
        cancelButton.AsButton().Invoke();

        CloseMainWindow(session);
    }

    private static void CloseMainWindow(UiSession session)
    {
        session.MainWindow.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.F4);

        for (int i = 0; i < 5 && !session.Process.HasExited; i++)
        {
            Thread.Sleep(500);

            Window? modal = session.Application.GetAllTopLevelWindows(session.Automation)
                .FirstOrDefault(w => w != session.MainWindow
                    && w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button)) is not null);

            if (modal is not null)
            {
                var confirmButtons = modal.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                AutomationElement? yesButton = confirmButtons
                    .Select(b => b.AsButton())
                    .Cast<AutomationElement?>()
                    .FirstOrDefault(b => b?.Name is "Yes" or "Tak" or "&Yes");
                yesButton ??= confirmButtons.FirstOrDefault();
                yesButton?.AsButton()?.Invoke();
            }
        }

        bool exited = session.Process.WaitForExit(10_000);
        if (!exited)
        {
            session.Application.Kill();
            session.Process.WaitForExit(5_000);
        }
        Assert.True(exited, "The application did not exit within the expected timeout after closing the window.");
    }
}
