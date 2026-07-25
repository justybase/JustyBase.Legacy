namespace JustData.Application.Schema;

/// <summary>
/// Controls yielding between large schema-tree child batches.
/// The abstraction keeps production batching responsive and makes ViewModel
/// tests deterministic without relying on wall-clock timing.
/// </summary>
public interface IExplorerBatchScheduler
{
    Task DelayAsync(CancellationToken cancellationToken = default);
}
