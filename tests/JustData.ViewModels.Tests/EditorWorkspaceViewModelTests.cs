using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.ViewModels.Editor;

namespace JustData.ViewModels.Tests;

public sealed class EditorWorkspaceViewModelTests
{
    [Fact]
    public async Task Opening_the_same_path_activates_the_existing_document()
    {
        string path = Path.Combine(Path.GetTempPath(), "editor-workspace-test.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT 1";
        var setup = Create(files);
        using var workspace = setup.Workspace;

        EditorDocumentViewModel first = await workspace.OpenPathAsync(path);
        EditorDocumentViewModel second = await workspace.OpenPathAsync(path.ToUpperInvariant());

        Assert.Same(first, second);
        Assert.Single(workspace.Documents);
        Assert.Same(first, workspace.ActiveDocument);
        Assert.False(first.IsDirty);
    }

    [Fact]
    public async Task Saving_updates_path_and_clears_dirty_only_after_success()
    {
        string path = Path.Combine(Path.GetTempPath(), "editor-save-test.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT 1";
        var setup = Create(files);
        using var workspace = setup.Workspace;

        EditorDocumentViewModel document = await workspace.OpenPathAsync(path);
        document.UpdateTextFromView("SELECT 2");
        Assert.True(document.IsDirty);

        Assert.True(await workspace.SaveAsync(document.Id));
        Assert.False(document.IsDirty);
        Assert.Equal("SELECT 2", files.Files[path]);
    }

    [Fact]
    public async Task Save_as_replaces_the_old_path_index()
    {
        string oldPath = Path.Combine(Path.GetTempPath(), "editor-save-as-old.sql");
        string newPath = Path.Combine(Path.GetTempPath(), "editor-save-as-new.sql");
        var files = new FakeEditorFileService();
        files.Files[oldPath] = "SELECT 1";
        var setup = Create(files);
        using var workspace = setup.Workspace;

        EditorDocumentViewModel document = await workspace.OpenPathAsync(oldPath);
        Assert.True(await workspace.SaveAsAsync(document.Id, newPath));

        Assert.Null(workspace.FindByPath(oldPath));
        Assert.Same(document, workspace.FindByPath(newPath));
        Assert.Equal(newPath, document.FilePath);
    }

    [Fact]
    public async Task Save_as_forwards_the_requested_utf8_bom_policy()
    {
        string path = Path.Combine(Path.GetTempPath(), "editor-save-encoding.sql");
        var files = new FakeEditorFileService();
        var setup = Create(files);
        using var workspace = setup.Workspace;
        EditorDocumentViewModel document = workspace.NewDocument("scratch", "SELECT 'żółw'");

        Assert.True(await workspace.SaveAsAsync(
            document.Id,
            path,
            useUtf8WithoutBom: false));

        Assert.False(files.LastUseUtf8WithoutBom);
    }

    [Fact]
    public async Task Closing_a_dirty_document_honors_cancel_and_discard()
    {
        var files = new FakeEditorFileService();
        var setup = Create(files);
        using var workspace = setup.Workspace;
        FakeEditorDialogService dialog = setup.Dialog;
        EditorDocumentViewModel document = workspace.NewDocument(text: "SELECT 1");
        document.UpdateTextFromView("SELECT 2");

        dialog.UnsavedDecision = UnsavedDocumentDecision.Cancel;
        Assert.False(await workspace.CloseAsync(document.Id));
        Assert.Single(workspace.Documents);

        dialog.UnsavedDecision = UnsavedDocumentDecision.Discard;
        Assert.True(await workspace.CloseAsync(document.Id));
        Assert.Empty(workspace.Documents);
    }

    [Fact]
    public async Task Many_sql_order_and_selected_document_are_restored()
    {
        string path = Path.Combine(Path.GetTempPath(), "bundle-file.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT file";
        var bundle = new FakeBundleService
        {
            Loaded = new ManySqlBundle(
                [path],
                [new ManySqlContent("memory", "SELECT memory")],
                ["memory", path],
                1)
        };
        var setup = Create(files, bundle);
        using var workspace = setup.Workspace;

        await workspace.OpenManySqlAsync("session.manysql");

        Assert.Equal(["memory", "bundle-file.sql"], workspace.Documents.Select(item => item.Title));
        Assert.Equal("bundle-file.sql", workspace.ActiveDocument?.Title);
    }

    [Fact]
    public async Task Saving_many_sql_preserves_paths_content_order_and_selection()
    {
        string path = Path.Combine(Path.GetTempPath(), "bundle-save-file.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT file";
        var bundle = new FakeBundleService();
        var setup = Create(files, bundle);
        using var workspace = setup.Workspace;

        EditorDocumentViewModel fileDocument = await workspace.OpenPathAsync(path);
        EditorDocumentViewModel memoryDocument = workspace.NewDocument("scratch", "SELECT memory");
        workspace.Activate(memoryDocument.Id);

        await workspace.SaveManySqlAsync("session.manysql");

        Assert.NotNull(bundle.Saved);
        Assert.Equal([path], bundle.Saved!.SqlPaths);
        Assert.Equal([new ManySqlContent("scratch", "SELECT memory")], bundle.Saved.SqlContentList);
        Assert.Equal([path, "scratch"], bundle.Saved.TabsOrder);
        Assert.Equal(1, bundle.Saved.SelectedTabNum);
        Assert.Equal(fileDocument, workspace.Documents[0]);
    }

    [Fact]
    public async Task Saving_many_sql_uses_the_supplied_view_order_for_tabs_and_selection()
    {
        string path = Path.Combine(Path.GetTempPath(), "bundle-view-order.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT file";
        var bundle = new FakeBundleService();
        var setup = Create(files, bundle);
        using var workspace = setup.Workspace;

        EditorDocumentViewModel fileDocument = await workspace.OpenPathAsync(path);
        EditorDocumentViewModel memoryDocument = workspace.NewDocument("scratch", "SELECT memory");
        workspace.Activate(fileDocument.Id);

        await workspace.SaveManySqlAsync(
            "session.manysql",
            documentOrder: [memoryDocument.Id, fileDocument.Id]);

        Assert.NotNull(bundle.Saved);
        Assert.Equal(["scratch", path], bundle.Saved!.TabsOrder);
        Assert.Equal(1, bundle.Saved.SelectedTabNum);
        Assert.Equal([memoryDocument.Id, fileDocument.Id], workspace.GetDocumentOrder());
    }

    [Fact]
    public void Reorder_keeps_unknown_documents_after_the_requested_order()
    {
        var setup = Create(new FakeEditorFileService());
        using var workspace = setup.Workspace;
        EditorDocumentViewModel first = workspace.NewDocument("first");
        EditorDocumentViewModel second = workspace.NewDocument("second");
        EditorDocumentViewModel third = workspace.NewDocument("third");

        workspace.Reorder([third.Id, first.Id, EditorDocumentId.New()]);

        Assert.Equal([third.Id, first.Id, second.Id], workspace.GetDocumentOrder());
        Assert.Same(third, workspace.ActiveDocument);
    }

    [Fact]
    public async Task External_change_marks_document_pending_and_reload_clears_it()
    {
        string path = Path.Combine(Path.GetTempPath(), "editor-watch-test.sql");
        var files = new FakeEditorFileService();
        files.Files[path] = "SELECT 1";
        var watcher = new FakeEditorFileWatchService();
        var dialog = new FakeEditorDialogService { ExternalDecision = ExternalDocumentChangeDecision.Reload };
        var setup = Create(files, null, watcher, dialog);
        using var workspace = setup.Workspace;
        EditorDocumentViewModel document = await workspace.OpenPathAsync(path);
        EditorDocumentViewModel? reloadedDocument = null;
        workspace.DocumentReloaded += reloaded => reloadedDocument = reloaded;

        files.Files[path] = "SELECT 2";
        watcher.Raise(path, new(EditorFileChangeKind.Changed, path));
        await WaitForAsync(() => document.Text == "SELECT 2");

        Assert.False(document.ExternalChangePending);
        Assert.Equal("SELECT 2", document.Text);
        Assert.Same(document, reloadedDocument);
    }

    private static (EditorWorkspaceViewModel Workspace, FakeEditorDialogService Dialog) Create(
        FakeEditorFileService files,
        FakeBundleService? bundle = null,
        FakeEditorFileWatchService? watcher = null,
        FakeEditorDialogService? dialog = null)
    {
        watcher ??= new FakeEditorFileWatchService();
        dialog ??= new FakeEditorDialogService();
        return (
            new EditorWorkspaceViewModel(
                files,
                watcher,
                bundle ?? new FakeBundleService(),
                dialog,
                new FakeRecentFileStore()),
            dialog);
    }

    private static async Task WaitForAsync(Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!predicate() && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        Assert.True(predicate());
    }

    private sealed class FakeEditorFileService : IEditorFileService
    {
        public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool LastUseUtf8WithoutBom { get; private set; }

        public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files[Path.GetFullPath(path)]);

        public Task WriteAsync(string path, string contents, bool useUtf8WithoutBom, CancellationToken cancellationToken = default)
        {
            LastUseUtf8WithoutBom = useUtf8WithoutBom;
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

        public void Raise(string path, EditorFileChange change) => _callbacks[Path.GetFullPath(path)](change);
        public void Dispose() => _callbacks.Clear();

        private sealed class Registration(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    private sealed class FakeBundleService : IManySqlBundleService
    {
        public ManySqlBundle Loaded { get; init; } = new([], [], [], 0);
        public string? SavedPath { get; private set; }
        public ManySqlBundle? Saved { get; private set; }

        public Task<ManySqlBundle> LoadAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(Loaded);

        public Task SaveAsync(string path, ManySqlBundle bundle, CancellationToken cancellationToken = default)
        {
            SavedPath = path;
            Saved = bundle;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEditorDialogService : IEditorDialogService
    {
        public UnsavedDocumentDecision UnsavedDecision { get; set; } = UnsavedDocumentDecision.Discard;
        public ExternalDocumentChangeDecision ExternalDecision { get; set; } = ExternalDocumentChangeDecision.KeepOpen;
        public string? SavePath { get; set; }

        public Task<UnsavedDocumentDecision> ConfirmUnsavedDocumentAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(UnsavedDecision);

        public Task<ExternalDocumentChangeDecision> ConfirmExternalChangeAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExternalDecision);

        public Task<string?> PickSavePathAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(SavePath);
    }

    private sealed class FakeRecentFileStore : IRecentFileStore
    {
        private readonly Dictionary<RecentFileKind, IReadOnlyList<string>> _values = new();

        public Task<IReadOnlyList<string>> LoadAsync(RecentFileKind kind, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(kind, out var values) ? values : (IReadOnlyList<string>)Array.Empty<string>());

        public Task SaveAsync(RecentFileKind kind, IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
        {
            _values[kind] = paths.ToArray();
            return Task.CompletedTask;
        }
    }
}
