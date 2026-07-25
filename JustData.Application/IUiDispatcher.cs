namespace JustData.Application;

/// <summary>Schedules application work on the presentation thread.</summary>
public interface IUiDispatcher
{
    bool CheckAccess();

    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);
}
