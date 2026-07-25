using AppBase.Common;
using AppBase.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Windows.Forms;

namespace JustData.UiTests;

public sealed class SqlAutocompleteUiTests
{
    private const string ConnectionName = "test_nz_connection";
    private const string TablePrefix = "SELECT * FROM JUST_DATA..DIMD";
    private const string TableSql = "JUST_DATA..DIMDATE";

    [Theory]
    [InlineData("SELECT")]
    [InlineData("WHERE")]
    [Trait("Category", "UI")]
    public void TestoweConnection_CompletesDimDateAliasedColumnInSelectAndWhere(string scenario)
    {
        UiTestHelpers.EnsureTestoweProfile();

        UiTestHelpers.KillExistingInstances();
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
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
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null),
                "the main JustData window");
            AutomationElement editor = WaitFor(
                () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")),
                "the SQL editor");

            // Wait for schema download to complete — status bar shows
            // "Schema downloaded" after all column data is available.
            WaitFor(
                () =>
                {
                    try
                    {
                        var status = mainWindow.FindFirstDescendant(
                            cf => cf.ByAutomationId("statusTextBox"));
                        // Use AsTextBox() wrapper which reads ValuePattern correctly.
                        var text = status?.AsTextBox()?.Text;
                        return text?.Contains("Schema downloaded") == true ? status : null;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        return null;
                    }
                },
                "schema downloaded (status bar)",
                TimeSpan.FromSeconds(90));

            SetEditorText(editor, TablePrefix);
            AcceptAutocompleteSuggestion();
            string completedTableSql = CopyEditorText(editor);
            Assert.Contains(TableSql, completedTableSql, StringComparison.OrdinalIgnoreCase);

            string expectedPattern;
            if (scenario == "SELECT")
            {
                SetEditorText(editor, $"SELECT  FROM {TableSql} D");
                MoveCaretTo("SELECT ".Length);
                Keyboard.Type("D.");
                expectedPattern = @"(?is)^\s*SELECT\s+D\.[\w""\[\]]+\s+FROM\s+JUST_DATA\.\.DIMDATE\s+D\s*$";
            }
            else
            {
                SetEditorText(editor, $"SELECT * FROM {TableSql} D WHERE D.");
                expectedPattern = @"(?is)^\s*SELECT\s+\*\s+FROM\s+JUST_DATA\.\.DIMDATE\s+D\s+WHERE\s+D\.[\w""\[\]]+\s*$";
            }

            Keyboard.Press(VirtualKeyShort.ESCAPE);
            AcceptAutocompleteSuggestion();
            string completedColumnSql = CopyEditorText(editor);
            Assert.Matches(expectedPattern, completedColumnSql);
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

    private static void AcceptAutocompleteSuggestion()
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SPACE);
        // Give the autocomplete popup time to render. 1500ms covers
        // schema-heavy lookups without being fragile — Enter fires
        // after the popup is visible.
        Thread.Sleep(1500);
        Keyboard.Press(VirtualKeyShort.ENTER);
        Thread.Sleep(500);
    }

    private static void SetEditorText(AutomationElement editor, string sql)
    {
        Keyboard.Press(VirtualKeyShort.ESCAPE);
        editor.Click();
        Thread.Sleep(250);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(sql);
        Thread.Sleep(100);
        Keyboard.Press(VirtualKeyShort.ESCAPE);
    }

    private static void MoveCaretTo(int characterOffset)
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.HOME);
        Thread.Sleep(100);
        for (int index = 0; index < characterOffset; index++)
        {
            Keyboard.Press(VirtualKeyShort.RIGHT);
            Thread.Sleep(50);
        }
    }

    private static string CopyEditorText(AutomationElement editor)
    {
        editor.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(250);
        return RunInSta(() =>
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (Clipboard.ContainsText())
                {
                    return Clipboard.GetText();
                }

                Thread.Sleep(50);
            }

            throw new TimeoutException("Timed out waiting for the editor text in the clipboard.");
        });
    }

    private static T RunInSta<T>(Func<T> operation)
    {
        T? result = default;
        Exception? exception = null;
        Thread thread = new(() =>
        {
            try
            {
                result = operation();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "The STA clipboard operation timed out.");
        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        return result!;
    }

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

    private static T WaitFor<T>(Func<T?> read, string description, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        T? value;
        do
        {
            value = read();
            if (value is not null)
            {
                return value;
            }

            Thread.Sleep(250);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
