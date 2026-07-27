using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.Application.Sql;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Editor;

public sealed class EditorWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly IEditorFileService _fileService;
    private readonly IEditorFileWatchService _watchService;
    private readonly IManySqlBundleService _bundleService;
    private readonly IEditorDialogService _dialogService;
    private readonly IRecentFileStore _recentFileStore;
    private readonly ISqlExecutionUseCase? _sqlExecutionUseCase;
    private readonly ISqlAuthoringUseCase? _sqlAuthoringUseCase;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<string, EditorDocumentViewModel> _documentsByPath = new(StringComparer.OrdinalIgnoreCase);
    private EditorDocumentViewModel? _activeDocument;
    private bool _disposed;

    public EditorWorkspaceViewModel(
        IEditorFileService fileService,
        IEditorFileWatchService watchService,
        IManySqlBundleService bundleService,
        IEditorDialogService dialogService,
        IRecentFileStore recentFileStore,
        ISqlExecutionUseCase? sqlExecutionUseCase = null,
        ISqlAuthoringUseCase? sqlAuthoringUseCase = null,
        IUiDispatcher? uiDispatcher = null)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _watchService = watchService ?? throw new ArgumentNullException(nameof(watchService));
        _bundleService = bundleService ?? throw new ArgumentNullException(nameof(bundleService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _recentFileStore = recentFileStore ?? throw new ArgumentNullException(nameof(recentFileStore));
        _sqlExecutionUseCase = sqlExecutionUseCase;
        _sqlAuthoringUseCase = sqlAuthoringUseCase;
        _uiDispatcher = uiDispatcher;

        NewCommand = new AsyncRelayCommand(() => NewDocumentAsync());
        OpenCommand = new AsyncRelayCommand<string?>(OpenCommandAsync);
        CloseCommand = new AsyncRelayCommand<EditorDocumentId?>(CloseCommandAsync);
        SaveCommand = new AsyncRelayCommand<EditorDocumentId?>(SaveCommandAsync);
        SaveAsCommand = new AsyncRelayCommand<EditorDocumentId?>(SaveAsCommandAsync);
        SaveAllCommand = new AsyncRelayCommand(SaveAllAsync);
    }

    public ObservableCollection<EditorDocumentViewModel> Documents { get; } = [];

    public EditorDocumentViewModel? ActiveDocument
    {
        get => _activeDocument;
        private set => SetProperty(ref _activeDocument, value);
    }

    public IAsyncRelayCommand NewCommand { get; }
    public IAsyncRelayCommand<string?> OpenCommand { get; }
    public IAsyncRelayCommand<EditorDocumentId?> CloseCommand { get; }
    public IAsyncRelayCommand<EditorDocumentId?> SaveCommand { get; }
    public IAsyncRelayCommand<EditorDocumentId?> SaveAsCommand { get; }
    public IAsyncRelayCommand SaveAllCommand { get; }

    public event Action<EditorDocumentViewModel, EditorFileChange>? ExternalChangeDetected;
    public event Action<EditorDocumentViewModel>? DocumentReloaded;
    public event Action<EditorDocumentViewModel>? DocumentClosed;

    public EditorDocumentViewModel? FindByPath(string path)
    {
        ThrowIfDisposed();
        return _documentsByPath.TryGetValue(NormalizePath(path), out var document) ? document : null;
    }

    /// <summary>
    /// Applies a presentation-reported document order to the workspace. The
    /// workspace remains the owner of the order; unknown or omitted documents
    /// retain their relative order at the end of the collection.
    /// </summary>
    public void Reorder(IReadOnlyList<EditorDocumentId> order)
    {
        ThrowIfDisposed();
        if (order is null || order.Count == 0 || Documents.Count < 2)
            return;

        var byId = Documents.ToDictionary(document => document.Id);
        var reordered = new List<EditorDocumentViewModel>(Documents.Count);
        var included = new HashSet<EditorDocumentId>();

        foreach (EditorDocumentId id in order)
        {
            if (byId.TryGetValue(id, out EditorDocumentViewModel? document) && included.Add(id))
                reordered.Add(document);
        }

        foreach (EditorDocumentViewModel document in Documents)
        {
            if (included.Add(document.Id))
                reordered.Add(document);
        }

        if (Documents.SequenceEqual(reordered))
            return;

        Documents.Clear();
        foreach (EditorDocumentViewModel document in reordered)
            Documents.Add(document);
    }

    public IReadOnlyList<EditorDocumentId> GetDocumentOrder() => Documents
        .Select(document => document.Id)
        .ToArray();

    /// <summary>
    /// Registers a document whose editor control is being created by the
    /// WinForms adapter.  The adapter uses this synchronous seam while the
    /// legacy control construction remains synchronous; normal file-opening
    /// code should use <see cref="OpenPathAsync"/>.
    /// </summary>
    public EditorDocumentViewModel AddDocumentFromView(
        string title,
        string text,
        string? filePath = null,
        string? connectionName = null,
        string? databaseName = null,
        bool keepConnectionOpen = false,
        bool continueOnError = false)
    {
        ThrowIfDisposed();
        if (!string.IsNullOrWhiteSpace(filePath)
            && FindByPath(filePath) is { } existing)
        {
            Activate(existing.Id);
            return existing;
        }

        var document = CreateDocument(
            GetUniqueTitle(title),
            text,
            filePath,
            connectionName ?? ActiveDocument?.ConnectionName ?? string.Empty,
            databaseName ?? ActiveDocument?.DatabaseName ?? string.Empty,
            keepConnectionOpen,
            continueOnError);
        AddDocument(document);
        return document;
    }

    public bool RemoveDocument(EditorDocumentId id)
    {
        ThrowIfDisposed();
        var document = Documents.FirstOrDefault(item => item.Id == id);
        if (document is null)
            return false;
        RemoveDocumentCore(document);
        return true;
    }

    public EditorDocumentViewModel NewDocument(
        string? title = null,
        string text = "",
        string? connectionName = null,
        string? databaseName = null,
        bool? keepConnectionOpen = null,
        bool? continueOnError = null)
    {
        ThrowIfDisposed();
        string uniqueTitle = GetUniqueTitle(title);
        var document = CreateDocument(
            uniqueTitle,
            text,
            filePath: null,
            connectionName ?? ActiveDocument?.ConnectionName ?? string.Empty,
            databaseName ?? ActiveDocument?.DatabaseName ?? string.Empty,
            keepConnectionOpen ?? ActiveDocument?.KeepConnectionOpen ?? false,
            continueOnError ?? ActiveDocument?.ContinueOnError ?? false);
        AddDocument(document);
        return document;
    }

    public async Task<EditorDocumentViewModel> OpenPathAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        string normalizedPath = NormalizePath(path);
        if (_documentsByPath.TryGetValue(normalizedPath, out var existing))
        {
            Activate(existing.Id);
            return existing;
        }

        string text = await _fileService.ReadAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        var document = CreateDocument(
            Path.GetFileName(normalizedPath),
            text,
            normalizedPath,
            ActiveDocument?.ConnectionName ?? string.Empty,
            ActiveDocument?.DatabaseName ?? string.Empty,
            ActiveDocument?.KeepConnectionOpen ?? false,
            ActiveDocument?.ContinueOnError ?? false);
        await OnUiAsync(() => AddDocument(document), cancellationToken).ConfigureAwait(false);
        await _recentFileStore.RecordAsync(RecentFileKind.Single, normalizedPath, cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task<bool> SaveAsync(
        EditorDocumentId? id = null,
        CancellationToken cancellationToken = default,
        bool useUtf8WithoutBom = true)
    {
        ThrowIfDisposed();
        var document = Resolve(id);
        string? path = document.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = await _dialogService.PickSavePathAsync(document.ToSnapshot(), cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(path))
                return false;
        }

        return await SaveToPathAsync(document, path, cancellationToken, useUtf8WithoutBom).ConfigureAwait(false);
    }

    public async Task<bool> SaveAsAsync(
        EditorDocumentId? id = null,
        string? path = null,
        CancellationToken cancellationToken = default,
        bool useUtf8WithoutBom = true)
    {
        ThrowIfDisposed();
        var document = Resolve(id);
        path ??= await _dialogService.PickSavePathAsync(document.ToSnapshot(), cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(path)
            && await SaveToPathAsync(document, path, cancellationToken, useUtf8WithoutBom).ConfigureAwait(false);
    }

    public async Task<bool> SaveAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        foreach (var document in Documents.ToArray())
        {
            if (!document.IsDirty)
                continue;

            if (!await SaveAsync(document.Id, cancellationToken).ConfigureAwait(false))
                return false;
        }

        return true;
    }

    public async Task<bool> CloseAsync(
        EditorDocumentId? id = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var document = Resolve(id);
        if (document.IsDirty)
        {
            UnsavedDocumentDecision decision = await _dialogService
                .ConfirmUnsavedDocumentAsync(document.ToSnapshot(), cancellationToken).ConfigureAwait(false);
            if (decision == UnsavedDocumentDecision.Cancel)
                return false;
            if (decision == UnsavedDocumentDecision.Save
                && !await SaveAsync(document.Id, cancellationToken).ConfigureAwait(false))
                return false;
        }

        await OnUiAsync(() => RemoveDocumentCore(document), cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    public async Task<int> OpenManySqlAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ManySqlBundle bundle = await _bundleService.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        int firstDocumentIndex = Documents.Count;
        var openedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var openedContent = new HashSet<int>();

        foreach (string token in bundle.TabsOrder)
        {
            int pathIndex = FindPath(bundle.SqlPaths, token, openedPaths);
            if (pathIndex >= 0)
            {
                try
                {
                    await OpenPathAsync(bundle.SqlPaths[pathIndex], cancellationToken).ConfigureAwait(false);
                    openedPaths.Add(NormalizePath(bundle.SqlPaths[pathIndex]));
                }
                catch (FileNotFoundException)
                {
                    // Preserve the rest of the bundle when one file is gone.
                }
                continue;
            }

            int contentIndex = FindContent(bundle.SqlContentList, token, openedContent);
            if (contentIndex >= 0)
            {
                ManySqlContent content = bundle.SqlContentList[contentIndex];
                await OnUiAsync(() => NewDocument(content.Title, content.Text), cancellationToken).ConfigureAwait(false);
                openedContent.Add(contentIndex);
            }
        }

        for (int i = 0; i < bundle.SqlPaths.Count; i++)
        {
            if (openedPaths.Contains(NormalizePath(bundle.SqlPaths[i])))
                continue;
            try
            {
                await OpenPathAsync(bundle.SqlPaths[i], cancellationToken).ConfigureAwait(false);
                openedPaths.Add(NormalizePath(bundle.SqlPaths[i]));
            }
            catch (FileNotFoundException)
            {
            }
        }

        for (int i = 0; i < bundle.SqlContentList.Count; i++)
        {
            if (!openedContent.Contains(i))
            {
                ManySqlContent content = bundle.SqlContentList[i];
                await OnUiAsync(() => NewDocument(content.Title, content.Text), cancellationToken).ConfigureAwait(false);
            }
        }

        if (Documents.Count == firstDocumentIndex)
            await OnUiAsync(() => NewDocument("tab"), cancellationToken).ConfigureAwait(false);

        if (Documents.Count > 0)
        {
            int selectedIndex = Math.Clamp(firstDocumentIndex + bundle.SelectedTabNum, 0, Documents.Count - 1);
            await OnUiAsync(() => ActiveDocument = Documents[selectedIndex], cancellationToken).ConfigureAwait(false);
        }

        return Documents.Count - firstDocumentIndex;
    }

    public async Task SaveManySqlAsync(
        string path,
        CancellationToken cancellationToken = default,
        IReadOnlyList<EditorDocumentId>? documentOrder = null)
    {
        ThrowIfDisposed();
        if (documentOrder is { Count: > 0 })
            Reorder(documentOrder);

        var paths = new List<string>();
        var contents = new List<ManySqlContent>();
        var order = new List<string>();
        var tokens = new Dictionary<EditorDocumentId, string>();
        var documents = Documents.ToArray();

        foreach (var document in documents)
        {
            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                paths.Add(document.FilePath!);
                tokens[document.Id] = document.FilePath!;
            }
            else
            {
                contents.Add(new ManySqlContent(document.Title, document.Text));
                tokens[document.Id] = document.Title;
            }
        }

        foreach (var document in documents)
            order.Add(tokens[document.Id]);

        int selected = 0;
        if (ActiveDocument is not null)
        {
            for (int index = 0; index < documents.Length; index++)
            {
                if (ReferenceEquals(documents[index], ActiveDocument))
                {
                    selected = index;
                    break;
                }
            }
        }
        await _bundleService.SaveAsync(
            path,
            new ManySqlBundle(paths, contents, order, selected),
            cancellationToken).ConfigureAwait(false);
        await _recentFileStore.RecordAsync(RecentFileKind.ManySql, NormalizePath(path), cancellationToken).ConfigureAwait(false);
    }

    public void Activate(EditorDocumentId id)
    {
        ThrowIfDisposed();
        var document = Documents.FirstOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException($"Editor document '{id}' is not open.");
        ActiveDocument = document;
    }

    public async Task ReloadExternalAsync(EditorDocumentId id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var document = Resolve(id);
        if (string.IsNullOrWhiteSpace(document.FilePath))
            return;

        string text = await _fileService.ReadAsync(document.FilePath, cancellationToken).ConfigureAwait(false);
        await OnUiAsync(() =>
        {
            document.SetLoadedText(text);
            DocumentReloaded?.Invoke(document);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task NewDocumentAsync() => NewDocument();

    private Task OpenCommandAsync(string? path) => string.IsNullOrWhiteSpace(path)
        ? Task.CompletedTask
        : OpenPathAsync(path);

    private Task<bool> CloseCommandAsync(EditorDocumentId? id) => CloseAsync(id);

    private Task<bool> SaveCommandAsync(EditorDocumentId? id) => SaveAsync(id);

    private Task<bool> SaveAsCommandAsync(EditorDocumentId? id) => SaveAsAsync(id);

    private EditorDocumentViewModel CreateDocument(
        string title,
        string text,
        string? filePath,
        string connectionName,
        string databaseName,
        bool keepConnectionOpen,
        bool continueOnError)
    {
        var document = new EditorDocumentViewModel(
            EditorDocumentId.New(),
            title,
            text,
            filePath,
            connectionName,
            databaseName,
            keepConnectionOpen,
            continueOnError,
            _watchService,
            _sqlExecutionUseCase,
            _sqlAuthoringUseCase,
            _uiDispatcher);
        document.ExternalChangeDetected += OnDocumentExternalChange;
        return document;
    }

    private void AddDocument(EditorDocumentViewModel document)
    {
        Documents.Add(document);
        ActiveDocument = document;
        if (!string.IsNullOrWhiteSpace(document.FilePath))
            _documentsByPath[NormalizePath(document.FilePath)] = document;
    }

    private void RemoveDocumentCore(EditorDocumentViewModel document)
    {
        if (!string.IsNullOrWhiteSpace(document.FilePath))
            _documentsByPath.Remove(NormalizePath(document.FilePath));

        int index = Documents.IndexOf(document);
        Documents.Remove(document);
        document.ExternalChangeDetected -= OnDocumentExternalChange;
        document.Dispose();
        DocumentClosed?.Invoke(document);

        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = Documents.Count == 0
                ? null
                : Documents[Math.Clamp(index, 0, Documents.Count - 1)];
        }
    }

    private async Task<bool> SaveToPathAsync(
        EditorDocumentViewModel document,
        string path,
        CancellationToken cancellationToken,
        bool useUtf8WithoutBom)
    {
        string normalizedPath = NormalizePath(path);
        if (_documentsByPath.TryGetValue(normalizedPath, out var existing)
            && !ReferenceEquals(existing, document))
        {
            Activate(existing.Id);
            return false;
        }

        document.SuppressExternalChanges(TimeSpan.FromSeconds(2));
        await _fileService.WriteAsync(normalizedPath, document.Text, useUtf8WithoutBom, cancellationToken).ConfigureAwait(false);
        await OnUiAsync(() =>
        {
            string? previousPath = document.FilePath;
            document.SetSavedPath(normalizedPath);
            document.MarkSaved();
            if (!string.IsNullOrWhiteSpace(previousPath)
                && !string.Equals(previousPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                _documentsByPath.Remove(NormalizePath(previousPath));
            }
            _documentsByPath[normalizedPath] = document;
        }, cancellationToken).ConfigureAwait(false);
        await _recentFileStore.RecordAsync(RecentFileKind.Single, normalizedPath, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private void OnDocumentExternalChange(EditorDocumentViewModel document, EditorFileChange change)
    {
        _ = HandleDocumentExternalChangeAsync(document, change)
            .ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    private async Task HandleDocumentExternalChangeAsync(EditorDocumentViewModel document, EditorFileChange change)
    {
        try
        {
            await OnUiAsync(() => ExternalChangeDetected?.Invoke(document, change), _lifetime.Token)
                .ConfigureAwait(false);
            ExternalDocumentChangeDecision decision = await _dialogService
                .ConfirmExternalChangeAsync(document.ToSnapshot());
            if (decision == ExternalDocumentChangeDecision.Reload)
                await ReloadExternalAsync(document.Id, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The document remains open and marked pending when a watcher
            // notification cannot be resolved.
        }
    }

    private EditorDocumentViewModel Resolve(EditorDocumentId? id) =>
        id is null
            ? ActiveDocument ?? throw new InvalidOperationException("No active editor document exists.")
            : Documents.FirstOrDefault(item => item.Id == id.Value)
                ?? throw new InvalidOperationException($"Editor document '{id}' is not open.");

    private string GetUniqueTitle(string? requested)
    {
        string baseTitle = string.IsNullOrWhiteSpace(requested) ? "tab" : requested.Trim();
        if (!Documents.Any(document => string.Equals(document.Title, baseTitle, StringComparison.OrdinalIgnoreCase)))
            return baseTitle;

        for (int index = 2; ; index++)
        {
            string candidate = $"{baseTitle}{index}";
            if (!Documents.Any(document => string.Equals(document.Title, candidate, StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
    }

    private static int FindPath(
        IReadOnlyList<string> paths,
        string token,
        HashSet<string> openedPaths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (!openedPaths.Contains(NormalizePath(paths[i]))
                && string.Equals(paths[i], token, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static int FindContent(
        IReadOnlyList<ManySqlContent> contents,
        string token,
        HashSet<int> openedContent)
    {
        for (int i = 0; i < contents.Count; i++)
        {
            if (!openedContent.Contains(i)
                && string.Equals(contents[i].Title, token, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path.Trim());

    private Task OnUiAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _uiDispatcher.InvokeOnUiAsync(() =>
        {
            if (!_disposed)
                action();
        }, cancellationToken);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var document in Documents.ToArray())
        {
            document.ExternalChangeDetected -= OnDocumentExternalChange;
            document.Dispose();
        }

        Documents.Clear();
        _documentsByPath.Clear();
        ActiveDocument = null;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
