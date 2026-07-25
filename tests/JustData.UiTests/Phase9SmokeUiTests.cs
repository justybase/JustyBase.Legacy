using FlaUI.Core.AutomationElements;

namespace JustData.UiTests;

/// <summary>
/// Phase 9 smoke tests covering the unchecked manual checklist items that can
/// be verified automatically. Each test covers one or more stable AutomationId
/// presence checks or basic UI workflow scenarios.
/// </summary>
public sealed class Phase9SmokeUiTests
{
    private const string MainWindowId = "NetezzaSQL_addedFastColored";

    [Fact]
    [Trait("Category", "Smoke")]
    public void Login_and_main_window_renders()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement? editor = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId(MainWindowId));
        Assert.NotNull(editor);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Variables_panel_AutomationId_present()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement? grid = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("dgvVariables"));
        Assert.NotNull(grid);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Files_panel_AutomationId_present()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement? tree = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("filesTreeView"));
        Assert.NotNull(tree);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Explorer_panels_AutomationIds_present()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement? dbExplorer = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
        Assert.NotNull(dbExplorer);

        AutomationElement? objExplorer = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("mvvmObjectExplorerControl"));
        Assert.NotNull(objExplorer);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Diagnostics_panel_AutomationIds_present()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        string[] ids = ["diagnosticsGrid", "diagnosticsSearchBox", "diagnosticsSeverityFilter"];
        foreach (string id in ids)
        {
            Assert.NotNull(session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(id)));
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void SQL_editor_is_accessible()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(MainWindowId)),
            "SQL editor");
        Assert.NotNull(editor);
        Assert.True(editor.IsEnabled);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void DockSuite_tab_manager_stable()
    {
        using var session = UiTestHelpers.LaunchAndLogin();
        AutomationElement? dockPanel = session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("dockPanel"));
        Assert.NotNull(dockPanel);
    }
}

