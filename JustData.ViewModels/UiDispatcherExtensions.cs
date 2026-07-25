using JustData.Application;

namespace JustData.ViewModels;

internal static class UiDispatcherExtensions
{
    public static Task InvokeOnUiAsync(
        this IUiDispatcher? dispatcher,
        Action action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, cancellationToken);
    }
}
