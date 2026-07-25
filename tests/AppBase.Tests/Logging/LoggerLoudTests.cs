using AppBase.Services;

namespace AppBase.Tests.Logging;

public sealed class LoggerLoudTests
{
    [Fact]
    public void Schema_problem_prompt_can_be_shown_before_a_window_is_assigned()
    {
        var logger = new RecordingLoggerLoud("&Yes");

        bool shouldRestart = logger.OnSchemaProblemMessage("Production");

        Assert.True(shouldRestart);
        Assert.Null(logger.Owner);
    }

    private sealed class RecordingLoggerLoud(string response) : LoggerLoud
    {
        public IWin32Window? Owner { get; private set; }

        protected override TaskDialogButton ShowSchemaProblemDialog(IWin32Window? owner, TaskDialogPage page)
        {
            Owner = owner;
            return new TaskDialogButton(response);
        }
    }
}
