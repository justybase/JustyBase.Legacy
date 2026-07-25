namespace JustData.Application.Startup;

/// <summary>Pure process-start decisions kept separate from WinForms and mutex APIs.</summary>
public static class StartupArguments
{
    public static bool IsSmokeTest(IReadOnlyList<string> args) =>
        args.Count == 1 && args[0].Equals("--smoke-test", StringComparison.OrdinalIgnoreCase);

    public static bool IsPreferencesUiTest(IReadOnlyList<string> args) =>
        args.Count == 2 && args[0].Equals("--ui-test-preferences", StringComparison.OrdinalIgnoreCase);

    public static bool IsLoginScreenshotUiTest(IReadOnlyList<string> args) =>
        args.Count >= 2
        && args[0].Equals("--ui-test-login-screenshot", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(args[1]);

    public static bool IsDocumentationShowcaseLayout(IReadOnlyList<string> args) =>
        args.Any(argument => argument.Equals("--ui-test-showcase-layout", StringComparison.OrdinalIgnoreCase));

    public static bool IsDocumentationNavigateDimDate(IReadOnlyList<string> args) =>
        args.Any(argument => argument.Equals("--ui-test-navigate-dimdate", StringComparison.OrdinalIgnoreCase));

    public static bool IsDocumentationDarkTheme(IReadOnlyList<string> args) =>
        args.Any(argument => argument.Equals("--dark", StringComparison.OrdinalIgnoreCase));

    public static bool ShouldForwardToExistingInstance(bool ownsMutex, IReadOnlyList<string> args) =>
        !ownsMutex && args.Count >= 1 && !IsSilentOrScript(args[0]);

    public static bool ShouldShowAlreadyRunning(bool ownsMutex, IReadOnlyList<string> args) =>
        !ownsMutex && args.Count == 0;

    public static bool ShouldRunLogin(IReadOnlyList<string> args)
    {
        if (IsDocumentationOnlyLaunch(args))
        {
            return true;
        }

        return args.Count != 2;
    }

    private static bool IsDocumentationOnlyLaunch(IReadOnlyList<string> args) =>
        args.Count > 0 && args.All(IsDocumentationLaunchModifier);

    private static bool IsDocumentationLaunchModifier(string argument) =>
        argument.Equals("--dark", StringComparison.OrdinalIgnoreCase)
        || argument.Equals("--ui-test-navigate-dimdate", StringComparison.OrdinalIgnoreCase)
        || argument.Equals("--ui-test-showcase-layout", StringComparison.OrdinalIgnoreCase);

    public static bool ShouldRestoreStartupFiles(bool enabled, bool encryptedFileExists, bool plainFileExists) =>
        enabled && (encryptedFileExists || plainFileExists);

    private static bool IsSilentOrScript(string argument) =>
        argument.Equals("silent", StringComparison.OrdinalIgnoreCase)
        || argument.Equals("script", StringComparison.OrdinalIgnoreCase);
}
