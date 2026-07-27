using JustData.Application.Schema;
using JustData.ViewModels.Explorer;

namespace JustData.ViewModels.Tests;

/// <summary>
/// Regression: rapid schema refresh used to surface TaskCanceledException from
/// async WinForms click handlers and close the application.
/// </summary>
public sealed class DatabaseExplorerRefreshStabilityRegressionTests
{
    [Fact]
    public async Task Rapid_refresh_cancellation_does_not_throw_to_caller()
    {
        var repository = new FakeSchemaRepository { BlockRefresh = true };
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());

        Task first = vm.RefreshAsync();
        while (!vm.IsBusy)
            await Task.Yield();

        // Second refresh cancels the first. Neither call may throw TaskCanceledException
        // out to an async void / click handler.
        Task second = vm.RefreshAsync();
        repository.UnblockRefresh();
        await Task.WhenAll(first, second);

        Assert.False(vm.IsBusy);
        Assert.DoesNotContain("Collection was modified", vm.Status ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancelled_refresh_reports_cancelled_without_faulting()
    {
        var repository = new FakeSchemaRepository { BlockRefresh = true };
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());

        Task refresh = vm.RefreshAsync();
        while (!vm.IsBusy)
            await Task.Yield();

        Exception? fault = null;
        try
        {
            vm.Cancel();
            await refresh;
        }
        catch (Exception ex)
        {
            fault = ex;
        }

        Assert.Null(fault);
        Assert.Equal("Cancelled", vm.Status);
        Assert.False(vm.IsBusy);
    }

    private sealed class FakeDdlService : ISchemaDdlService
    {
        public Task<string> GetDdlAsync(SchemaDdlRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult("CREATE TABLE t");
    }

    private sealed class FakeSchemaRepository : ISchemaRepository
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool BlockRefresh { get; init; }

        public void UnblockRefresh() => _gate.TrySetResult();

        public async Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default, SchemaRefreshRequest? request = null)
        {
            if (BlockRefresh)
                await _gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task<bool> AttachDatabaseAsync(string connectionName, string databaseName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(string? connectionName = null, CancellationToken cancellationToken = default)
        {
            string name = string.IsNullOrWhiteSpace(connectionName) ? "local" : connectionName;
            return Task.FromResult<IReadOnlyList<SchemaNode>>(
                [new(name, name, SchemaNodeKind.Connection, new(name), true)]);
        }

        public Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(SchemaNode parent, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaNode>>([]);

        public Task<SchemaSearchResult> SearchAsync(SchemaSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchemaSearchResult([]));

        public Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaReference>>([]);
    }
}
