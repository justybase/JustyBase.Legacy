using JustData.Application.Files;

namespace JustData.Preferences.Tests;

public sealed class RecentFileStoreExtensionsTests
{
    [Fact]
    public async Task RecordAsync_prepends_path_and_saves()
    {
        var store = new FakeRecentStore(["old.sql"]);
        await store.RecordAsync(RecentFileKind.Single, "new.sql");

        Assert.Equal(["new.sql", "old.sql"], store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_removes_existing_duplicate()
    {
        var store = new FakeRecentStore(["first.sql", "second.sql", "third.sql"]);
        await store.RecordAsync(RecentFileKind.Single, "second.sql");

        Assert.Equal(["second.sql", "first.sql", "third.sql"], store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_case_insensitive_dedup()
    {
        var store = new FakeRecentStore(["C:\\Docs\\Query.sql"]);
        await store.RecordAsync(RecentFileKind.Single, "c:\\docs\\query.sql");

        Assert.Equal(["c:\\docs\\query.sql"], store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_limits_to_20_entries()
    {
        var existing = Enumerable.Range(0, 25).Select(i => $"file{i}.sql").ToArray();
        var store = new FakeRecentStore(existing);

        await store.RecordAsync(RecentFileKind.Single, "new.sql");

        Assert.Equal(20, store.LastSaved!.Count);
        Assert.Equal("new.sql", store.LastSaved[0]);
    }

    [Fact]
    public async Task RecordAsync_does_not_save_empty_path()
    {
        var store = new FakeRecentStore(["old.sql"]);
        await store.RecordAsync(RecentFileKind.Single, "");

        Assert.Null(store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_does_not_save_whitespace_path()
    {
        var store = new FakeRecentStore(["old.sql"]);
        await store.RecordAsync(RecentFileKind.Single, "   ");

        Assert.Null(store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_passes_correct_kind()
    {
        var store = new FakeRecentStore(["old.manysql"]);
        await store.RecordAsync(RecentFileKind.ManySql, "new.manysql");

        Assert.Equal(RecentFileKind.ManySql, store.LastKind);
    }

    [Fact]
    public async Task RecordAsync_empty_existing_list()
    {
        var store = new FakeRecentStore([]);
        await store.RecordAsync(RecentFileKind.Single, "first.sql");

        Assert.Equal(["first.sql"], store.LastSaved);
    }

    [Fact]
    public async Task RecordAsync_disposes_store_with_null_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IRecentFileStore)null!).RecordAsync(RecentFileKind.Single, "test.sql"));
    }

    private sealed class FakeRecentStore : IRecentFileStore
    {
        private readonly List<string> _files;
        public IReadOnlyList<string>? LastSaved { get; private set; }
        public RecentFileKind LastKind { get; private set; }

        public FakeRecentStore(string[] initial)
        {
            _files = initial.ToList();
        }

        public Task<IReadOnlyList<string>> LoadAsync(RecentFileKind kind, CancellationToken cancellationToken = default)
        {
            LastKind = kind;
            return Task.FromResult<IReadOnlyList<string>>(_files.ToArray());
        }

        public Task SaveAsync(RecentFileKind kind, IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
        {
            LastSaved = paths.ToArray();
            return Task.CompletedTask;
        }
    }
}
