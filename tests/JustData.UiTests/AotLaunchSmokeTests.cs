using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using System.Diagnostics;

namespace JustData.UiTests;

/// <summary>
/// Smoke tests that launch the AOT-published (native) executable to verify
/// it starts, renders the login window, and responds to basic interaction.
/// These tests require an AOT publish to have been run first:
/// <code>
/// dotnet publish JustData/JustData.csproj -p:UseAOT=true -p:SelfContained=true -c Release
/// </code>
/// </summary>
public sealed class AotLaunchSmokeTests
{
    /// <summary>
    /// Relative path from the test assembly output directory up to the repo root.
    /// Test output: tests/JustData.UiTests/bin/Debug/net10.0-windows10.0.22000.0/
    /// Repo root:   (go up 6 levels)
    /// </summary>
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    /// <summary>
    /// Expected path to the AOT-published executable.
    /// </summary>
    private static readonly string AotExePath = Path.Combine(
        RepoRoot,
        "JustData",
        "bin",
        "Release",
        "net10.0-windows10.0.22000.0",
        "win-x64",
        "publish",
        "JustyBaseLegacy.exe");

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_published_exe_launches_login_window()
    {
        // Arrange: skip if AOT exe not found (publish hasn't been run yet)
        AssertSkipIfAotExeMissing();

        // Act
        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = AotExePath,
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);

        try
        {
            // Assert: login window appears within timeout
            Window login = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window from the AOT-compiled exe");

            // Verify key AutomationIds are present in the login window
            Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId("userNameTextBox")));
            Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId("passwordTextBox")));
            Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton")));
            Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId("saveBt")));

            // Note: The login window does not respond to Escape or Alt+F4 in the AOT build.
            // The process is cleaned up in the finally block below.
            // If this test times out, the login window is likely blocking.
            // In that case, the finally block will kill it.
            // This is not a regression — the same behavior occurs in the non-AOT build.
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); }
                catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    [Fact]
    [Trait("Category", "AOT")]
    public void Aot_published_exe_shows_all_login_automation_ids()
    {
        AssertSkipIfAotExeMissing();

        UiTestHelpers.KillExistingInstances();
        using var application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = AotExePath,
            UseShellExecute = false
        });
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window login = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window from the AOT-compiled exe");

            // All stable AutomationIds that LoginUiTests checks
            foreach (var id in new[]
            {
                "userNameTextBox", "passwordTextBox", "serverTextBox", "connectionSelectorComboBox",
                "selectDatabaseButton", "saveBt", "addNewButton", "deleteButton", "nameTextBox",
                "checkBoxFastLogin", "xButton", "btReorder", "checkBox1", "databaseComboBox",
                "DriverComboBox", "rememberAsDefaultCheckBox"
            })
            {
                Assert.NotNull(login.FindFirstDescendant(cf => cf.ByAutomationId(id)));
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                try { application.Kill(); }
                catch (InvalidOperationException) { }
                process.WaitForExit(10_000);
            }
        }
    }

    private static void AssertSkipIfAotExeMissing()
    {
        if (!File.Exists(AotExePath))
        {
            Assert.Fail($"AOT-published executable not found at: {AotExePath}{Environment.NewLine}" +
                        "Run the AOT publish first:" +
                        "  dotnet publish JustData/JustData.csproj -p:UseAOT=true -p:SelfContained=true -c Release");
        }
    }

    private static T WaitFor<T>(Func<T?> read, string description) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var value = read();
            if (value is not null) return value;
            Thread.Sleep(200);
        }
        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
