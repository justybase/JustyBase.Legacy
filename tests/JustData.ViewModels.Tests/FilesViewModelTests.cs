using JustData.Application.Files;
using JustData.ViewModels.Files;

namespace JustData.ViewModels.Tests;

public sealed class FilesViewModelTests
{
    [Fact]
    public async Task Initialize_loads_entries_and_starts_watching_roots()
    {
        FakeFileService files = new();
        FakeWatchService watch = new();
        using FilesViewModel vm = new(files, new FakeRecentStore(), watch, new FakePicker());

        await vm.InitializeAsync(["root"], sortByLastWrite: true, sortByName: false, [".sql"]);

        Assert.Equal(["root"], vm.RootPaths);
        Assert.Equal(["root/a.sql"], vm.SearchFiles);
        Assert.True(watch.WatchCalled);
    }

    [Fact]
    public async Task Search_projects_query_options_and_exposes_result()
    {
        FakeFileService files = new();
        using FilesViewModel vm = new(files, new FakeRecentStore(), new FakeWatchService(), new FakePicker());
        await vm.InitializeAsync(["root"], false, false, [".sql"]);
        vm.SearchQuery = "select";
        vm.ExtensionPatterns = "*.sql";
        vm.MatchWholeWord = true;
        vm.MatchCase = true;
        vm.UseRegex = true;

        await vm.SearchAsync();

        Assert.Equal("select", files.LastRequest!.Query);
        Assert.True(files.LastRequest.MatchWholeWord);
        Assert.True(files.LastRequest.MatchCase);
        Assert.True(files.LastRequest.UseRegex);
        Assert.Single(vm.LastSearch.Files);
    }

    [Fact]
    public async Task Cancel_search_cancels_the_in_flight_operation_and_disposes_only_registration()
    {
        FakeFileService files = new() { BlockSearch = true };
        FakeWatchService watch = new();
        using FilesViewModel vm = new(files, new FakeRecentStore(), watch, new FakePicker());
        await vm.InitializeAsync(["root"], false, false, [".sql"]);
        vm.SearchQuery = "select";

        Task search = vm.SearchAsync();
        while (!vm.IsBusy) await Task.Yield();
        vm.CancelSearch();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);

        vm.Dispose();
        Assert.True(watch.RegistrationDisposed);
        Assert.False(watch.Disposed);
        watch.Emit(new(FileChangeKind.Created, "root/late.sql"));
        Assert.DoesNotContain(vm.Entries, entry => entry.Path == "root/late.sql");
    }

    [Fact]
    public async Task Remove_root_after_dispose_is_rejected()
    {
        using var vm = new FilesViewModel(new FakeFileService(), new FakeRecentStore(), new FakeWatchService(), new FakePicker());
        vm.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => vm.RemoveRootAsync("root"));
    }

    [Fact]
    public async Task File_operations_are_exposed_as_commands_and_watcher_deletions_update_state()
    {
        FakeFileService files = new();
        FakeWatchService watch = new();
        using FilesViewModel vm = new(files, new FakeRecentStore(), watch, new FakePicker());
        await vm.InitializeAsync(["root"], false, false, [".sql"]);

        await vm.CreateDirectoryCommand.ExecuteAsync("root/new");
        await vm.CreateFileCommand.ExecuteAsync("root/new.sql");
        await vm.RenameCommand.ExecuteAsync(("root/new.sql", "root/renamed.sql"));
        await vm.DeleteCommand.ExecuteAsync("root/renamed.sql");
        watch.Emit(new(FileChangeKind.Created, "root/c.sql"));
        watch.Emit(new(FileChangeKind.Renamed, "root/d.sql", "root/c.sql"));
        watch.Emit(new(FileChangeKind.Deleted, "root/a.sql"));

        Assert.Equal(["root/new", "root/new.sql", "root/renamed.sql", "root/renamed.sql"], files.Operations);
        Assert.Contains(vm.Entries, entry => entry.Path == "root/d.sql");
        Assert.DoesNotContain(vm.Entries, entry => entry.Path == "root/a.sql");
    }

    [Fact]
    public async Task Recording_a_recent_file_preserves_order_and_deduplicates()
    {
        FakeRecentStore recent = new(["old.sql", "same.sql"]);
        using FilesViewModel vm = new(new FakeFileService(), recent, new FakeWatchService(), new FakePicker());

        await vm.RecordRecentFileAsync("same.sql");

        Assert.Equal(["same.sql", "old.sql"], recent.Saved);
    }

    private sealed class FakeFileService : IDocumentFileService
    {
        public bool BlockSearch { get; init; }
        public FileSearchRequest? LastRequest { get; private set; }
        public List<string> Operations { get; } = [];

        public Task<IReadOnlyList<FileSystemEntry>> EnumerateAsync(IReadOnlyList<string> roots, FileEnumerationOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FileSystemEntry>>([new("root", true), new("root/a.sql", false)]);

        public async Task<FileSearchResult> SearchAsync(IReadOnlyList<string> candidateFiles, FileSearchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (BlockSearch) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new([new("root/a.sql", [new(1, "select", 0, 6)], false)], false, false, 1);
        }

        public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default) { Operations.Add(path); return Task.CompletedTask; }
        public Task CreateFileAsync(string path, CancellationToken cancellationToken = default) { Operations.Add(path); return Task.CompletedTask; }
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) { Operations.Add(path); return Task.CompletedTask; }
        public Task RenameAsync(string path, string newPath, CancellationToken cancellationToken = default) { Operations.Add(newPath); return Task.CompletedTask; }
    }

    private sealed class FakeRecentStore(IReadOnlyList<string>? initial = null) : IRecentFileStore
    {
        public IReadOnlyList<string> Saved { get; private set; } = initial ?? [];
        public Task<IReadOnlyList<string>> LoadAsync(RecentFileKind kind, CancellationToken cancellationToken = default) => Task.FromResult(Saved);
        public Task SaveAsync(RecentFileKind kind, IReadOnlyList<string> paths, CancellationToken cancellationToken = default) { Saved = paths; return Task.CompletedTask; }
    }

    private sealed class FakeWatchService : IFileWatchService
    {
        public bool WatchCalled { get; private set; }
        public bool Disposed { get; private set; }
        public bool RegistrationDisposed { get; private set; }
        private Action<FileChange>? _onChanged;
        public IDisposable Watch(IReadOnlyList<string> roots, Action<FileChange> onChanged)
        {
            WatchCalled = true;
            _onChanged = onChanged;
            return new ActionDisposable(() => RegistrationDisposed = true);
        }
        public void Emit(FileChange change) => _onChanged?.Invoke(change);
        public void Dispose() => Disposed = true;
    }

    private sealed class FakePicker : IFilePickerService
    {
        public string? PickFolder() => null;
    }

    private sealed class ActionDisposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
