using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace JustData.UiTests;

public sealed class SchemaRefreshRefactorFlaUiTests
{
    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "RefactorPhaseSpot")]
    public void Manual_schema_refresh_populates_mvvm_explorer()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using var session = UiTestHelpers.LaunchAndLogin();

        WaitForSchemaDownloaded(session.MainWindow);

        AutomationElement explorer = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl")),
            "the MVVM database explorer");

        AutomationElement? connectionNode = UiTestHelpers.WaitFor(
            () => explorer.FindFirstDescendant(cf => cf.ByControlType(ControlType.TreeItem))
                ?? session.MainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.TreeItem).And(cf.ByName("NPS_144"))),
            "a connection root in the database explorer",
            timeout: TimeSpan.FromSeconds(60));

        Assert.False(string.IsNullOrWhiteSpace(connectionNode.Name));
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "RefactorPhaseSpot")]
    public void Schema_refresh_updates_connection_database_combo()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using var session = UiTestHelpers.LaunchAndLogin();

        WaitForSchemaDownloaded(session.MainWindow);

        AutomationElement explorer = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl")),
            "the MVVM database explorer");

        AutomationElement comboElement = UiTestHelpers.WaitFor(
            () => explorer.FindFirstDescendant(cf => cf.ByAutomationId("cbWhatDb")),
            "the explorer database combo (cbWhatDb)",
            timeout: TimeSpan.FromSeconds(60));

        FlaUI.Core.AutomationElements.ComboBox? combo = comboElement.AsComboBox();
        Assert.NotNull(combo);

        // DropDownList combos often report Items.Length == 0 until expanded.
        combo.Expand();
        Thread.Sleep(200);
        Assert.True(
            combo.Items.Length > 0 || !string.IsNullOrWhiteSpace(combo.SelectedItem?.Text) || !string.IsNullOrWhiteSpace(combo.Name),
            "Expected cbWhatDb to expose catalog databases after schema refresh.");
    }

    private static void WaitForSchemaDownloaded(Window mainWindow)
    {
        UiTestHelpers.WaitFor(
            () =>
            {
                try
                {
                    var status = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("statusTextBox"));
                    string? text = status?.AsTextBox()?.Text ?? status?.Name;
                    return text?.Contains("Schema downloaded", StringComparison.OrdinalIgnoreCase) == true
                        ? status
                        : null;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    return null;
                }
            },
            "schema downloaded (status bar)",
            timeout: TimeSpan.FromSeconds(90));
    }
}
