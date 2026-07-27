using FlaUI.Core.AutomationElements;

namespace JustData.UiTests;

public sealed class Phase9SmokeUiTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void Login_and_core_panels_are_available()
    {
        using var session = UiTestHelpers.LaunchAndLogin();

        foreach (string id in new[]
        {
            "_addedFastColored", "dgvVariables", "filesTreeView",
            "mvvmDatabaseExplorerControl", "mvvmObjectExplorerControl",
            "diagnosticsGrid"
        })
        {
            AutomationElement? element = session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(id));
            Assert.True(element is not null, $"Missing core UI AutomationId '{id}'.");
        }
    }
}

