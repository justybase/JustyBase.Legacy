using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.ViewModels.Editor;

namespace JustData.ViewModels.Tests;

public sealed class EditorWorkspaceViewModelExtendedTests
{
    [Fact]
    public void NewDocument_generates_unique_titles()
    {
        using var workspace = CreateWorkspace();

        var doc1 = workspace.NewDocument("tab");
        var doc2 = workspace.NewDocument("tab");
        var doc3 = workspace.NewDocument("tab");

        Assert.Equal("tab", doc1.Title);
        Assert.Equal("tab2", doc2.Title);
        Assert.Equal("tab3", doc3.Title);
        Assert.Equal(3, workspace.Documents.Count);
    }

    [Fact]
    public void NewDocument_default_title_is_tab()
    {
        using var workspace = CreateWorkspace();

        var doc = workspace.NewDocument();

        Assert.Equal("tab", doc.Title);
    }

    [Fact]
    public void NewDocument_default_title_when_whitespace()
    {
        using var workspace = CreateWorkspace();

        var doc = workspace.NewDocument("  ");

        Assert.Equal("tab", doc.Title);
    }

    [Fact]
    public void RemoveDocument_removes_and_disposes_document()
    {
        using var workspace = CreateWorkspace();
        var doc = workspace.NewDocument("test");

        bool removed = workspace.RemoveDocument(doc.Id);

        Assert.True(removed);
        Assert.Empty(workspace.Documents);
        Assert.Null(workspace.ActiveDocument);
    }

    [Fact]
    public void RemoveDocument_returns_false_for_nonexistent()
    {
        using var workspace = CreateWorkspace();

        bool removed = workspace.RemoveDocument(EditorDocumentId.New());

        Assert.False(removed);
    }

    [Fact]
    public void Activate_throws_for_nonexistent_document()
    {
        using var workspace = CreateWorkspace();

        Assert.Throws<InvalidOperationException>(() => workspace.Activate(EditorDocumentId.New()));
    }

    [Fact]
    public void Activate_sets_active_document()
    {
        using var workspace = CreateWorkspace();
        var doc1 = workspace.NewDocument("first");
        var doc2 = workspace.NewDocument("second");

        workspace.Activate(doc1.Id);

        Assert.Same(doc1, workspace.ActiveDocument);

        workspace.Activate(doc2.Id);

        Assert.Same(doc2, workspace.ActiveDocument);
    }

    [Fact]
    public void Removing_active_document_activates_neighbor()
    {
        using var workspace = CreateWorkspace();
        var doc1 = workspace.NewDocument("first");
        var doc2 = workspace.NewDocument("second");
        var doc3 = workspace.NewDocument("third");

        workspace.Activate(doc2.Id);
        workspace.RemoveDocument(doc2.Id);

        Assert.NotNull(workspace.ActiveDocument);
        Assert.NotEqual(doc2.Id, workspace.ActiveDocument!.Id);
    }

    [Fact]
    public void Removing_last_document_sets_active_to_null()
    {
        using var workspace = CreateWorkspace();
        var doc = workspace.NewDocument("only");

        workspace.RemoveDocument(doc.Id);

        Assert.Null(workspace.ActiveDocument);
    }

    [Fact]
    public void AddDocumentFromView_duplicates_path_activates_existing()
    {
        string path = Path.Combine(Path.GetTempPath(), "dedup.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "select 1";
        using var workspace = CreateWorkspace(files);

        var first = workspace.AddDocumentFromView("tab", "select 1", path);
        var second = workspace.AddDocumentFromView("tab2", "select 2", path);

        Assert.Same(first, second);
        Assert.Single(workspace.Documents);
    }

    [Fact]
    public void AddDocumentFromView_without_path_always_creates_new()
    {
        using var workspace = CreateWorkspace();

        var doc1 = workspace.AddDocumentFromView("tab", "select 1");
        var doc2 = workspace.AddDocumentFromView("tab", "select 2");

        Assert.NotSame(doc1, doc2);
        Assert.Equal(2, workspace.Documents.Count);
    }

    [Fact]
    public async Task CloseAsync_saves_before_closing_when_user_chooses_save()
    {
        string path = Path.Combine(Path.GetTempPath(), "close-save.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "select 1";
        var dialog = new FakeEditorDialogService { UnsavedDecision = UnsavedDocumentDecision.Save };
        using var workspace = CreateWorkspace(files, dialog: dialog);

        var doc = await workspace.OpenPathAsync(path);
        doc.UpdateTextFromView("select 2");

        bool closed = await workspace.CloseAsync(doc.Id);

        Assert.True(closed);
        Assert.Empty(workspace.Documents);
        Assert.Equal("select 2", files.Files[Path.GetFullPath(path)]);
    }

    [Fact]
    public async Task ExternalChangeDetected_event_propagates()
    {
        string path = Path.Combine(Path.GetTempPath(), "watch-event.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "select 1";
        var watcher = new FakeEditorFileWatchService();
        using var workspace = CreateWorkspace(files, watcher: watcher);

        var doc = await workspace.OpenPathAsync(path);
        EditorFileChange? receivedChange = null;
        workspace.ExternalChangeDetected += (d, c) => receivedChange = c;

        watcher.Raise(path, new EditorFileChange(EditorFileChangeKind.Changed, path));

        Assert.NotNull(receivedChange);
        Assert.Equal(EditorFileChangeKind.Changed, receivedChange!.Kind);
    }

    [Fact]
    public void DocumentClosed_event_fires_on_remove()
    {
        using var workspace = CreateWorkspace();
        var doc = workspace.NewDocument("test");
        EditorDocumentViewModel? closedDoc = null;
        workspace.DocumentClosed += d => closedDoc = d;

        workspace.RemoveDocument(doc.Id);

        Assert.NotNull(closedDoc);
        Assert.Equal(doc.Id, closedDoc!.Id);
    }

    [Fact]
    public async Task SaveAllAsync_saves_all_dirty_documents()
    {
        string path1 = Path.Combine(Path.GetTempPath(), "saveall1.sql");
        string path2 = Path.Combine(Path.GetTempPath(), "saveall2.sql");
        var files = new FakeEditorFileService();
        files.Files[path1] = "select 1";
        files.Files[path2] = "select 2";
        using var workspace = CreateWorkspace(files);

        var doc1 = await workspace.OpenPathAsync(path1);
        var doc2 = await workspace.OpenPathAsync(path2);
        doc1.UpdateTextFromView("modified 1");
        doc2.UpdateTextFromView("modified 2");

        bool saved = await workspace.SaveAllAsync();

        Assert.True(saved);
        Assert.Equal("modified 1", files.Files[Path.GetFullPath(path1)]);
        Assert.Equal("modified 2", files.Files[Path.GetFullPath(path2)]);
    }

    [Fact]
    public void Documents_collection_updates_on_new_and_close()
    {
        using var workspace = CreateWorkspace();
        var changed = new List<string>();
        workspace.Documents.CollectionChanged += (_, e) =>
            changed.Add(e.Action.ToString());

        var doc = workspace.NewDocument("test");
        workspace.RemoveDocument(doc.Id);

        Assert.Contains("Add", changed);
        Assert.Contains("Remove", changed);
    }

    private static EditorWorkspaceViewModel CreateWorkspace(
        FakeEditorFileService? files = null,
        FakeEditorFileWatchService? watcher = null,
        FakeEditorDialogService? dialog = null)
    {
        files ??= new FakeEditorFileService();
        watcher ??= new FakeEditorFileWatchService();
        dialog ??= new FakeEditorDialogService();
        return new EditorWorkspaceViewModel(
            files, watcher, new FakeBundleService(), dialog, new FakeRecentFileStore());
    }

    private sealed class FakeEditorFileService : IEditorFileService
    {
        public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files[Path.GetFullPath(path)]);

        public Task WriteAsync(string path, string contents, bool useUtf8WithoutBom, CancellationToken cancellationToken = default)
        {
            Files[Path.GetFullPath(path)] = contents;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEditorFileWatchService : IEditorFileWatchService
    {
        private readonly Dictionary<string, Action<EditorFileChange>> _callbacks = new(StringComparer.OrdinalIgnoreCase);

        public IDisposable Watch(string path, Action<EditorFileChange> onChanged)
        {
            string normalized = Path.GetFullPath(path);
            _callbacks[normalized] = onChanged;
            return new Registration(() => _callbacks.Remove(normalized));
        }

        public void Raise(string path, EditorFileChange change)
        {
            if (_callbacks.TryGetValue(Path.GetFullPath(path), out var callback))
                callback?.Invoke(change);
        }

        public void Dispose() => _callbacks.Clear();

        private sealed class Registration(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    private sealed class FakeEditorDialogService : IEditorDialogService
    {
        public UnsavedDocumentDecision UnsavedDecision { get; set; } = UnsavedDocumentDecision.Discard;
        public ExternalDocumentChangeDecision ExternalDecision { get; set; } = ExternalDocumentChangeDecision.KeepOpen;

        public Task<UnsavedDocumentDecision> ConfirmUnsavedDocumentAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(UnsavedDecision);

        public Task<ExternalDocumentChangeDecision> ConfirmExternalChangeAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExternalDecision);

        public Task<string?> PickSavePathAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class FakeBundleService : IManySqlBundleService
    {
        public Task<ManySqlBundle> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManySqlBundle([], [], [], 0));

        public Task SaveAsync(string path, ManySqlBundle bundle, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeRecentFileStore : IRecentFileStore
    {
        public Task<IReadOnlyList<string>> LoadAsync(RecentFileKind kind, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task SaveAsync(RecentFileKind kind, IReadOnlyList<string> paths, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
