using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Diagnostics;

namespace JustData.UiTests;

public sealed class ExplorerUiTests
{
    [Fact]
    [Trait("Category", "UI")]
    public void Explorer_tree_expands_connection_to_tables()
    {
        UiTestHelpers.EnsureTestoweProfile();
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);
        try
        {
            var login = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login");
            login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))!.AsButton().Invoke();

            var main = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null), "main window");

            // Verify MVVM control is present and sufficiently tall
            var mvvmExplorer = main.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
            Assert.NotNull(mvvmExplorer);
            Assert.True(mvvmExplorer.BoundingRectangle.Height >= 200,
                $"The explorer tool is clipped to {mvvmExplorer.BoundingRectangle.Height}px.");

            // Verify legacy automation IDs resolve
            foreach (string id in new[] { "databaseTreeView", "dgvFastDbBrowser", "dgvObjectExplorer" })
                Assert.NotNull(main.FindFirstDescendant(cf => cf.ByAutomationId(id)));

            // Expand tree: connection → database → schema → tables
            var tree = mvvmExplorer.FindFirstDescendant(cf => cf.ByAutomationId("databaseTreeView"));
            Assert.NotNull(tree);

            var connection = tree.FindFirstDescendant(cf => cf.ByName("test_nz_connection"));
            Assert.NotNull(connection);
            connection.Click();
            if (connection.Patterns.ExpandCollapse.IsSupported)
                connection.Patterns.ExpandCollapse.Pattern.Expand();

            var database = WaitFor(
                () => connection.FindAllChildren().FirstOrDefault(child => !string.IsNullOrWhiteSpace(child.Name)),
                "database child after expanding connection",
                TimeSpan.FromSeconds(90));
            Assert.NotNull(database);
            database.Click();
            if (database.Patterns.ExpandCollapse.IsSupported)
                database.Patterns.ExpandCollapse.Pattern.Expand();

            var schema = WaitFor(
                () => database.FindAllChildren().FirstOrDefault(child => !string.IsNullOrWhiteSpace(child.Name)),
                "schema child after expanding database",
                TimeSpan.FromSeconds(90));
            Assert.NotNull(schema);
            schema.Click();
            if (schema.Patterns.ExpandCollapse.IsSupported)
                schema.Patterns.ExpandCollapse.Pattern.Expand();

            Assert.Contains(schema.FindAllChildren(), child => !string.IsNullOrWhiteSpace(child.Name));
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); } catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    private static bool IsDimDateNode(FlaUI.Core.AutomationElements.AutomationElement element)
    {
        try
        {
            return element.Name.Equals("DIMDATE", StringComparison.OrdinalIgnoreCase)
                || element.Name.EndsWith(".DIMDATE", StringComparison.OrdinalIgnoreCase);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Lazy tree expansion replaces loading nodes while UIA is walking
            // the subtree. Retry with a fresh tree handle on the next poll.
            return false;
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Database_explorer_toolbar_buttons_are_present()
    {
        UiTestHelpers.EnsureTestoweProfile();
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);
        try
        {
            var login = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login");
            login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))!.AsButton().Invoke();

            var main = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null), "main window");

            var explorer = main.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
            Assert.NotNull(explorer);

            // Verify toolbar buttons by their text content (avoid ControlType
            // which throws PropertyNotSupportedException on some child elements)
            var allDescendants = explorer!.FindAllDescendants();
            var allNames = allDescendants
                .Select(e => { try { return e.Name; } catch { return string.Empty; } })
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToArray();

            Assert.Contains("+ Add", allNames);
            Assert.Contains("⚙ Edit", allNames);
            Assert.Contains("↻", allNames);
            Assert.Contains("⊟", allNames);
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); } catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Database_explorer_mvvm_controls_have_correct_automation_ids()
    {
        UiTestHelpers.EnsureTestoweProfile();
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);
        try
        {
            var login = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login");
            login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))!.AsButton().Invoke();

            var main = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null), "main window");

            var explorer = main.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
            Assert.NotNull(explorer);

            // Verify migrated MVVM controls are present (IsEnabled may be false during schema load)
            Assert.NotNull(explorer.FindFirstDescendant(cf => cf.ByAutomationId("cbWhatDb")));
            Assert.NotNull(explorer.FindFirstDescendant(cf => cf.ByAutomationId("tbFastSchemaSearch")));
            Assert.NotNull(explorer.FindFirstDescendant(cf => cf.ByAutomationId("dgvFastDbBrowser")));
            Assert.NotNull(explorer.FindFirstDescendant(cf => cf.ByAutomationId("databaseTreeView")));
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); } catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Database_explorer_escape_clears_filter_box()
    {
        UiTestHelpers.EnsureTestoweProfile();
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);
        try
        {
            var login = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login");
            login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))!.AsButton().Invoke();

            var main = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null), "main window");

            var explorer = main.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
            Assert.NotNull(explorer);

            var filterBox = explorer!.FindFirstDescendant(cf => cf.ByAutomationId("tbFastSchemaSearch"))!.AsTextBox();
            filterBox.Focus();
            // Use Text setter instead of Keyboard.Type for reliable input
            filterBox.Text = "DIMDATE";
            Assert.Equal("DIMDATE", filterBox.Text);

            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Thread.Sleep(500);
            Assert.Equal(string.Empty, filterBox.Text);
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); } catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Database_explorer_search_finds_DIMDATE()
    {
        UiTestHelpers.EnsureTestoweProfile();
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);
        try
        {
            var login = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null), "Login");
            login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))!.AsButton().Invoke();

            var main = WaitFor(() => application.GetAllTopLevelWindows(automation)
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null), "main window");

            var explorer = main.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl"));
            Assert.NotNull(explorer);

            // Type DIMDATE in the schema search filter box — Text setter triggers
            // TextChanged → search timer (250ms debounce) → SearchAsync → results.
            var filterBox = explorer.FindFirstDescendant(cf => cf.ByAutomationId("tbFastSchemaSearch"))!.AsTextBox();
            filterBox.Focus();
            filterBox.Text = "DIMDATE";
            Assert.Equal("DIMDATE", filterBox.Text);

            // Wait up to 90s for DIMDATE to appear in the search results grid.
            // SearchAsync uses ConnectionName (set by InitializeAsync after login)
            // to query the repository for matching schema objects.
            var searchGrid = explorer.FindFirstDescendant(cf => cf.ByAutomationId("dgvFastDbBrowser"))!.AsDataGridView();

            FlaUI.Core.AutomationElements.DataGridViewRow? dimdateRow = WaitFor(
                () =>
                {
                    try
                    {
                        return searchGrid.Rows
                            .Cast<FlaUI.Core.AutomationElements.DataGridViewRow>()
                            .FirstOrDefault(r => r.Cells.Cast<FlaUI.Core.AutomationElements.DataGridViewCell>()
                                .Any(c =>
                                {
                                    try
                                    {
                                        return c.Value?.ToString()?.IndexOf("DIMDATE", StringComparison.OrdinalIgnoreCase) >= 0;
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                }));
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        return null;
                    }
                },
                "DIMDATE in search results",
                TimeSpan.FromSeconds(90));
            Assert.NotNull(dimdateRow);

            // Verify the name cell specifically matches DIMDATE
            Assert.Contains(dimdateRow.Cells.Cast<FlaUI.Core.AutomationElements.DataGridViewCell>(),
                cell =>
                {
                    try { return "DIMDATE".Equals(cell.Value?.ToString(), StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); } catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    private static T WaitFor<T>(Func<T?> read, string description, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (read() is { } value) return value;
            Thread.Sleep(250);
        }

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
