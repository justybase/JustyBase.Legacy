using JustData.Application.QueryWatch;
using JustData.ViewModels.QueryWatch;

namespace JustData.ViewModels.Tests.QueryWatch;

public sealed class QueryWatchViewModelTests
{
    [Fact]
    public async Task Refresh_populates_rows_and_columns()
    {
        var service = new FakeService([
            CreateRow(("ID", 1), ("USERNAME", "alice"), dropSql: "DROP SESSION 1;"),
            CreateRow(("ID", 2), ("USERNAME", "bob"), dropSql: null),
        ]);
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", "DB1", 4));

        await vm.RefreshAsync();

        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal(["ID", "USERNAME"], vm.ColumnNames);
        Assert.Equal("local · DB1", vm.ConnectionLabel);
        Assert.NotNull(vm.LastRefreshed);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Refresh_sets_error_message_on_failure()
    {
        var service = new FakeService { RefreshException = new InvalidOperationException("boom") };
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", null, 4));

        await vm.RefreshAsync();

        Assert.Empty(vm.Rows);
        Assert.Contains("boom", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void RequestDropSession_returns_sql_only_when_available()
    {
        var withDrop = CreateRow(("ID", 1), dropSql: "DROP SESSION 1;");
        var withoutDrop = CreateRow(("ID", 2), dropSql: "  ");
        var vm = new QueryWatchViewModel(
            new FakeService(),
            () => new QueryWatchContext("local", null, 4));

        Assert.Equal("DROP SESSION 1;", vm.RequestDropSession(withDrop));
        Assert.Null(vm.RequestDropSession(withoutDrop));
        Assert.Null(vm.RequestDropSession(null));
        Assert.True(withDrop.CanDrop);
        Assert.False(withoutDrop.CanDrop);
    }

    [Fact]
    public async Task DropSession_calls_service_then_refreshes()
    {
        var service = new FakeService([
            CreateRow(("ID", 1), dropSql: "DROP SESSION 1;"),
        ]);
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", "DB1", 4));
        await vm.RefreshAsync();

        service.Rows =
        [
            CreateRow(("ID", 2), dropSql: "DROP SESSION 2;"),
        ];

        await vm.DropSessionAsync(vm.Rows[0]);

        Assert.Equal(["DROP SESSION 1;"], service.DroppedSql);
        Assert.Equal(2, Assert.Single(vm.Rows).Values["ID"]);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task DropSession_without_sql_is_noop()
    {
        var service = new FakeService();
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", null, 4));
        var row = CreateRow(("ID", 1), dropSql: null);

        await vm.DropSessionAsync(row);

        Assert.Empty(service.DroppedSql);
        Assert.Equal(0, service.RefreshCallCount);
    }

    [Fact]
    public async Task DropSession_sets_error_on_failure()
    {
        var service = new FakeService([
            CreateRow(("ID", 1), dropSql: "DROP SESSION 1;"),
        ])
        {
            DropException = new InvalidOperationException("denied"),
        };
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", null, 4));
        await vm.RefreshAsync();

        await vm.DropSessionAsync(vm.Rows[0]);

        Assert.Contains("denied", vm.ErrorMessage);
        Assert.Equal(1, service.RefreshCallCount);
    }

    [Fact]
    public async Task Refresh_empty_result_clears_column_names()
    {
        var service = new FakeService([
            CreateRow(("ID", 1), ("USERNAME", "alice"), dropSql: "DROP SESSION 1;"),
        ]);
        var vm = new QueryWatchViewModel(service, () => new QueryWatchContext("local", "DB1", 4));
        await vm.RefreshAsync();
        Assert.NotEmpty(vm.ColumnNames);

        service.Rows = [];
        await vm.RefreshAsync();

        Assert.Empty(vm.Rows);
        Assert.Empty(vm.ColumnNames);
    }

    [Fact]
    public void AutoRefreshEnabled_defaults_to_false()
    {
        var vm = new QueryWatchViewModel(
            new FakeService(),
            () => new QueryWatchContext("local", null, 4));

        Assert.False(vm.AutoRefreshEnabled);
        vm.AutoRefreshEnabled = true;
        Assert.True(vm.AutoRefreshEnabled);
    }

    private static QueryWatchRow CreateRow(
        (string Name, object? Value) first,
        (string Name, object? Value)? second = null,
        string? dropSql = null)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [first.Name] = first.Value,
        };
        if (second is { } pair)
        {
            values[pair.Name] = pair.Value;
        }

        return new QueryWatchRow(values, dropSql);
    }

    private sealed class FakeService : IQueryWatchService
    {
        public FakeService(IReadOnlyList<QueryWatchRow>? rows = null)
        {
            Rows = rows ?? [];
        }

        public IReadOnlyList<QueryWatchRow> Rows { get; set; }
        public List<string> DroppedSql { get; } = [];
        public int RefreshCallCount { get; private set; }
        public Exception? RefreshException { get; set; }
        public Exception? DropException { get; set; }

        public bool IsSupported(int databaseType) => true;

        public Task<IReadOnlyList<QueryWatchRow>> RefreshAsync(
            QueryWatchContext context,
            CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;
            if (RefreshException is not null)
            {
                return Task.FromException<IReadOnlyList<QueryWatchRow>>(RefreshException);
            }

            return Task.FromResult(Rows);
        }

        public Task DropSessionAsync(
            string dropSql,
            QueryWatchContext context,
            CancellationToken cancellationToken = default)
        {
            if (DropException is not null)
            {
                return Task.FromException(DropException);
            }

            DroppedSql.Add(dropSql);
            return Task.CompletedTask;
        }
    }
}
