using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.ViewModels.Editor;

namespace JustData.ViewModels.Tests;

public sealed class EditorWorkspaceDocumentEnsureTests
{
    [Fact]
    public void TryGetByEditorKey_returns_null_when_unmapped()
    {
        using var workspace = CreateWorkspace();
        var idsByEditor = new Dictionary<object, EditorDocumentId>();

        EditorDocumentViewModel? found = EditorWorkspaceDocumentEnsure.TryGetByEditorKey(
            workspace,
            idsByEditor,
            new object());

        Assert.Null(found);
    }

    [Fact]
    public void TryGetByEditorKey_returns_null_when_mapped_id_missing_from_workspace()
    {
        using var workspace = CreateWorkspace();
        var editor = new object();
        var idsByEditor = new Dictionary<object, EditorDocumentId>
        {
            [editor] = EditorDocumentId.New()
        };

        EditorDocumentViewModel? found = EditorWorkspaceDocumentEnsure.TryGetByEditorKey(
            workspace,
            idsByEditor,
            editor);

        Assert.Null(found);
    }

    [Fact]
    public void TryGetByEditorKey_returns_mapped_workspace_document()
    {
        using var workspace = CreateWorkspace();
        var document = workspace.AddDocumentFromView("tab", "select 1");
        var editor = new object();
        var idsByEditor = new Dictionary<object, EditorDocumentId>
        {
            [editor] = document.Id
        };

        EditorDocumentViewModel? found = EditorWorkspaceDocumentEnsure.TryGetByEditorKey(
            workspace,
            idsByEditor,
            editor);

        Assert.Same(document, found);
    }

    [Fact]
    public void GetOrCreateByEditorKey_returns_existing_without_creating()
    {
        using var workspace = CreateWorkspace();
        var document = workspace.AddDocumentFromView("tab", "select 1");
        var editor = new object();
        var idsByEditor = new Dictionary<object, EditorDocumentId>
        {
            [editor] = document.Id
        };
        int createCalls = 0;

        EditorDocumentViewModel? result = EditorWorkspaceDocumentEnsure.GetOrCreateByEditorKey(
            workspace,
            idsByEditor,
            editor,
            () =>
            {
                createCalls++;
                return workspace.AddDocumentFromView("created", "select 2");
            });

        Assert.Same(document, result);
        Assert.Equal(0, createCalls);
        Assert.Single(workspace.Documents);
    }

    [Fact]
    public void GetOrCreateByEditorKey_creates_when_unmapped()
    {
        using var workspace = CreateWorkspace();
        var editor = new object();
        var idsByEditor = new Dictionary<object, EditorDocumentId>();
        int createCalls = 0;

        EditorDocumentViewModel? result = EditorWorkspaceDocumentEnsure.GetOrCreateByEditorKey(
            workspace,
            idsByEditor,
            editor,
            () =>
            {
                createCalls++;
                var created = workspace.AddDocumentFromView("created", "select 2");
                idsByEditor[editor] = created.Id;
                return created;
            });

        Assert.NotNull(result);
        Assert.Equal(1, createCalls);
        Assert.Same(result, workspace.Documents.Single());
        Assert.Equal(result.Id, idsByEditor[editor]);
    }

    [Fact]
    public void GetOrCreateByEditorKey_creates_when_mapped_id_is_orphan()
    {
        using var workspace = CreateWorkspace();
        var editor = new object();
        var idsByEditor = new Dictionary<object, EditorDocumentId>
        {
            [editor] = EditorDocumentId.New()
        };

        EditorDocumentViewModel? result = EditorWorkspaceDocumentEnsure.GetOrCreateByEditorKey(
            workspace,
            idsByEditor,
            editor,
            () =>
            {
                var created = workspace.AddDocumentFromView("recovered", "select 3");
                idsByEditor[editor] = created.Id;
                return created;
            });

        Assert.NotNull(result);
        Assert.Same(result, workspace.Documents.Single());
        Assert.Equal(result.Id, idsByEditor[editor]);
    }

    private static EditorWorkspaceViewModel CreateWorkspace()
    {
        return new EditorWorkspaceViewModel(
            new FakeEditorFileService(),
            new FakeEditorFileWatchService(),
            new FakeBundleService(),
            new FakeEditorDialogService(),
            new FakeRecentFileStore());
    }

    private sealed class FakeEditorFileService : IEditorFileService
    {
        public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task WriteAsync(string path, string contents, bool useUtf8WithoutBom, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeEditorFileWatchService : IEditorFileWatchService
    {
        public IDisposable Watch(string path, Action<EditorFileChange> onChanged) =>
            new NoopDisposable();

        public void Dispose()
        {
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class FakeEditorDialogService : IEditorDialogService
    {
        public Task<UnsavedDocumentDecision> ConfirmUnsavedDocumentAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(UnsavedDocumentDecision.Discard);

        public Task<ExternalDocumentChangeDecision> ConfirmExternalChangeAsync(EditorDocumentSnapshot document, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExternalDocumentChangeDecision.KeepOpen);

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
