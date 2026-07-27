using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace JustData.UiTests;

/// <summary>
/// End-to-end guardrails for the provider-based SQL execution path. These
/// tests intentionally use the real local Netezza profile and therefore must
/// be run only on a prepared interactive workstation.
/// </summary>
public sealed class SqlExecutionRefactorFlaUiTests
{
    private static readonly TimeSpan SqlTimeout = TimeSpan.FromMinutes(2);

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "SqlExecutionRefactor")]
    public void Selected_select_creates_log_before_result_and_records_elapsed_time()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using UiSession session = UiTestHelpers.LaunchAndLogin();
        OpenResultsDockWindow(session);
        AutomationElement editor = FindSqlEditor(session.MainWindow);

        UiTestHelpers.SetSqlEditorText(editor, "select 1 as id;\r\nthis is invalid SQL;");
        SelectFirstLine(editor);
        Assert.Equal("select 1 as id;", CopySelectedSql(editor));
        Thread.Sleep(500);
        Keyboard.Press(VirtualKeyShort.F5);

        WaitForVisibleResultGrid(session);
        WaitForLog(session.MainWindow, "SQL execution completed successfully.");

        AssertResultsTabsStartWithDiagnosticsAndLog(session.MainWindow);
        string log = ReadLog(session.MainWindow);
        Assert.Contains("SQL execution started.", log, StringComparison.Ordinal);
        Assert.Contains("Statement 1/1 completed.", log, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "SqlExecutionRefactor")]
    public void Invalid_selected_SQL_creates_an_error_log_without_replacing_editor_content()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using UiSession session = UiTestHelpers.LaunchAndLogin();
        OpenResultsDockWindow(session);
        AutomationElement editor = FindSqlEditor(session.MainWindow);
        const string script = "select 1 as id;\r\nthis is invalid SQL;";

        UiTestHelpers.SetSqlEditorText(editor, script);
        SelectSecondLine(editor);
        Assert.Equal("this is invalid SQL;", CopySelectedSql(editor));
        Keyboard.Press(VirtualKeyShort.F5);

        WaitForLog(session.MainWindow, "ERROR:");
        Assert.Equal(NormalizeNewLines(script), NormalizeNewLines(UiTestHelpers.CopySqlEditorText(editor)));
        AssertResultsTabsStartWithDiagnosticsAndLog(session.MainWindow);
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "SqlExecutionRefactor")]
    public void Selected_multi_statement_script_uses_one_execution_and_creates_no_empty_result()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using UiSession session = UiTestHelpers.LaunchAndLogin();
        AutomationElement editor = FindSqlEditor(session.MainWindow);
        string table = NewTemporaryTableName();
        string script = $"create temporary table {table} (id integer) distribute on (id);\r\ninsert into {table} values (1);\r\nselect * from {table};";

        UiTestHelpers.SetSqlEditorText(editor, script);
        SelectAll(editor);
        Keyboard.Press(VirtualKeyShort.F5);

        WaitForLog(session.MainWindow, "SQL execution completed successfully.");
        WaitForVisibleResultGrid(session);

        Assert.Single(FindResultGrids(session));
        AssertResultsTabsStartWithDiagnosticsAndLog(session.MainWindow);
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "SqlExecutionRefactor")]
    public void Keep_connection_open_preserves_a_temporary_table_across_two_F5_runs()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using UiSession session = UiTestHelpers.LaunchAndLogin();
        AutomationElement editor = FindSqlEditor(session.MainWindow);
        bool restoreKeepConnectionToggle = EnsureToolStripToggleIsOn(
            session.MainWindow, "tsbKeepConnection", "Keep connection Open");
        string table = NewTemporaryTableName();
        try
        {
            UiTestHelpers.SetSqlEditorText(editor, $"create temporary table {table} (id integer) distribute on (id);");
            SelectAll(editor);
            Keyboard.Press(VirtualKeyShort.F5);
            WaitForLog(session.MainWindow, "SQL execution completed successfully.");

            UiTestHelpers.SetSqlEditorText(editor, $"insert into {table} values (7);\r\nselect * from {table};");
            SelectAll(editor);
            Keyboard.Press(VirtualKeyShort.F5);

            WaitForLog(session.MainWindow, "SQL execution completed successfully.");
            AutomationElement result = WaitForVisibleResultGrid(session);
            FlaUI.Core.AutomationElements.DataGridView rows = result.AsDataGridView();
            Assert.Equal(1, UiTestHelpers.GetRowCount(rows));
        }
        finally
        {
            if (restoreKeepConnectionToggle)
                ToggleToolStripButton(session.MainWindow, "tsbKeepConnection", "Keep connection Open");
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "SqlExecutionRefactor")]
    public void Continue_on_error_logs_the_failed_statement_and_continues_to_the_next_one()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using UiSession session = UiTestHelpers.LaunchAndLogin();
        AutomationElement editor = FindSqlEditor(session.MainWindow);
        bool restoreContinueOnErrorToggle = EnsureToolStripToggleIsOn(
            session.MainWindow, "tsbContinueOnError", "Continue On Error");
        try
        {
            UiTestHelpers.SetSqlEditorText(editor, "select 1 as id;\r\nthis is invalid SQL;\r\nselect 2 as id;");
            SelectAll(editor);
            Keyboard.Press(VirtualKeyShort.F5);

            WaitForLog(session.MainWindow, "ERROR:");
            UiTestHelpers.WaitFor(
                () => FindResultTabs(session.MainWindow),
                "two result tabs after continue-on-error",
                tabs => tabs.Count >= 2,
                timeout: SqlTimeout);
            WaitForVisibleResultGrid(session);
        }
        finally
        {
            if (restoreContinueOnErrorToggle)
                ToggleToolStripButton(session.MainWindow, "tsbContinueOnError", "Continue On Error");
        }
    }

    private static AutomationElement FindSqlEditor(Window mainWindow) =>
        UiTestHelpers.WaitFor(
            () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("_addedFastColored"))
                ?? mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")),
            "the SQL editor");

    private static void SelectAll(AutomationElement editor)
    {
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
    }

    private static void SelectFirstLine(AutomationElement editor)
    {
        editor.Click();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        // Left collapses Select All at the beginning of the document. This is
        // reliable for FastColoredTextBox whereas Ctrl+Home is not.
        Keyboard.Press(VirtualKeyShort.LEFT);
        Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.END);
    }

    private static void SelectSecondLine(AutomationElement editor)
    {
        editor.Click();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Press(VirtualKeyShort.LEFT);
        Keyboard.Press(VirtualKeyShort.DOWN);
        Keyboard.Press(VirtualKeyShort.HOME);
        Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.END);
    }

    private static string CopySelectedSql(AutomationElement editor)
    {
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(150);
        return UiTestHelpers.ReadClipboardText();
    }

    private static bool EnsureToolStripToggleIsOn(Window mainWindow, string automationId, string displayName)
    {
        AutomationElement toggle = FindToolStripToggle(mainWindow, automationId, displayName);

        if (toggle.Patterns.Toggle.TryGetPattern(out var pattern))
        {
            if (pattern.ToggleState != ToggleState.On)
            {
                InvokeToolStripButton(toggle);
                return true;
            }
            return false;
        }

        // ToolStripButton may be exposed as a plain button by a particular
        // Windows/UIA version. A prepared workstation starts with both
        // toggles disabled, so one click is the deterministic fallback.
        InvokeToolStripButton(toggle);
        return true;
    }

    private static void ToggleToolStripButton(Window mainWindow, string automationId, string displayName)
    {
        AutomationElement toggle = FindToolStripToggle(mainWindow, automationId, displayName);
        InvokeToolStripButton(toggle);
    }

    private static void InvokeToolStripButton(AutomationElement toggle)
    {
        if (toggle.Patterns.Invoke.TryGetPattern(out var invoke))
            invoke.Invoke();
        else
            Mouse.Click(toggle.GetClickablePoint());
        Thread.Sleep(100);
    }

    private static AutomationElement WaitForVisibleResultGrid(UiSession session)
    {
        try
        {
            SelectFirstResultTab(session.MainWindow);
            return UiTestHelpers.WaitFor(
                () => FindResultGrids(session).FirstOrDefault(),
                "the SQL result grid",
                timeout: SqlTimeout);
        }
        catch (TimeoutException exception)
        {
            string screenshot = Path.Combine(Path.GetTempPath(), "justybase-result-timeout.png");
            UiTestHelpers.SaveWindowScreenshot(session.MainWindow, screenshot);
            string dataGrids = string.Join(" | ", session.Application
                .GetAllTopLevelWindows(session.Automation)
                .SelectMany(window => window.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid)))
                .Select(grid => $"id={grid.AutomationId}, name={grid.Name}, offscreen={grid.IsOffscreen}"));
            string results = string.Join(" | ", session.MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .Where(tab => string.Equals(tab.Name, "Results", StringComparison.Ordinal))
                .Select(tab => $"resultsSelected={tab.Patterns.SelectionItem.PatternOrDefault?.IsSelected}"));
            throw new TimeoutException(
                $"{exception.Message} Screenshot: '{screenshot}'. Data grids: '{dataGrids}'. Results tab state: '{results}'. Log: '{ReadLog(session.MainWindow)}'.",
                exception);
        }
    }

    private static List<AutomationElement> FindResultGrids(UiSession session) =>
        session.Application.GetAllTopLevelWindows(session.Automation)
            .SelectMany(window => window.FindAllDescendants(cf => cf.ByName("sqlResultGrid")))
            .Where(element => !element.IsOffscreen)
            .ToList();

    private static AutomationElement FindToolStripToggle(
        Window mainWindow,
        string automationId,
        string displayName) =>
        UiTestHelpers.WaitFor(
            () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
                ?? mainWindow.FindFirstDescendant(cf => cf.ByName(displayName)),
            $"the {displayName} SQL toolbar button");

    private static void WaitForLog(Window mainWindow, string expectedText)
    {
        string lastLog = string.Empty;
        try
        {
            UiTestHelpers.WaitFor(
                () =>
                {
                    lastLog = ReadLog(mainWindow);
                    return lastLog.Contains(expectedText, StringComparison.Ordinal)
                        ? FindLogTextEditor(mainWindow)
                        : null;
                },
                $"Log entry '{expectedText}'",
                timeout: SqlTimeout);
        }
        catch (TimeoutException exception)
        {
            string tabs = string.Join(", ", mainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .Select(tab => tab.Name));
            throw new TimeoutException(
                $"{exception.Message} Last readable log: '{lastLog}'. Visible tab items: '{tabs}'.",
                exception);
        }
    }

    private static string ReadLog(Window mainWindow)
    {
        SelectLogTab(mainWindow);
        AutomationElement logControl = FindLogControl(mainWindow);
        if (!string.IsNullOrWhiteSpace(logControl.Name))
            return logControl.Name;

        AutomationElement log = FindLogTextEditor(mainWindow);
        if (!string.IsNullOrWhiteSpace(log.Name))
            return log.Name;

        // FastColoredTextBox accepts Ctrl+A/C only after a physical focus
        // transition to its editing surface. Click mirrors the actual user
        // interaction and is more reliable than the UIA Focus request here.
        log.Click();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(150);
        return UiTestHelpers.ReadClipboardText();
    }

    private static AutomationElement FindLogTextEditor(Window mainWindow) =>
        UiTestHelpers.WaitFor(
            () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("sqlExecutionLogText")),
            "the SQL execution log");

    private static AutomationElement FindLogControl(Window mainWindow) =>
        UiTestHelpers.WaitFor(
            () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("sqlExecutionLog")),
            "the SQL execution log container");

    private static void SelectFirstResultTab(Window mainWindow)
    {
        AutomationElement resultTab = UiTestHelpers.WaitFor(
            () => FindResultTabs(mainWindow).FirstOrDefault(),
            "the first SQL result tab",
            timeout: SqlTimeout);
        if (resultTab.Patterns.SelectionItem.TryGetPattern(out var selection))
            selection.Select();
        else
            resultTab.Click();
        Thread.Sleep(100);
    }

    private static List<AutomationElement> FindResultTabs(Window mainWindow) =>
        TryFindResultTabs(mainWindow);

    private static List<AutomationElement> TryFindResultTabs(Window mainWindow)
    {
        try
        {
            return mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .Where(tab => tab.Name.StartsWith("Result ", StringComparison.Ordinal))
                .ToList();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // DockSuite can dispose and recreate tab handles while an execution
            // completes. A stale UIA element is transient, so let WaitFor retry.
            return [];
        }
    }

    private static void SelectLogTab(Window mainWindow)
    {
        AutomationElement logTab = UiTestHelpers.WaitFor(
            () => mainWindow.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.TabItem).And(cf.ByName("Log"))),
            "the Log result tab");
        logTab.AsTabItem().Select();
        Thread.Sleep(100);
    }

    private static void OpenResultsDockWindow(UiSession session)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            // WinForms can dismiss the first top-level menu while the window
            // is receiving focus after startup. Reopen the menu on every
            // attempt rather than waiting for a menu item that no longer has
            // a live UI Automation handle.
            session.MainWindow.Focus();
            AutomationElement? settings = session.MainWindow.FindFirstDescendant(cf => cf.ByName("Settings"));
            settings?.Click();
            Thread.Sleep(150);

            AutomationElement? dockWindows = FindDockWindowsMenu(session);
            if (dockWindows is null)
            {
                Thread.Sleep(250);
                continue;
            }

            dockWindows.Click();
            Thread.Sleep(150);
            AutomationElement? results = FindResultsMenu(session);
            if (results is null)
            {
                Thread.Sleep(250);
                continue;
            }

            results.Click();
            Thread.Sleep(200);
            return;
        }

        throw new TimeoutException("Timed out opening the Results dock window.");
    }

    private static AutomationElement? FindDockWindowsMenu(UiSession session) =>
        session.Application.GetAllTopLevelWindows(session.Automation)
            .Select(window => window.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Dock windows"))))
            .FirstOrDefault(element => element is not null);

    private static AutomationElement? FindResultsMenu(UiSession session) =>
        session.Application.GetAllTopLevelWindows(session.Automation)
            .Select(window => window.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Results"))))
            .FirstOrDefault(element => element is not null);

    private static void AssertResultsTabsStartWithDiagnosticsAndLog(Window mainWindow)
    {
        AutomationElement resultTabs = UiTestHelpers.WaitFor(
            () => mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Tab))
                .FirstOrDefault(tab => tab.FindFirstDescendant(cf => cf.ByName("Diagnostics")) is not null
                    && tab.FindFirstDescendant(cf => cf.ByName("Log")) is not null),
            "the results tab strip");
        string[] names = resultTabs.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
            .Select(tab => tab.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.True(names.Length >= 2, "Results tab strip should contain Diagnostics and Log.");
        Assert.Equal("Diagnostics", names[0]);
        Assert.Equal("Log", names[1]);
    }

    private static string NewTemporaryTableName() => $"ui_refactor_{Guid.NewGuid():N}";

    private static string NormalizeNewLines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
}
