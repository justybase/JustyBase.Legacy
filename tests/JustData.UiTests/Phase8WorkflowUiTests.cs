using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Runtime.ExceptionServices;

namespace JustData.UiTests;

public sealed class Phase8WorkflowUiTests : IDisposable
{
    private readonly string _outputDirectory = Path.Combine(
        Path.GetTempPath(),
        "JustyBaseLegacy.Phase8Ui",
        Guid.NewGuid().ToString("N"));

    [Fact]
    [Trait("Category", "UI")]
    public void Lint_diagnostics_support_filter_search_and_editor_navigation()
    {
        UiTestHelpers.EnsureTestoweProfile();
        using var session = UiTestHelpers.LaunchAndLogin();

        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("_addedFastColored")),
            "the SQL editor");
        SetEditorText(editor, "SELECT * FROM TEST_TABLE");
        Assert.Equal("SELECT * FROM TEST_TABLE", ReadEditorText(editor));

        FlaUI.Core.AutomationElements.DataGridView diagnostics;
        try
        {
            diagnostics = UiTestHelpers.WaitFor(
                () => session.MainWindow.FindFirstDescendant(
                        cf => cf.ByAutomationId("diagnosticsGrid"))?.AsDataGridView(),
                "the diagnostics grid",
                grid => !grid.IsOffscreen && UiTestHelpers.GetRowCount(grid) > 0,
                TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException(
                $"{exception.Message} {DescribeDataGrids(session.MainWindow)}",
                exception);
        }

        FlaUI.Core.AutomationElements.TextBox search = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                    cf => cf.ByAutomationId("diagnosticsSearchBox"))?.AsTextBox(),
            "the diagnostics search box");
        search.Text = "NZ001";
        UiTestHelpers.WaitFor(
            () => UiTestHelpers.GetRowCount(diagnostics) == 1 ? diagnostics : null,
            "the NZ001 filtered diagnostic");

        FlaUI.Core.AutomationElements.ComboBox severity = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                    cf => cf.ByAutomationId("diagnosticsSeverityFilter"))?.AsComboBox(),
            "the diagnostics severity filter");
        severity.Focus();
        Keyboard.Press(VirtualKeyShort.END);
        UiTestHelpers.WaitFor(
            () => FindDiagnosticsGrid(session.MainWindow, expectedRows: 0),
            "the empty Hint severity result");
        severity.Focus();
        Keyboard.Press(VirtualKeyShort.HOME);
        diagnostics = UiTestHelpers.WaitFor(
            () => FindDiagnosticsGrid(session.MainWindow, expectedRows: 1),
            "the restored NZ001 diagnostic");

        AutomationElement row = UiTestHelpers.WaitFor(
            () => diagnostics.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataItem)),
            "the NZ001 diagnostic row");
        row.DoubleClick();
        UiTestHelpers.WaitFor(
            () => editor.Properties.HasKeyboardFocus.Value ? editor : null,
            "focus returned to the SQL editor after diagnostic navigation");
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Query_export_streams_csv_and_xlsx_and_preserves_import_entry_point()
    {
        UiTestHelpers.EnsureTestoweProfile();
        Directory.CreateDirectory(_outputDirectory);
        using var session = UiTestHelpers.LaunchAndLogin();

        Assert.NotNull(session.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("ImportToolStripMenuItem").Or(cf.ByName("Import from"))));

        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("_addedFastColored")),
            "the SQL editor");
        string csvPath = Path.Combine(_outputDirectory, "phase8.csv");
        string csvCommand = $"___expCsv: SELECT 1 AS EXPORT_VALUE -> {csvPath};";
        SetEditorText(editor, csvCommand);
        Thread.Sleep(1_000);
        Assert.Equal(csvCommand, ReadEditorText(editor));
        Keyboard.Press(VirtualKeyShort.F5);
        WaitForExport(csvPath);
        string csv = File.ReadAllText(csvPath);
        Assert.Contains("EXPORT_VALUE", csv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains('1', csv);

        string xlsxPath = Path.Combine(_outputDirectory, "phase8.xlsx");
        string xlsxCommand = $"___expXlsx: SELECT 1 AS EXPORT_VALUE -> {xlsxPath};";
        SetEditorText(editor, xlsxCommand);
        Thread.Sleep(1_000);
        Assert.Equal(xlsxCommand, ReadEditorText(editor));
        Keyboard.Press(VirtualKeyShort.F5);
        WaitForExport(xlsxPath);
        Assert.True(new FileInfo(xlsxPath).Length > 100, "The XLSX export was unexpectedly empty.");
    }

    private static void WaitForExport(string outputPath)
    {
        UiTestHelpers.WaitFor(
            () => File.Exists(outputPath) && new FileInfo(outputPath).Length > 0
                ? new FileInfo(outputPath)
                : null,
            $"the exported file {Path.GetFileName(outputPath)}",
            timeout: TimeSpan.FromMinutes(2));
    }

    private static FlaUI.Core.AutomationElements.DataGridView? FindDiagnosticsGrid(
        Window mainWindow,
        int expectedRows)
    {
        var grid = mainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("diagnosticsGrid"))?.AsDataGridView();
        return grid is not null && UiTestHelpers.GetRowCount(grid) == expectedRows ? grid : null;
    }

    private static string DescribeDataGrids(Window mainWindow) =>
        "Data grids: [" + string.Join(", ", mainWindow
            .FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid))
            .Select(element =>
            {
                var grid = element.AsDataGridView();
                return $"id='{element.AutomationId}', name='{element.Name}', offscreen={element.IsOffscreen}, rows={UiTestHelpers.GetRowCount(grid)}";
            })) + "].";

    private static void SetEditorText(AutomationElement editor, string sql)
    {
        editor.Click();
        Thread.Sleep(250);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(sql);
    }

    private static string ReadEditorText(AutomationElement editor)
    {
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(250);
        string? result = null;
        Exception? exception = null;
        Thread thread = new(() =>
        {
            try { result = System.Windows.Forms.Clipboard.GetText(); }
            catch (Exception caught) { exception = caught; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "The clipboard read timed out.");
        if (exception is not null)
            ExceptionDispatchInfo.Capture(exception).Throw();
        return result ?? string.Empty;
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, recursive: true);
    }
}
