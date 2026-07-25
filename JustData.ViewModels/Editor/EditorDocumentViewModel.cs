using CommunityToolkit.Mvvm.ComponentModel;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Editor;

public sealed class EditorDocumentViewModel : ObservableObject, IDisposable
{
    private readonly IEditorFileWatchService _watchService;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly CancellationTokenSource _lifetime = new();
    private IDisposable? _watchRegistration;
    private string _title;
    private string _text;
    private string? _filePath;
    private bool _isDirty;
    private bool _isLoading;
    private bool _isReadOnly;
    private string _connectionName;
    private string _databaseName;
    private bool _keepConnectionOpen;
    private bool _continueOnError;
    private bool _externalChangePending;
    private long _ignoreExternalChangesUntilTicks;
    private int _selectionStart;
    private int _selectionLength;
    private int _caretOffset;
    private bool _disposed;

    public EditorDocumentViewModel(
        EditorDocumentId id,
        string title,
        string text,
        string? filePath,
        string connectionName,
        string databaseName,
        bool keepConnectionOpen,
        bool continueOnError,
        IEditorFileWatchService watchService,
        ISqlExecutionUseCase? sqlExecutionUseCase = null,
        ISqlAuthoringUseCase? sqlAuthoringUseCase = null,
        IUiDispatcher? uiDispatcher = null)
    {
        Id = id;
        _title = string.IsNullOrWhiteSpace(title) ? "tab" : title;
        _text = text ?? string.Empty;
        _connectionName = connectionName ?? string.Empty;
        _databaseName = databaseName ?? string.Empty;
        _keepConnectionOpen = keepConnectionOpen;
        _continueOnError = continueOnError;
        _watchService = watchService ?? throw new ArgumentNullException(nameof(watchService));
        _uiDispatcher = uiDispatcher;

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _filePath = NormalizePath(filePath);
            _title = Path.GetFileName(_filePath);
            AttachWatcher();
        }

        SqlExecution = new SqlExecutionViewModel(Id, BuildExecutionRequest, sqlExecutionUseCase, _uiDispatcher);
        SqlAuthoring = new SqlAuthoringViewModel(Id, sqlAuthoringUseCase, _uiDispatcher);
        SqlAuthoring.DiagnosticsChanged += OnDiagnosticsChanged;
    }

    public EditorDocumentId Id { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Text
    {
        get => _text;
        private set => SetProperty(ref _text, value);
    }

    public string? FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        internal set => SetProperty(ref _isLoading, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    public string ConnectionName
    {
        get => _connectionName;
        set => SetProperty(ref _connectionName, value ?? string.Empty);
    }

    public string DatabaseName
    {
        get => _databaseName;
        set => SetProperty(ref _databaseName, value ?? string.Empty);
    }

    public bool KeepConnectionOpen
    {
        get => _keepConnectionOpen;
        set => SetProperty(ref _keepConnectionOpen, value);
    }

    public bool ContinueOnError
    {
        get => _continueOnError;
        set => SetProperty(ref _continueOnError, value);
    }

    public bool ExternalChangePending
    {
        get => _externalChangePending;
        private set => SetProperty(ref _externalChangePending, value);
    }

    public int SelectionStart
    {
        get => _selectionStart;
        private set => SetProperty(ref _selectionStart, value);
    }

    public int SelectionLength
    {
        get => _selectionLength;
        private set => SetProperty(ref _selectionLength, value);
    }

    public int CaretOffset
    {
        get => _caretOffset;
        private set => SetProperty(ref _caretOffset, value);
    }

    public SqlExecutionViewModel SqlExecution { get; }

    public SqlExecutionViewModel Execution => SqlExecution;

    public SqlAuthoringViewModel SqlAuthoring { get; }

    public SqlAuthoringViewModel Authoring => SqlAuthoring;

    public ObservableCollection<SqlDiagnostic> Diagnostics => SqlAuthoring.Diagnostics;

    public event Action<EditorDocumentViewModel, EditorFileChange>? ExternalChangeDetected;
    public event Action<EditorDocumentViewModel, IReadOnlyList<SqlDiagnostic>>? DiagnosticsChanged;

    public void UpdateTextFromView(string text)
    {
        ThrowIfDisposed();
        text ??= string.Empty;
        if (string.Equals(Text, text, StringComparison.Ordinal))
            return;

        Text = text;
        IsDirty = true;
        _ = SqlAuthoring.ScheduleLintAsync(Text, ConnectionName);
    }

    public void SetLoadedText(string text)
    {
        ThrowIfDisposed();
        Text = text ?? string.Empty;
        IsDirty = false;
        ExternalChangePending = false;
        _ = SqlAuthoring.ScheduleLintAsync(Text, ConnectionName);
    }

    public void MarkSaved()
    {
        ThrowIfDisposed();
        IsDirty = false;
        ExternalChangePending = false;
        if (SqlAuthoring.LintOnSave)
            _ = SqlAuthoring.LintOnSaveAsync(Text, ConnectionName);
    }

    /// <summary>Copies the active editor's selection into document state.</summary>
    public void UpdateEditorSelection(int selectionStart, int selectionLength, int caretOffset)
    {
        ThrowIfDisposed();
        SelectionStart = Math.Max(0, selectionStart);
        SelectionLength = Math.Max(0, selectionLength);
        CaretOffset = Math.Max(0, caretOffset);
    }

    public SqlExecutionRequest BuildExecutionRequest(
        SqlExecutionMode mode = SqlExecutionMode.Selection,
        SqlOutputMode outputMode = SqlOutputMode.Grid,
        string? outputPath = null)
    {
        ThrowIfDisposed();
        return new SqlExecutionRequest(Id, Text)
        {
            ConnectionName = ConnectionName,
            DatabaseName = DatabaseName,
            Mode = mode,
            OutputMode = outputMode,
            SelectionStart = SelectionStart,
            SelectionLength = SelectionLength,
            CaretOffset = CaretOffset,
            KeepConnectionOpen = KeepConnectionOpen,
            ContinueOnError = ContinueOnError,
            OutputPath = outputPath
        };
    }

    /// <summary>
    /// Prevents the file watcher from treating the document's own save as an
    /// external edit. The short grace period covers the multiple notifications
    /// emitted by common editors and by <see cref="FileSystemWatcher"/>.
    /// </summary>
    public void SuppressExternalChanges(TimeSpan duration)
    {
        ThrowIfDisposed();
        if (duration <= TimeSpan.Zero)
            return;

        Interlocked.Exchange(
            ref _ignoreExternalChangesUntilTicks,
            DateTime.UtcNow.Add(duration).Ticks);
    }

    public void SetSavedPath(string path)
    {
        ThrowIfDisposed();
        string normalized = NormalizePath(path);
        if (!string.Equals(FilePath, normalized, StringComparison.OrdinalIgnoreCase))
        {
            _watchRegistration?.Dispose();
            _watchRegistration = null;
            FilePath = normalized;
            Title = Path.GetFileName(normalized);
            AttachWatcher();
        }
        else
        {
            Title = Path.GetFileName(normalized);
        }
    }

    public EditorDocumentSnapshot ToSnapshot() => new(
        Id,
        Title,
        Text,
        FilePath,
        IsDirty,
        IsReadOnly,
        ConnectionName,
        DatabaseName,
        KeepConnectionOpen,
        ContinueOnError,
        ExternalChangePending);

    private void AttachWatcher()
    {
        if (_disposed || string.IsNullOrWhiteSpace(FilePath))
            return;

        _watchRegistration = _watchService.Watch(FilePath, change =>
        {
            if (_disposed)
                return;

            void Apply()
            {
                if (_disposed)
                    return;

                if (DateTime.UtcNow.Ticks <= Volatile.Read(ref _ignoreExternalChangesUntilTicks))
                    return;

                ExternalChangePending = true;
                ExternalChangeDetected?.Invoke(this, change);
            }

            try
            {
                if (_uiDispatcher is null || _uiDispatcher.CheckAccess())
                    Apply();
                else
                    _ = _uiDispatcher.InvokeAsync(Apply, _lifetime.Token)
                        .ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                // Disposal racing with a final watcher notification is benign.
            }
        });
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path.Trim());

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void OnDiagnosticsChanged(IReadOnlyList<SqlDiagnostic> diagnostics) =>
        DiagnosticsChanged?.Invoke(this, diagnostics);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        SqlExecution.Dispose();
        SqlAuthoring.DiagnosticsChanged -= OnDiagnosticsChanged;
        SqlAuthoring.Dispose();
        _lifetime.Cancel();
        _lifetime.Dispose();
        _watchRegistration?.Dispose();
        _watchRegistration = null;
        ExternalChangeDetected = null;
        DiagnosticsChanged = null;
    }
}
