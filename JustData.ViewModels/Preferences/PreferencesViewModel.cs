using CommunityToolkit.Mvvm.Input;
using JustData.Application.Settings;

namespace JustData.ViewModels.Preferences;

/// <summary>Transient transactional VM for the Preferences document.</summary>
public sealed class PreferencesViewModel : ViewModelBase, IDisposable
{
    private readonly IApplicationSettingsStore _store;
    private readonly ISettingsThemePreview? _themePreview;
    private bool _isLoaded;
    private bool _isBusy;
    private bool _isCancelled;
    private bool _isSaved;
    private string? _errorMessage;
    private IReadOnlyDictionary<string, string> _validationErrors = new Dictionary<string, string>();
    private CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _operationCancellation;
    private bool _disposed;

    public PreferencesViewModel(IApplicationSettingsStore store, ISettingsThemePreview? themePreview = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _themePreview = themePreview;
        Draft = new ApplicationSettingsDraft();
        Appearance = new AppearanceSettingsViewModel(Draft.Appearance);
        Editor = new EditorSettingsViewModel(Draft.Editor);
        SqlResults = new SqlResultsSettingsViewModel(Draft.SqlResults);
        ImportExport = new ImportExportSettingsViewModel(Draft.ImportExport);
        FilesStartup = new FilesStartupSettingsViewModel(Draft.FilesStartup);
        Lint = new LintSettingsViewModel(Draft.Lint);
        Terminal = new TerminalSettingsViewModel(Draft.Terminal);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ApplicationSettingsDraft Draft { get; private set; }
    public ApplicationSettingsSnapshot? LoadedSnapshot { get; private set; }
    public AppearanceSettingsViewModel Appearance { get; }
    public EditorSettingsViewModel Editor { get; }
    public SqlResultsSettingsViewModel SqlResults { get; }
    public ImportExportSettingsViewModel ImportExport { get; }
    public FilesStartupSettingsViewModel FilesStartup { get; }
    public LintSettingsViewModel Lint { get; }
    public TerminalSettingsViewModel Terminal { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand ReloadCommand { get; }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) NotifyCommands(); } }
    public bool IsCancelled { get => _isCancelled; private set => SetProperty(ref _isCancelled, value); }
    public bool IsSaved { get => _isSaved; private set => SetProperty(ref _isSaved, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public IReadOnlyDictionary<string, string> ValidationErrors { get => _validationErrors; private set { if (SetProperty(ref _validationErrors, value)) NotifyCommands(); } }
    public bool IsValid => ValidationErrors.Count == 0;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var operation = BeginOperation(cancellationToken);
        IsBusy = true;
        ErrorMessage = null;
        IsCancelled = false;
        IsSaved = false;
        try
        {
            var snapshot = await _store.LoadAsync(operation.Token).ConfigureAwait(false);
            ApplySnapshot(snapshot);
            _isLoaded = true;
            Validate();
            _themePreview?.Preview(Draft);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            if (!_disposed)
            {
                IsCancelled = true;
                _themePreview?.Revert();
            }
        }
        catch (Exception ex)
        {
            if (_disposed)
                return;
            ErrorMessage = "Unable to load preferences.";
            ValidationErrors = new Dictionary<string, string>();
            System.Diagnostics.Trace.WriteLine($"Preferences load failed: {ex.GetType().Name}");
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, operation))
                _operationCancellation = null;
            if (!_disposed)
                IsBusy = false;
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Validate();
        if (!CanSave()) return;
        using var operation = BeginOperation(cancellationToken);
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _store.SaveAsync(Draft.Clone(), operation.Token).ConfigureAwait(false);
            LoadedSnapshot = new ApplicationSettingsSnapshot(Draft);
            _themePreview?.Commit(LoadedSnapshot);
            IsSaved = true;
            IsCancelled = false;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            if (!_disposed)
            {
                IsCancelled = true;
                _themePreview?.Revert();
            }
        }
        catch (Exception ex)
        {
            if (_disposed)
                return;
            ErrorMessage = "Unable to save preferences.";
            System.Diagnostics.Trace.WriteLine($"Preferences save failed: {ex.GetType().Name}");
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, operation))
                _operationCancellation = null;
            if (!_disposed)
                IsBusy = false;
        }
    }

    public void Cancel()
    {
        ThrowIfDisposed();
        if (IsBusy)
        {
            _operationCancellation?.Cancel();
            _themePreview?.Revert();
            IsCancelled = true;
            return;
        }
        _themePreview?.Revert();
        IsCancelled = true;
        IsSaved = false;
        ErrorMessage = null;
    }

    public void PreviewTheme() { ThrowIfDisposed(); _themePreview?.Preview(Draft); }

    /// <summary>Replaces the editable draft after a legacy control buffer has been projected into it.</summary>
    public void ReplaceDraft(ApplicationSettingsDraft draft)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(draft);
        Draft = draft.Clone();
        Appearance.ReplaceValues(Draft.Appearance);
        Editor.ReplaceValues(Draft.Editor);
        SqlResults.ReplaceValues(Draft.SqlResults);
        ImportExport.ReplaceValues(Draft.ImportExport);
        FilesStartup.ReplaceValues(Draft.FilesStartup);
        Lint.ReplaceValues(Draft.Lint);
        Terminal.ReplaceValues(Draft.Terminal);
        Validate();
        OnPropertyChanged(nameof(Draft));
    }

    public void Validate()
    {
        var errors = new Dictionary<string, string>();
        if (Draft.Appearance.FontSize <= 0) errors["Appearance.FontSize"] = "Font size must be positive.";
        if (Draft.Editor.TypoLimit is < 1 or > 4) errors["Editor.TypoLimit"] = "Typo limit must be between 1 and 4.";
        if (Draft.Editor.FileSearchTimeout is < 1000 or > 3_600_000) errors["Editor.FileSearchTimeout"] = "File search timeout is outside the legacy range.";
        if (Draft.SqlResults.CommandTimeout < 2) errors["SqlResults.CommandTimeout"] = "Command timeout must be at least 2 seconds.";
        if (Draft.SqlResults.ResultRowsLimit < 10) errors["SqlResults.ResultRowsLimit"] = "Result row limit must be at least 10.";
        if (Draft.SqlResults.ResultRowsLimitWarning < 10) errors["SqlResults.ResultRowsLimitWarning"] = "Warning row limit must be at least 10.";
        if (Draft.SqlResults.MaxSchemaParallelism is < 1 or > 64) errors["SqlResults.MaxSchemaParallelism"] = "Schema parallelism must be between 1 and 64.";
        ValidationErrors = errors;
        OnPropertyChanged(nameof(IsValid));
    }

    private bool CanSave() => _isLoaded && !_isBusy && IsValid;
    private bool CanCancel() => _isLoaded;
    private void NotifyCommands() { SaveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged(); OnPropertyChanged(nameof(IsValid)); }

    private void ApplySnapshot(ApplicationSettingsSnapshot snapshot)
    {
        LoadedSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Draft = snapshot.ToDraft();
        Appearance.ReplaceValues(Draft.Appearance);
        Editor.ReplaceValues(Draft.Editor);
        SqlResults.ReplaceValues(Draft.SqlResults);
        ImportExport.ReplaceValues(Draft.ImportExport);
        FilesStartup.ReplaceValues(Draft.FilesStartup);
        Lint.ReplaceValues(Draft.Lint);
        Terminal.ReplaceValues(Draft.Terminal);
        OnPropertyChanged(nameof(Draft));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private CancellationTokenSource BeginOperation(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        var operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        _operationCancellation = operation;
        return operation;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _themePreview?.Revert();
    }
}
