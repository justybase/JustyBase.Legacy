using System;
using System.Threading.Tasks;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    /// <summary>
    /// Converts a WinForms event callback into a task boundary with one
    /// consistent exception policy. Event handlers cannot return Task, so an
    /// unobserved exception from an async void handler would otherwise be
    /// raised on the Windows Forms synchronization context.
    /// </summary>
    private async Task RunUiEventAsync(string operationName, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected when the window is closing or a newer
            // operation supersedes the current one.
        }
        catch (Exception exception)
        {
            _loggerLoud.LogError($"{operationName} failed", exception);
        }
    }
}
