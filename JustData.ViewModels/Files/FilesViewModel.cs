using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Files;
using JustData.ViewModels;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Files;

public sealed class FilesViewModel : ObservableObject, IDisposable
{
    private readonly IDocumentFileService _fileService;
    private readonly IRecentFileStore _recentFileStore;
    private readonly IFileWatchService _watchService;
    private readonly IFilePickerService _filePicker;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _operationCancellation;
    private IDisposable? _watchRegistration;
    private string _searchQuery = string.Empty;
    private string _extensionPatterns = string.Empty;
    private bool _matchWholeWord;
    private bool _matchCase;
    private bool _useRegex;
    private bool _isBusy;
    private TimeSpan _searchTimeout = TimeSpan.FromSeconds(10);
    private FileSearchResult _lastSearch = new([], false, false, 0);
    private bool _disposed;
    private IReadOnlyList<string> _extensions = [];
    private bool _sortByLastWrite;
    private bool _sortByName;
    private readonly IUiDispatcher? _uiDispatcher;

    public FilesViewModel(
        IDocumentFileService fileService,
        IRecentFileStore recentFileStore,
        IFileWatchService watchService,
        IFilePickerService filePicker,
        IUiDispatcher? uiDispatcher = null)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _recentFileStore = recentFileStore ?? throw new ArgumentNullException(nameof(recentFileStore));
        _watchService = watchService ?? throw new ArgumentNullException(nameof(watchService));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _uiDispatcher = uiDispatcher;

        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync, CanAddFolder);
        RefreshCommand = new AsyncRelayCommand(() => RefreshAsync(), CanRefresh);
        SearchCommand = new AsyncRelayCommand(() => SearchAsync(), CanSearch);
        CancelSearchCommand = new RelayCommand(CancelSearch, () => IsBusy);
        RemoveRootCommand = new AsyncRelayCommand<string?>(RemoveRootAsync, CanRemoveRoot);
        CreateDirectoryCommand = new AsyncRelayCommand<string?>(CreateDirectoryAsync, HasPath);
        CreateFileCommand = new AsyncRelayCommand<string?>(CreateFileAsync, HasPath);
        DeleteCommand = new AsyncRelayCommand<string?>(DeleteAsync, HasPath);
        RenameCommand = new AsyncRelayCommand<(string Path, string NewPath)>(RenameAsync);
    }

    public ObservableCollection<string> RootPaths { get; } = [];

    public ObservableCollection<FileSystemEntry> Entries { get; } = [];

    public IReadOnlyList<string> SearchFiles { get; private set; } = [];

    public FileSearchResult LastSearch
    {
        get => _lastSearch;
        private set => SetProperty(ref _lastSearch, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value ?? string.Empty))
                SearchCommand.NotifyCanExecuteChanged();
        }
    }

    public string ExtensionPatterns
    {
        get => _extensionPatterns;
        set => SetProperty(ref _extensionPatterns, value ?? string.Empty);
    }

    public bool MatchWholeWord { get => _matchWholeWord; set => SetProperty(ref _matchWholeWord, value); }
    public bool MatchCase { get => _matchCase; set => SetProperty(ref _matchCase, value); }
    public bool UseRegex { get => _useRegex; set => SetProperty(ref _useRegex, value); }

    public TimeSpan SearchTimeout
    {
        get => _searchTimeout;
        set => SetProperty(ref _searchTimeout, value <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CancelSearchCommand.NotifyCanExecuteChanged();
                RefreshCommand.NotifyCanExecuteChanged();
                SearchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand AddFolderCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand CancelSearchCommand { get; }
    public IAsyncRelayCommand<string?> RemoveRootCommand { get; }
    public IAsyncRelayCommand<string?> CreateDirectoryCommand { get; }
    public IAsyncRelayCommand<string?> CreateFileCommand { get; }
    public IAsyncRelayCommand<string?> DeleteCommand { get; }
    public IAsyncRelayCommand<(string Path, string NewPath)> RenameCommand { get; }

    public async Task InitializeAsync(
        IEnumerable<string> roots,
        bool sortByLastWrite,
        bool sortByName,
        IEnumerable<string>? extensions = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var normalizedRoots = roots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        await OnUiAsync(() =>
        {
            RootPaths.Clear();
            foreach (var root in normalizedRoots)
                RootPaths.Add(root);
            RestartWatchers();
        }, cancellationToken).ConfigureAwait(false);

        _sortByLastWrite = sortByLastWrite;
        _sortByName = sortByName;
        _extensions = (extensions ?? []).ToArray();

        await RefreshAsync(sortByLastWrite, sortByName, _extensions, cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshAsync(
        bool? sortByLastWrite = null,
        bool? sortByName = null,
        IEnumerable<string>? extensions = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await RunOperationAsync(async token =>
        {
            if (extensions is not null)
                _extensions = extensions.ToArray();
            if (sortByLastWrite.HasValue)
                _sortByLastWrite = sortByLastWrite.Value;
            if (sortByName.HasValue)
                _sortByName = sortByName.Value;
            var options = new FileEnumerationOptions(
                _extensions, _sortByLastWrite, _sortByName);
            string[] roots = [];
            await OnUiAsync(() => roots = RootPaths.ToArray(), token).ConfigureAwait(false);
            var entries = await _fileService.EnumerateAsync(roots, options, token).ConfigureAwait(false);
            await OnUiAsync(() =>
            {
                Entries.Clear();
                foreach (var entry in entries)
                    Entries.Add(entry);
                SearchFiles = entries.Where(entry => !entry.IsDirectory).Select(entry => entry.Path).ToArray();
                OnPropertyChanged(nameof(SearchFiles));
            }, token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => LastSearch = new([], false, false, 0),
                cancellationToken);
            return;
        }

        await RunOperationAsync(async token =>
        {
            FileSearchRequest? request = null;
            string[] files = [];
            await OnUiAsync(() =>
            {
                request = new FileSearchRequest(
                    SearchQuery,
                    ParseExtensions(ExtensionPatterns),
                    MatchWholeWord,
                    MatchCase,
                    UseRegex,
                    Timeout: SearchTimeout);
                files = SearchFiles.ToArray();
            }, token).ConfigureAwait(false);
            FileSearchResult result = await _fileService
                .SearchAsync(files, request!, token)
                .ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(
                () => LastSearch = result,
                token);
        }, cancellationToken).ConfigureAwait(false);
    }

    public void CancelSearch() => _operationCancellation?.Cancel();

    public async Task AddRootAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;
        await OnUiAsync(() =>
        {
            if (!RootPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                RootPaths.Add(path);
            RestartWatchers();
        }, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordRecentFileAsync(string path, RecentFileKind kind = RecentFileKind.Single, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var paths = (await _recentFileStore.LoadAsync(kind, cancellationToken).ConfigureAwait(false))
            .Where(existing => !string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
            .Prepend(path)
            .Take(20)
            .ToArray();
        await _recentFileStore.SaveAsync(kind, paths, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<string>> LoadRecentFilesAsync(RecentFileKind kind, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _recentFileStore.LoadAsync(kind, cancellationToken);
    }

    private async Task AddFolderAsync()
    {
        ThrowIfDisposed();
        var path = _filePicker.PickFolder();
        if (!string.IsNullOrWhiteSpace(path))
            await AddRootAsync(path).ConfigureAwait(false);
    }

    public async Task RemoveRootAsync(string? path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(path)) return;
        await OnUiAsync(() =>
        {
            RootPaths.Remove(path);
            RestartWatchers();
        }, cancellationToken).ConfigureAwait(false);
        await RefreshAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task CreateDirectoryAsync(string? path)
    {
        ThrowIfDisposed();
        return _fileService.CreateDirectoryAsync(path!);
    }

    private Task CreateFileAsync(string? path)
    {
        ThrowIfDisposed();
        return _fileService.CreateFileAsync(path!);
    }

    private Task DeleteAsync(string? path)
    {
        ThrowIfDisposed();
        return _fileService.DeleteAsync(path!);
    }

    private Task RenameAsync((string Path, string NewPath) value)
    {
        ThrowIfDisposed();
        return _fileService.RenameAsync(value.Path, value.NewPath);
    }

    private async Task RunOperationAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        _operationCancellation = linked;
        if (!_disposed)
            await _uiDispatcher.InvokeOnUiAsync(
                () => IsBusy = true,
                cancellationToken);
        try { await operation(linked.Token).ConfigureAwait(false); }
        finally
        {
            if (ReferenceEquals(_operationCancellation, linked))
                _operationCancellation = null;
            if (!_disposed)
                await _uiDispatcher.InvokeOnUiAsync(
                    () => IsBusy = false,
                    CancellationToken.None);
        }
    }

    private void ApplyFileChange(FileChange change)
    {
        if (_disposed) return;
        try
        {
            // Capture the token before scheduling. Dispose can run between
            // the disposed check and this callback on FileSystemWatcher’s
            // thread, and CTS.Token throws after the source is disposed.
            CancellationToken lifetimeToken = _lifetime.Token;
            if (_disposed) return;

            _ = OnUiAsync(() => ApplyFileChangeCore(change), lifetimeToken)
                .ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        catch (OperationCanceledException)
        {
            // Disposal racing with a final watcher notification is benign.
        }
        catch (ObjectDisposedException)
        {
            // The lifetime CTS may be disposed immediately after the first
            // disposed check. A late watcher event is still harmless.
        }
    }

    private void ApplyFileChangeCore(FileChange change)
    {
        if (_disposed) return;
        if (change.Kind == FileChangeKind.Deleted)
            Entries.Remove(Entries.FirstOrDefault(entry => string.Equals(entry.Path, change.Path, StringComparison.OrdinalIgnoreCase))!);
        else if (change.Kind == FileChangeKind.Renamed)
        {
            Entries.Remove(Entries.FirstOrDefault(entry => string.Equals(entry.Path, change.OldPath, StringComparison.OrdinalIgnoreCase))!);
            if (!Entries.Any(entry => string.Equals(entry.Path, change.Path, StringComparison.OrdinalIgnoreCase)))
                Entries.Add(new FileSystemEntry(change.Path, Directory.Exists(change.Path)));
        }
        else if (!Entries.Any(entry => string.Equals(entry.Path, change.Path, StringComparison.OrdinalIgnoreCase)))
        {
            Entries.Add(new FileSystemEntry(change.Path, Directory.Exists(change.Path)));
        }
        SearchFiles = Entries.Where(entry => !entry.IsDirectory).Select(entry => entry.Path).ToArray();
        OnPropertyChanged(nameof(SearchFiles));
    }

    private void RestartWatchers()
    {
        _watchRegistration?.Dispose();
        _watchRegistration = _watchService.Watch(RootPaths, ApplyFileChange);
    }

    private Task OnUiAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _uiDispatcher.InvokeOnUiAsync(() =>
        {
            if (!_disposed)
                action();
        }, cancellationToken);
    }

    private static IReadOnlyList<string> ParseExtensions(string value) =>
        value.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pattern => pattern.StartsWith("*.", StringComparison.Ordinal) ? pattern[1..] : pattern)
            .Select(pattern => pattern.StartsWith(".", StringComparison.Ordinal) ? pattern : "." + pattern)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private bool CanRefresh() => !IsBusy;
    private bool CanSearch() => !IsBusy && !string.IsNullOrWhiteSpace(SearchQuery);
    private bool CanAddFolder() => !IsBusy;
    private bool CanRemoveRoot(string? path) => !IsBusy && !string.IsNullOrWhiteSpace(path);
    private static bool HasPath(string? path) => !string.IsNullOrWhiteSpace(path);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _watchRegistration?.Dispose();
        _lifetime.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
