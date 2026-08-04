using JustData.Application.Schema;
using JustData.ViewModels.Explorer;

namespace JustData.ViewModels.Tests;

public sealed class ExplorerViewModelTests
{
    [Fact]
    public async Task Database_explorer_loads_roots_lazily_expands_searches_and_generates_ddl()
    {
        var repository = new FakeSchemaRepository();
        var ddl = new FakeDdlService();
        using var vm = new DatabaseExplorerViewModel(repository, ddl);

        await vm.InitializeAsync("local");

        Assert.Single(vm.RootNodes);
        Assert.Equal("local", vm.ConnectionName);
        Assert.False(vm.RootNodes[0].ChildrenLoaded);

        await vm.ExpandCommand.ExecuteAsync(vm.RootNodes[0]);
        Assert.True(vm.RootNodes[0].ChildrenLoaded);
        Assert.Equal("orders", vm.RootNodes[0].Children[0].Name);

        vm.Filter = "orders";
        await vm.SearchCommand.ExecuteAsync(null);
        Assert.Single(vm.RootNodes);
        Assert.Single(vm.SearchResults);
        await vm.DdlCommand.ExecuteAsync(vm.SearchResults[0]);
        Assert.Equal("CREATE TABLE orders", vm.LastDdl);
        Assert.Equal(SchemaDdlKind.Create, ddl.LastRequest!.Kind);
    }

    [Fact]
    public async Task Search_results_do_not_replace_the_expandable_schema_tree()
    {
        using var vm = new DatabaseExplorerViewModel(new FakeSchemaRepository(), new FakeDdlService());
        await vm.InitializeAsync("local", refresh: false);
        ExplorerNodeViewModel root = Assert.Single(vm.RootNodes);

        vm.Filter = "orders";
        await vm.SearchAsync();

        Assert.Same(root, Assert.Single(vm.RootNodes));
        Assert.Equal("orders", Assert.Single(vm.SearchResults).Name);

        vm.Filter = string.Empty;
        await vm.SearchAsync();
        Assert.Empty(vm.SearchResults);
        Assert.Same(root, Assert.Single(vm.RootNodes));
    }

    [Fact]
    public async Task Refresh_cancellation_reports_cancelled_and_dispose_cancels_future_work()
    {
        var repository = new FakeSchemaRepository { BlockRefresh = true };
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());

        Task refresh = vm.RefreshAsync();
        while (!vm.IsBusy) await Task.Yield();
        vm.CancelCommand.Execute(null);
        await refresh;
        Assert.Equal("Cancelled", vm.Status);

        vm.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => vm.RefreshAsync());
    }

    [Fact]
    public async Task Ddl_operation_is_busy_and_cancellable()
    {
        var ddl = new BlockingDdlService();
        using var vm = new DatabaseExplorerViewModel(new FakeSchemaRepository(), ddl);
        await vm.InitializeAsync("local", refresh: false);
        ExplorerNodeViewModel root = Assert.Single(vm.RootNodes);

        Task load = vm.LoadDdlAsync(root);
        while (!vm.IsBusy)
            await Task.Yield();

        Assert.False(vm.DdlCommand.CanExecute(root));
        vm.Cancel();
        await load;

        Assert.Equal("Cancelled", vm.Status);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Object_explorer_uses_the_same_repository_and_disposes_cancellation()
    {
        var repository = new FakeSchemaRepository();
        using var vm = new ObjectExplorerViewModel(repository) { SqlText = "SELECT * FROM orders" };

        await vm.RefreshAsync("local");

        Assert.Single(vm.References);
        Assert.Equal("orders", vm.References[0].Name);
        Assert.Equal("local", repository.LastReferenceConnection);
    }

    [Fact]
    public async Task A_superseded_object_refresh_cannot_clear_busy_state_of_the_new_refresh()
    {
        var repository = new FakeSchemaRepository { BlockFirstReferences = true };
        using var vm = new ObjectExplorerViewModel(repository) { SqlText = "SELECT * FROM orders" };

        Task first = vm.RefreshAsync("local");
        await repository.FirstReferencesStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Task second = vm.RefreshAsync("local");
        await Task.WhenAll(first, second);

        Assert.False(vm.IsBusy);
        Assert.Equal("1 reference(s)", vm.Status);
        Assert.Single(vm.References);
    }

    [Fact]
    public async Task A_superseded_database_refresh_cannot_clear_busy_state_of_the_new_refresh()
    {
        var repository = new FakeSchemaRepository { BlockFirstRefresh = true };
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());

        Task first = vm.RefreshAsync();
        await repository.FirstRefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Task second = vm.RefreshAsync();
        await Task.WhenAll(first, second);

        Assert.False(vm.IsBusy);
        Assert.Equal("1 node(s)", vm.Status);
        Assert.Single(vm.RootNodes);
    }

    [Fact]
    public async Task Large_schema_keeps_children_lazy_for_ten_thousand_nodes()
    {
        var repository = new FakeSchemaRepository { LargeRootCount = 10_000 };
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await vm.InitializeAsync(refresh: false);

        stopwatch.Stop();
        Assert.Equal(10_000, vm.RootNodes.Count);
        Assert.All(vm.RootNodes, node => Assert.False(node.ChildrenLoaded));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Large schema initialization took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task Large_child_collection_renders_an_initial_hundred_then_continues_in_batches()
    {
        var repository = new FakeSchemaRepository { ChildCount = 250 };
        var scheduler = new BlockingBatchScheduler();
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService(), batchScheduler: scheduler);
        await vm.InitializeAsync("local");
        ExplorerNodeViewModel root = Assert.Single(vm.RootNodes);

        Task expansion = vm.ExpandAsync(root);

        Assert.Equal(100, root.Children.Count);
        Assert.True(root.IsLoading);
        Assert.False(root.ChildrenLoaded);

        scheduler.ReleaseFirstBatch();
        await expansion;

        Assert.Equal(250, root.Children.Count);
        Assert.True(root.ChildrenLoaded);
        Assert.False(root.HasPendingChildren);
        Assert.False(root.IsLoading);
    }

    [Fact]
    public async Task Empty_db2_object_group_refreshes_catalog_before_completing_expansion()
    {
        var repository = new Db2LazyCatalogRepository();
        using var vm = new DatabaseExplorerViewModel(repository, new FakeDdlService());

        await vm.InitializeAsync("db2", refresh: false);
        ExplorerNodeViewModel connection = Assert.Single(vm.RootNodes);
        await vm.ExpandAsync(connection);
        ExplorerNodeViewModel database = Assert.Single(connection.Children);
        await vm.ExpandAsync(database);
        ExplorerNodeViewModel tableGroup = Assert.Single(database.Children);

        await vm.ExpandAsync(tableGroup);

        Assert.Equal(1, repository.RefreshCalls);
        Assert.True(tableGroup.ChildrenLoaded);
        Assert.Equal("JBL_LIVE.JBL_ORDERS", Assert.Single(tableGroup.Children).Model.DisplayName);
    }

    private sealed class BlockingBatchScheduler : IExplorerBatchScheduler
    {
        private readonly TaskCompletionSource _firstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _delayCalls;

        public Task DelayAsync(CancellationToken cancellationToken = default)
        {
            return Interlocked.Increment(ref _delayCalls) == 1
                ? _firstBatch.Task.WaitAsync(cancellationToken)
                : Task.CompletedTask;
        }

        public void ReleaseFirstBatch() => _firstBatch.TrySetResult();
    }

    private sealed class Db2LazyCatalogRepository : ISchemaRepository
    {
        public int RefreshCalls { get; private set; }

        public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(
            string? connectionName = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaNode>>([
                new("db2", "db2", SchemaNodeKind.Connection, new("db2"), true)
            ]);

        public Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(
            SchemaNode parent,
            CancellationToken cancellationToken = default)
        {
            return parent.Kind switch
            {
                SchemaNodeKind.Connection => Task.FromResult<IReadOnlyList<SchemaNode>>([
                    new("db2/TESTDB", "TESTDB", SchemaNodeKind.Database, new("db2", "TESTDB"), true)
                ]),
                SchemaNodeKind.Database => Task.FromResult<IReadOnlyList<SchemaNode>>([
                    new("db2/TESTDB/TABLE", "TABLE", SchemaNodeKind.ObjectGroup, new("db2", "TESTDB"), true)
                ]),
                SchemaNodeKind.ObjectGroup when RefreshCalls == 0 => Task.FromResult<IReadOnlyList<SchemaNode>>([]),
                SchemaNodeKind.ObjectGroup => Task.FromResult<IReadOnlyList<SchemaNode>>([
                    new(
                        "db2/TESTDB/TABLE/JBL_LIVE/JBL_ORDERS",
                        "JBL_ORDERS",
                        SchemaNodeKind.Table,
                        new("db2", "TESTDB", "JBL_LIVE", "JBL_ORDERS"),
                        false,
                        DisplayName: "JBL_LIVE.JBL_ORDERS")
                ]),
                _ => Task.FromResult<IReadOnlyList<SchemaNode>>([])
            };
        }

        public Task<SchemaSearchResult> SearchAsync(SchemaSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchemaSearchResult([]));

        public Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaReference>>([]);

        public Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default, SchemaRefreshRequest? request = null)
        {
            RefreshCalls++;
            return Task.CompletedTask;
        }

        public Task<bool> AttachDatabaseAsync(string connectionName, string databaseName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeSchemaRepository : ISchemaRepository
    {
        public bool BlockRefresh { get; init; }
        public bool BlockFirstRefresh { get; init; }
        public bool BlockFirstReferences { get; init; }
        public int LargeRootCount { get; init; }
        public int ChildCount { get; init; }
        public string? LastReferenceConnection { get; private set; }
        public TaskCompletionSource FirstRefreshStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstReferencesStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _refreshCalls;
        private int _referenceCalls;

        private static SchemaNode Root => new("local", "local", SchemaNodeKind.Connection, new("local"), true);
        private static SchemaNode Table => new("local/orders", "orders", SchemaNodeKind.Table, new("local", "SYSTEM", "APP", "orders"), false);

        public async Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default, SchemaRefreshRequest? request = null)
        {
            if (BlockRefresh) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            if (BlockFirstRefresh && Interlocked.Increment(ref _refreshCalls) == 1)
            {
                FirstRefreshStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
        }

        public Task<bool> AttachDatabaseAsync(string connectionName, string databaseName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(string? connectionName = null, CancellationToken cancellationToken = default)
        {
            if (LargeRootCount == 0) return Task.FromResult<IReadOnlyList<SchemaNode>>([Root]);
            return Task.FromResult<IReadOnlyList<SchemaNode>>(Enumerable.Range(0, LargeRootCount)
                .Select(index => new SchemaNode($"node-{index}", $"node-{index}", SchemaNodeKind.Table, new("local", "SYSTEM", "APP", $"node-{index}"), false))
                .ToArray());
        }

        public Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(SchemaNode parent, CancellationToken cancellationToken = default)
        {
            if (ChildCount == 0)
                return Task.FromResult<IReadOnlyList<SchemaNode>>([Table]);

            return Task.FromResult<IReadOnlyList<SchemaNode>>(Enumerable.Range(0, ChildCount)
                .Select(index => new SchemaNode($"local/table-{index}", $"table-{index}", SchemaNodeKind.Table,
                    new("local", "SYSTEM", "APP", $"table-{index}"), false))
                .ToArray());
        }

        public Task<SchemaSearchResult> SearchAsync(SchemaSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchemaSearchResult([Table]));

        public async Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default)
        {
            LastReferenceConnection = connectionName;
            if (BlockFirstReferences && Interlocked.Increment(ref _referenceCalls) == 1)
            {
                FirstReferencesStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return [new("orders", SchemaNodeKind.Table, 14)];
        }
    }

    private sealed class FakeDdlService : ISchemaDdlService
    {
        public SchemaDdlRequest? LastRequest { get; private set; }
        public Task<string> GetDdlAsync(SchemaDdlRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult("CREATE TABLE orders");
        }
    }

    private sealed class BlockingDdlService : ISchemaDdlService
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<string> GetDdlAsync(SchemaDdlRequest request, CancellationToken cancellationToken = default)
        {
            await _release.Task.WaitAsync(cancellationToken);
            return "CREATE TABLE orders";
        }
    }
}
