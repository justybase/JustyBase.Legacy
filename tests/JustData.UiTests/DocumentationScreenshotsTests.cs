using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Diagnostics;

namespace JustData.UiTests;

/// <summary>
/// Captures PNG screenshots for README documentation.
/// Requires local <c>NPS_144</c> credentials and Netezza connectivity for main-window shots.
/// </summary>
public sealed class DocumentationScreenshotsTests
{
    // No trailing semicolon — F5 with the caret after ';' can skip execution.
    internal const string DimDateResultsSql = """
        SELECT *
        FROM JUST_DATA..DIMDATE
        LIMIT 80
        """;

    /// <summary>Advanced script shown in the editor after DIMDATE results are loaded (not re-executed).</summary>
    internal const string ShowcaseSql = """
        /* Fiscal calendar bands — documentation demo */
        WITH spine AS (
            SELECT
                d.DATE_KEY,
                ROW_NUMBER() OVER (ORDER BY d.DATE_KEY DESC) AS day_rank,
                COUNT(*) OVER () AS horizon_days
            FROM JUST_DATA..DIMDATE d
            WHERE d.DATE_KEY IS NOT NULL
        ),
        bands AS (
            SELECT
                DATE_KEY,
                day_rank,
                horizon_days,
                NTILE(4) OVER (ORDER BY DATE_KEY) AS quartile
            FROM spine
            WHERE day_rank <= 120
        )
        SELECT
            quartile,
            MIN(DATE_KEY) AS period_start,
            MAX(DATE_KEY) AS period_end,
            COUNT(*) AS days_in_band
        FROM bands
        GROUP BY quartile
        ORDER BY quartile
        """;

    private static readonly string[] ScreenshotBaseNames =
    [
        "login",
        "preferences",
        "explorer",
        "editor-showcase"
    ];

    [Fact]
    [Trait("Category", "DocumentationScreenshots")]
    public void Capture_documentation_screenshots()
    {
        string outputDirectory = UiTestHelpers.GetDocumentationImagesDirectory();

        CaptureDocumentationSet(outputDirectory, darkTheme: false);
        CaptureDocumentationSet(outputDirectory, darkTheme: true);

        foreach (string baseName in ScreenshotBaseNames)
        {
            foreach (string suffix in new[] { "", "-dark" })
            {
                string path = Path.Combine(outputDirectory, $"{baseName}{suffix}.png");
                Assert.True(File.Exists(path), $"Expected screenshot was not written: {path}");
                Assert.True(new FileInfo(path).Length > 10_000, $"Screenshot looks empty: {path}");
            }
        }
    }

    private static void CaptureDocumentationSet(string outputDirectory, bool darkTheme)
    {
        string suffix = darkTheme ? "-dark" : string.Empty;
        CaptureLoginScreenshot(outputDirectory, suffix, darkTheme);
        CapturePreferencesScreenshot(outputDirectory, suffix, darkTheme);
        CaptureMainWindowScreenshots(outputDirectory, suffix, darkTheme);
    }

    private static void CaptureLoginScreenshot(string outputDirectory, string suffix, bool darkTheme)
    {
        string outputPath = Path.Combine(outputDirectory, $"login{suffix}.png");
        UiTestHelpers.KillExistingInstances();
        string arguments = $"--ui-test-login-screenshot \"{outputPath}\"";
        if (darkTheme)
        {
            arguments += " --dark";
        }

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            Arguments = arguments,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("The login screenshot process could not be started.");

        Assert.True(process.WaitForExit(60_000), "The login screenshot process did not exit.");
        Assert.Equal(0, process.ExitCode);
    }

    private static void CapturePreferencesScreenshot(string outputDirectory, string suffix, bool darkTheme)
    {
        // Real user path: login → main editor → Preferences menu → docked "JustyBase Settings" tab.
        // Do not use --ui-test-preferences-screenshot (standalone fake PreferencesForm).
        using var session = UiTestHelpers.LaunchAndLogin(useDarkTheme: darkTheme);
        UiTestHelpers.DismissBlockingDialogs(session);
        UiTestHelpers.OpenPreferences(session.MainWindow);
        UiTestHelpers.DismissBlockingDialogs(session);
        Thread.Sleep(400);
        UiTestHelpers.SaveWindowScreenshot(
            session.MainWindow,
            Path.Combine(outputDirectory, $"preferences{suffix}.png"));
    }

    private static void CaptureMainWindowScreenshots(string outputDirectory, string suffix, bool darkTheme)
    {
        UiTestHelpers.EnsureTestoweProfile();
        using var session = UiTestHelpers.LaunchAndLogin(
            useDarkTheme: darkTheme,
            navigateDocumentationDimDate: true,
            documentationShowcaseLayout: true);

        // 1) SELECT * → result grid (showcase must include the grid)
        // 2) advanced SQL in editor (not re-run)
        // 3) capture editor+results before explorer navigation changes layout focus
        // 4) expand explorer to DIMDATE for the explorer shot
        RunShowcaseQuery(session);
        UiTestHelpers.DismissBlockingDialogs(session);
        EnsureResultsGridVisible(session);
        UiTestHelpers.DismissBlockingDialogs(session);
        Thread.Sleep(600);

        session.MainWindow.Focus();
        Keyboard.Press(VirtualKeyShort.ESCAPE);
        Thread.Sleep(200);
        UiTestHelpers.DismissBlockingDialogs(session);

        UiTestHelpers.SaveWindowScreenshot(
            session.MainWindow,
            Path.Combine(outputDirectory, $"editor-showcase{suffix}.png"));

        SignalDimDateNavigation();
        WaitForDimDateInExplorer(session);
        UiTestHelpers.DismissBlockingDialogs(session);
        Thread.Sleep(400);
        UiTestHelpers.DismissBlockingDialogs(session);
        UiTestHelpers.SaveWindowScreenshot(
            session.MainWindow,
            Path.Combine(outputDirectory, $"explorer{suffix}.png"));
    }

    private static void EnsureResultsGridVisible(UiSession session)
    {
        FlaUI.Core.AutomationElements.DataGridView resultGrid = UiTestHelpers.WaitFor(
            () => FindSqlResultGrid(session.MainWindow),
            "the visible SQL result grid for showcase",
            timeout: TimeSpan.FromSeconds(30),
            condition: grid => grid.BoundingRectangle.Height >= 120);

        // Nudge layout: focus results so DockSuite does not leave them collapsed.
        try
        {
            resultGrid.Focus();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Best-effort — screenshot still proceeds if the grid is already tall enough.
        }

        Thread.Sleep(300);
        session.MainWindow.Focus();
    }

    private static void RunShowcaseQuery(UiSession session)
    {
        AutomationElement editor = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("_addedFastColored")),
            "the SQL editor");

        UiTestHelpers.SetSqlEditorText(editor, DimDateResultsSql);
        editor.Focus();
        editor.Click();
        Keyboard.Press(VirtualKeyShort.F5);

        FlaUI.Core.AutomationElements.DataGridView resultGrid = UiTestHelpers.WaitFor(
            () => FindSqlResultGrid(session.MainWindow),
            "the DIMDATE SQL result grid",
            timeout: TimeSpan.FromMinutes(3));

        UiTestHelpers.WaitFor(
            () => UiTestHelpers.GetRowCount(resultGrid) > 0 ? resultGrid : null,
            "rows in the DIMDATE result grid",
            timeout: TimeSpan.FromMinutes(3));

        Thread.Sleep(1000);

        // Keep the grid; only replace editor text. Focus first so paste hits the editor.
        editor.Focus();
        editor.Click();
        UiTestHelpers.SetSqlEditorText(editor, ShowcaseSql);
        Thread.Sleep(500);
        session.MainWindow.Focus();
    }

    private static FlaUI.Core.AutomationElements.DataGridView? FindSqlResultGrid(AutomationElement mainWindow)
    {
        try
        {
            // Prefer the real result grid; ignore the diagnostics grid.
            foreach (AutomationElement element in mainWindow.FindAllDescendants(
                         cf => cf.ByAutomationId("dataGridView1")))
            {
                FlaUI.Core.AutomationElements.DataGridView grid = element.AsDataGridView();
                if (grid.BoundingRectangle.Height > 60)
                {
                    return grid;
                }
            }

            return null;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private static void SignalDimDateNavigation()
    {
        string signalPath = Path.Combine(Path.GetTempPath(), "justybase-doc-navigate-dimdate");
        File.WriteAllText(signalPath, "1");
        Thread.Sleep(500);
    }

    private static void WaitForDimDateInExplorer(UiSession session)
    {
        var explorer = UiTestHelpers.WaitFor(
            () => session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("mvvmDatabaseExplorerControl")),
            "the database explorer");
        var tree = UiTestHelpers.WaitFor(
            () => explorer.FindFirstDescendant(cf => cf.ByAutomationId("databaseTreeView")),
            "the database tree");

        AutomationElement dimDate = UiTestHelpers.WaitFor(
            () => FindDimDateTreeNode(tree),
            "the DIMDATE table in the object tree",
            timeout: TimeSpan.FromMinutes(2));

        if (dimDate.Patterns.ScrollItem.TryGetPattern(out var scrollPattern))
        {
            scrollPattern.ScrollIntoView();
            Thread.Sleep(200);
        }

        dimDate.Click();
        Thread.Sleep(400);
        session.MainWindow.Focus();
    }

    private static AutomationElement? FindDimDateTreeNode(AutomationElement tree)
    {
        try
        {
            return tree.FindAllDescendants().FirstOrDefault(IsDimDateTreeNode);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private static bool IsDimDateTreeNode(AutomationElement element)
    {
        try
        {
            return element.Name.Equals("DIMDATE", StringComparison.OrdinalIgnoreCase)
                || element.Name.Equals("ADMIN.DIMDATE", StringComparison.OrdinalIgnoreCase)
                || element.Name.EndsWith(".DIMDATE", StringComparison.OrdinalIgnoreCase);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
}
