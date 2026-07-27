using AppBase.Common;
using AppBase.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace JustData.UiTests;

public sealed class WideResultGridPerformanceUiTests
{
    private const string ConnectionName = "NPS_144";
    private const int ExpectedColumnCount = 300;
    private static readonly string WideSelect = BuildWideSelect();
    private const string FirstRenderPipeEnvironmentVariable = "JUSTYBASE_FIRST_RENDER_PIPE";

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Performance")]
    public async Task TestoweConnection_RendersThreeHundredColumnResultWithinTwoSeconds()
    {
        EnsureTestoweProfile();
        string pipeName = $"justybase-first-render-{Guid.NewGuid():N}";
        Task<FirstRenderReport> initialRenderReport = ReceiveRenderReportAsync(pipeName);

        UiTestHelpers.KillExistingInstances();
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false,
            Environment = { [FirstRenderPipeEnvironmentVariable] = pipeName }
        });
        using UIA3Automation automation = new();
        using Process process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window loginWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window");

            WaitFor(
                () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button")
                .Invoke();

            Window mainWindow = WaitFor(
                () => UiTestHelpers.TryFindMainWindow(application, automation),
                "the main JustData window");

            AutomationElement editor = WaitFor(
                () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("_addedFastColored")),
                "the SQL editor");

            SetEditorText(editor, "SELECT 1");
            Keyboard.Press(VirtualKeyShort.F5);
            FirstRenderReport initialReport = await initialRenderReport;

            SetEditorText(editor, WideSelect);
            // Keyboard.Type posts a large sequence of Win32 input messages.
            // Give the editor time to consume that input before starting the
            // measurement; the benchmark begins with execution, not typing.
            Thread.Sleep(1_000);
            Task<FirstRenderReport> wideRenderReport = ReceiveRenderReportAsync(
                pipeName,
                TimeSpan.FromSeconds(15));
            Keyboard.Press(VirtualKeyShort.F5);
            FirstRenderReport report;
            try
            {
                report = await wideRenderReport;
            }
            catch (OperationCanceledException exception)
            {
                throw new TimeoutException(
                    $"The wide-result first-paint probe did not report. {DescribeApplicationState(application, automation, mainWindow)}",
                    exception);
            }

            Assert.NotEqual(initialReport.RunId, report.RunId);
            Assert.True(report.ColumnCount >= ExpectedColumnCount,
                $"The first rendered grid had {report.ColumnCount} columns; expected at least {ExpectedColumnCount}.");
            Assert.True(report.ElapsedMilliseconds <= 2_000,
                $"Rendering {ExpectedColumnCount} result columns took {report.ElapsedMilliseconds} ms; the limit is 2000 ms.");
            AutomationElement wideResult = WaitFor(
                () => FindVisibleResultGridWithAtLeastColumns(mainWindow, ExpectedColumnCount),
                    $"the {ExpectedColumnCount}-column result grid",
                    timeout: TimeSpan.FromSeconds(10));
            Assert.NotNull(wideResult);
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

    private static string BuildWideSelect() =>
        "SELECT " + string.Join(',', Enumerable.Range(1, ExpectedColumnCount)
            .Select(number => ((number - 1) % 30 + 1).ToString()));

    private static void EnsureTestoweProfile()
    {
        string credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JustyBaseLegacy",
            "credentials.json.enc");

        Assert.True(File.Exists(credentialsPath), $"The real local credentials file was not found: {credentialsPath}");
        CredentialStoreReadResult credentials = new CredentialStore().Read(credentialsPath);
        List<LoginData> profiles = JsonSerializer.Deserialize<List<LoginData>>(credentials.Content) ?? [];
        Assert.NotEmpty(profiles);
        int defaultIndex = Math.Clamp(profiles[0].DefaultIndex, 0, profiles.Count - 1);
        Assert.Equal(ConnectionName, profiles[defaultIndex].Name);
    }

    private static AutomationElement? FindVisibleResultGrid(Window mainWindow) =>
        mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid))
            .FirstOrDefault(grid => grid.AutomationId == "dataGridView1" && !grid.IsOffscreen);

    private static AutomationElement? FindVisibleResultGridWithAtLeastColumns(Window mainWindow, int columnCount) =>
        mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid))
            .FirstOrDefault(grid => grid.AutomationId == "dataGridView1"
                && !grid.IsOffscreen
                && HasAtLeastColumns(grid, columnCount));

    private static bool HasAtLeastColumns(AutomationElement grid, int columnCount) =>
        grid.Patterns.Grid.TryGetPattern(out var gridPattern)
            ? gridPattern.ColumnCount >= columnCount
            : grid.FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem)).Length >= columnCount;

    private static void SetEditorText(AutomationElement editor, string sql)
    {
        editor.Click();
        Thread.Sleep(250);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(sql);
    }

    private static string DescribeApplicationState(
        FlaUI.Core.Application application,
        UIA3Automation automation,
        Window mainWindow)
    {
        string windows = string.Join(", ", application.GetAllTopLevelWindows(automation)
            .Select(window => $"'{window.Title}'"));
        string grids = string.Join(", ", mainWindow
            .FindAllDescendants(cf => cf.ByAutomationId("dataGridView1"))
            .Select(grid =>
            {
                int columns = grid.Patterns.Grid.TryGetPattern(out var pattern)
                    ? pattern.ColumnCount
                    : -1;
                return $"columns={columns}, offscreen={grid.IsOffscreen}";
            }));
        return $"Top-level windows: [{windows}]. Result grids: [{grids}].";
    }

    private static async Task<FirstRenderReport> ReceiveRenderReportAsync(
        string pipeName,
        TimeSpan? timeout = null)
    {
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(1));
        await using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipe.WaitForConnectionAsync(cancellation.Token);
        using var reader = new StreamReader(pipe);
        string? message = await reader.ReadLineAsync(cancellation.Token);
        Assert.False(string.IsNullOrWhiteSpace(message), "The first-render probe sent an empty message.");
        return JsonSerializer.Deserialize<FirstRenderReport>(message!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })
            ?? throw new InvalidOperationException("The first-render probe sent invalid JSON.");
    }

    private sealed record FirstRenderReport(string RunId, int ColumnCount, long ElapsedMilliseconds);

    private static T WaitFor<T>(
        Func<T?> read,
        string description,
        Func<T?, bool>? condition = null,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
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

            Thread.Sleep(pollInterval ?? TimeSpan.FromMilliseconds(250));
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
