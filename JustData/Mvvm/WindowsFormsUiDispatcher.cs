using System.ComponentModel;
using JustData.Application;

namespace JustData.Mvvm;

internal sealed class WindowsFormsUiDispatcher : IUiDispatcher
{
    private ISynchronizeInvoke? _synchronizer;

    public WindowsFormsUiDispatcher()
    {
    }

    public WindowsFormsUiDispatcher(ISynchronizeInvoke synchronizer)
    {
        Attach(synchronizer);
    }

    /// <summary>
    /// The dispatcher is scoped with a main window.  The window is attached
    /// after its dependencies have been constructed, which avoids a service
    /// locator or a circular DI dependency on <see cref="BaseWindow"/>.
    /// </summary>
    public void Attach(ISynchronizeInvoke synchronizer)
    {
        ArgumentNullException.ThrowIfNull(synchronizer);
        Interlocked.CompareExchange(ref _synchronizer, synchronizer, null);
    }

    public bool CheckAccess() => _synchronizer is not { InvokeRequired: true };

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        var synchronizer = _synchronizer;
        if (synchronizer is null || CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            synchronizer.BeginInvoke(new Action(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    action();
                    completion.TrySetResult();
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }), null);
        }
        catch (Exception exception) when (
            exception is InvalidAsynchronousStateException
            || exception is ObjectDisposedException
            || exception is InvalidOperationException)
        {
            // A form can be disposed between CheckAccess and BeginInvoke.
            // Complete the task instead of leaving a ViewModel operation
            // awaiting forever during application shutdown.
            completion.TrySetException(exception);
        }

        return completion.Task;
    }
}
