using System.Collections.ObjectModel;
using JustData.Application.History;
using JustData.ViewModels.History;

namespace JustData.ViewModels.Tests.History;

public sealed class HistoryViewModelTests
{
    [Fact]
    public async Task Load_populates_filtered_entries()
    {
        var store = new FakeStore([
            new HistoryEntry(new DateTime(2025, 1, 1), "SELECT 1", "TestDB", "local")
        ]);
        var vm = new HistoryViewModel(store);

        Assert.False(vm.IsLoaded);
        await vm.LoadAsync("path.json");

        Assert.True(vm.IsLoaded);
        Assert.Single(vm.FilteredEntries);
        Assert.Equal("SELECT 1", vm.FilteredEntries[0].Sql);
    }

    [Fact]
    public async Task SearchText_filters_by_sql()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT * FROM users", "DB1", "local"),
            new HistoryEntry(DateTime.UtcNow, "INSERT INTO logs", "DB1", "local"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "SELECT";

        Assert.Single(vm.FilteredEntries);
        Assert.Contains("SELECT", vm.FilteredEntries[0].Sql);
    }

    [Fact]
    public async Task SearchText_filters_by_database()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "local"),
            new HistoryEntry(DateTime.UtcNow, "SELECT 2", "DB2", "dev"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "DB2";

        Assert.Single(vm.FilteredEntries);
        Assert.Equal("DB2", vm.FilteredEntries[0].Database);
    }

    [Fact]
    public async Task SearchText_filters_by_connection_name()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "production"),
            new HistoryEntry(DateTime.UtcNow, "SELECT 2", "DB1", "staging"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "production";

        Assert.Single(vm.FilteredEntries);
        Assert.Equal("production", vm.FilteredEntries[0].ConnectionName);
    }

    [Fact]
    public async Task SearchText_empty_shows_all_entries()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "local"),
            new HistoryEntry(DateTime.UtcNow, "SELECT 2", "DB2", "dev"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "";

        Assert.Equal(2, vm.FilteredEntries.Count);
    }

    [Fact]
    public async Task SearchText_whitespace_shows_all_entries()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "local"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "   ";

        Assert.Single(vm.FilteredEntries);
    }

    [Fact]
    public async Task Filter_method_updates_SearchText_and_filters()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "local"),
            new HistoryEntry(DateTime.UtcNow, "DROP TABLE", "DB1", "local"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.Filter("DROP");

        Assert.Single(vm.FilteredEntries);
        Assert.Equal("DROP TABLE", vm.FilteredEntries[0].Sql);
    }

    [Fact]
    public async Task Filter_reapplied_when_SearchText_changes()
    {
        var store = new FakeStore([
            new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB1", "local"),
            new HistoryEntry(DateTime.UtcNow, "SELECT 2", "DB2", "dev"),
            new HistoryEntry(DateTime.UtcNow, "INSERT", "DB3", "prod"),
        ]);
        var vm = new HistoryViewModel(store);
        await vm.LoadAsync("path.json");

        vm.SearchText = "SELECT";
        Assert.Equal(2, vm.FilteredEntries.Count);

        vm.SearchText = "INSERT";
        Assert.Single(vm.FilteredEntries);
    }

    [Fact]
    public async Task Load_error_sets_ErrorMessage_and_clears_entries()
    {
        var store = new FakeStore([]) { LoadException = new InvalidOperationException("test error") };
        var vm = new HistoryViewModel(store);

        await vm.LoadAsync("path.json");

        Assert.Contains("test error", vm.ErrorMessage);
        Assert.Empty(vm.FilteredEntries);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Load_cancellation_propagates()
    {
        var store = new FakeStore([]) { LoadCancellation = true };
        var vm = new HistoryViewModel(store);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => vm.LoadAsync("path.json", cts.Token));

        Assert.False(vm.IsLoaded);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task IsBusy_set_during_load()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<HistoryEntry>>();
        var store = new AsyncStore(tcs.Task);
        var vm = new HistoryViewModel(store);

        var loadTask = vm.LoadAsync("path.json");
        Assert.True(vm.IsBusy);

        tcs.TrySetResult([]);
        await loadTask;

        Assert.False(vm.IsBusy);
        Assert.True(vm.IsLoaded);
    }

    [Fact]
    public void SelectedEntry_can_be_set_and_read()
    {
        var store = new FakeStore([]);
        var vm = new HistoryViewModel(store);
        var entry = new HistoryEntry(DateTime.UtcNow, "SELECT 1", "DB", "conn");

        vm.SelectedEntry = entry;

        Assert.Same(entry, vm.SelectedEntry);
    }

    [Fact]
    public void SearchText_default_empty()
    {
        var store = new FakeStore([]);
        var vm = new HistoryViewModel(store);

        Assert.Equal("", vm.SearchText);
    }

    [Fact]
    public void ErrorMessage_default_null()
    {
        var store = new FakeStore([]);
        var vm = new HistoryViewModel(store);

        Assert.Null(vm.ErrorMessage);
    }

    private sealed class FakeStore(IReadOnlyList<HistoryEntry> entries) : IHistoryStore
    {
        public Exception? LoadException { get; set; }
        public bool LoadCancellation { get; set; }

        public Task<IReadOnlyList<HistoryEntry>> LoadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (LoadCancellation)
                return Task.FromCanceled<IReadOnlyList<HistoryEntry>>(cancellationToken);
            if (LoadException is not null)
                return Task.FromException<IReadOnlyList<HistoryEntry>>(LoadException);
            return Task.FromResult(entries);
        }
    }

    private sealed class AsyncStore(Task<IReadOnlyList<HistoryEntry>> result) : IHistoryStore
    {
        public Task<IReadOnlyList<HistoryEntry>> LoadAsync(string filePath, CancellationToken cancellationToken = default)
            => result;
    }
}
