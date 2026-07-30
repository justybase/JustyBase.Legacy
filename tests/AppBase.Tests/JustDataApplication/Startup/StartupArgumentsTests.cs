using JustData.Application.Startup;

namespace AppBase.Tests.JustDataApplication.Startup;

public sealed class StartupArgumentsTests
{
    // ── IsSmokeTest ──

    [Fact]
    public void IsSmokeTest_single_arg_matching_returns_true()
    {
        Assert.True(StartupArguments.IsSmokeTest(["--smoke-test"]));
    }

    [Fact]
    public void IsSmokeTest_single_arg_case_insensitive()
    {
        Assert.True(StartupArguments.IsSmokeTest(["--SMOKE-TEST"]));
    }

    [Fact]
    public void IsSmokeTest_wrong_arg_returns_false()
    {
        Assert.False(StartupArguments.IsSmokeTest(["--other"]));
    }

    [Fact]
    public void IsSmokeTest_multiple_args_returns_false()
    {
        Assert.False(StartupArguments.IsSmokeTest(["--smoke-test", "extra"]));
    }

    [Fact]
    public void IsSmokeTest_no_args_returns_false()
    {
        Assert.False(StartupArguments.IsSmokeTest([]));
    }

    [Fact]
    public void IsLoginScreenshotUiTest_correct_args_returns_true()
    {
        Assert.True(StartupArguments.IsLoginScreenshotUiTest(["--ui-test-login-screenshot", "out.png"]));
    }

    [Fact]
    public void IsLoginScreenshotUiTest_wrong_arg_returns_false()
    {
        Assert.False(StartupArguments.IsLoginScreenshotUiTest(["--ui-test-login-screenshot"]));
        Assert.False(StartupArguments.IsLoginScreenshotUiTest(["--ui-test-preferences", "x"]));
    }

    [Fact]
    public void IsDocumentationDarkTheme_when_dark_switch_present_returns_true()
    {
        Assert.True(StartupArguments.IsDocumentationDarkTheme(["--dark"]));
        Assert.True(StartupArguments.IsDocumentationDarkTheme(["--ui-test-login-screenshot", "out.png", "--dark"]));
    }

    [Fact]
    public void IsDocumentationDarkTheme_without_switch_returns_false()
    {
        Assert.False(StartupArguments.IsDocumentationDarkTheme([]));
        Assert.False(StartupArguments.IsDocumentationDarkTheme(["--ui-test-login-screenshot", "out.png"]));
    }

    [Fact]
    public void IsDocumentationShowcaseLayout_when_switch_present_returns_true()
    {
        Assert.True(StartupArguments.IsDocumentationShowcaseLayout(["--ui-test-showcase-layout"]));
    }

    [Fact]
    public void IsDocumentationNavigateDimDate_when_switch_present_returns_true()
    {
        Assert.True(StartupArguments.IsDocumentationNavigateDimDate(["--ui-test-navigate-dimdate"]));
        Assert.True(StartupArguments.IsDocumentationNavigateDimDate(["--dark", "--ui-test-navigate-dimdate"]));
    }

    [Fact]
    public void IsDocumentationNavigateDimDate_without_switch_returns_false()
    {
        Assert.False(StartupArguments.IsDocumentationNavigateDimDate([]));
        Assert.False(StartupArguments.IsDocumentationNavigateDimDate(["--dark"]));
    }

    [Fact]
    public void TryGetUiTestOpenFile_when_switch_present_returns_path()
    {
        Assert.True(
            StartupArguments.TryGetUiTestOpenFile(["--ui-test-open-file=C:\\temp\\big.sql"], out string path));
        Assert.Equal(@"C:\temp\big.sql", path);
    }

    [Fact]
    public void TryGetUiTestOpenFile_without_switch_returns_false()
    {
        Assert.False(StartupArguments.TryGetUiTestOpenFile(["--dark"], out string path));
        Assert.Equal(string.Empty, path);
    }

    // ── IsPreferencesUiTest ──

    [Fact]
    public void IsPreferencesUiTest_correct_args_returns_true()
    {
        Assert.True(StartupArguments.IsPreferencesUiTest(["--ui-test-preferences", "some-file"]));
    }

    [Fact]
    public void IsPreferencesUiTest_wrong_count_returns_false()
    {
        Assert.False(StartupArguments.IsPreferencesUiTest(["--ui-test-preferences"]));
        Assert.False(StartupArguments.IsPreferencesUiTest(["--ui-test-preferences", "a", "b"]));
    }

    // ── ShouldForwardToExistingInstance ──

    [Fact]
    public void ShouldForward_owns_mutex_returns_false()
    {
        Assert.False(StartupArguments.ShouldForwardToExistingInstance(true, ["file.sql"]));
    }

    [Fact]
    public void ShouldForward_not_own_mutex_with_file_returns_true()
    {
        Assert.True(StartupArguments.ShouldForwardToExistingInstance(false, ["file.sql"]));
    }

    [Fact]
    public void ShouldForward_not_own_mutex_no_args_returns_false()
    {
        Assert.False(StartupArguments.ShouldForwardToExistingInstance(false, []));
    }

    [Fact]
    public void ShouldForward_silent_arg_returns_false()
    {
        Assert.False(StartupArguments.ShouldForwardToExistingInstance(false, ["silent"]));
        Assert.False(StartupArguments.ShouldForwardToExistingInstance(false, ["script"]));
    }

    // ── ShouldShowAlreadyRunning ──

    [Fact]
    public void ShouldShow_not_own_mutex_no_args_returns_true()
    {
        Assert.True(StartupArguments.ShouldShowAlreadyRunning(false, []));
    }

    [Fact]
    public void ShouldShow_not_own_mutex_with_args_returns_false()
    {
        Assert.False(StartupArguments.ShouldShowAlreadyRunning(false, ["file.sql"]));
    }

    [Fact]
    public void ShouldShow_own_mutex_no_args_returns_false()
    {
        Assert.False(StartupArguments.ShouldShowAlreadyRunning(true, []));
    }

    // ── ShouldRunLogin ──

    [Fact]
    public void ShouldRunLogin_with_documentation_modifiers_only_returns_true()
    {
        Assert.True(StartupArguments.ShouldRunLogin(["--dark"]));
        Assert.True(StartupArguments.ShouldRunLogin(["--ui-test-navigate-dimdate"]));
        Assert.True(StartupArguments.ShouldRunLogin(["--dark", "--ui-test-navigate-dimdate"]));
        Assert.True(StartupArguments.ShouldRunLogin(["--dark", "--ui-test-navigate-dimdate", "--ui-test-showcase-layout"]));
    }

    [Fact]
    public void ShouldRunLogin_with_two_args_returns_false()
    {
        Assert.False(StartupArguments.ShouldRunLogin(["a", "b"]));
    }

    [Fact]
    public void ShouldRunLogin_with_zero_args_returns_true()
    {
        Assert.True(StartupArguments.ShouldRunLogin([]));
    }

    [Fact]
    public void ShouldRunLogin_with_one_arg_returns_true()
    {
        Assert.True(StartupArguments.ShouldRunLogin(["file.sql"]));
    }

    // ── ShouldRestoreStartupFiles ──

    [Fact]
    public void ShouldRestore_enabled_with_file_returns_true()
    {
        Assert.True(StartupArguments.ShouldRestoreStartupFiles(true, true, false));
        Assert.True(StartupArguments.ShouldRestoreStartupFiles(true, false, true));
        Assert.True(StartupArguments.ShouldRestoreStartupFiles(true, true, true));
    }

    [Fact]
    public void ShouldRestore_disabled_returns_false()
    {
        Assert.False(StartupArguments.ShouldRestoreStartupFiles(false, true, false));
        Assert.False(StartupArguments.ShouldRestoreStartupFiles(false, false, true));
    }

    [Fact]
    public void ShouldRestore_no_files_returns_false()
    {
        Assert.False(StartupArguments.ShouldRestoreStartupFiles(true, false, false));
    }
}
