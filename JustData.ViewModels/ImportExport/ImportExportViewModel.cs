using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using JustData.Application;
using JustData.Application.ImportExport;
using JustData.Application.Sql;

namespace JustData.ViewModels.ImportExport;

/// <summary>Transient state for one import or export operation.</summary>
public sealed class ImportExportViewModel : ObservableObject, IDisposable
{
    private readonly IImportUseCase? _importUseCase;
    private readonly IResultExportUseCase? _exportUseCase;
    private readonly IUiDispatcher? _uiDispatcher;
    private CancellationTokenSource? _operationCancellation;
    private bool _isRunning;
    private string _stage = string.Empty;
    private string? _errorMessage;
    private ImportResult? _importResult;
    private ImportRequest? _currentImportRequest;
    private ExportRequest? _currentExportRequest;
    private long _rowsRead;
    private long _rowsWritten;
    private int _operationActive;
    private bool _disposed;

    public ImportExportViewModel(
        IImportUseCase? importUseCase = null,
        IResultExportUseCase? exportUseCase = null,
        IUiDispatcher? uiDispatcher = null)
    {
        _importUseCase = importUseCase;
        _exportUseCase = exportUseCase;
        _uiDispatcher = uiDispatcher;
        ImportCommand = new AsyncRelayCommand(
            () => ImportAsync(CurrentImportRequest!),
            () => CurrentImportRequest is not null && !IsRunning && _importUseCase is not null);
        ExportCommand = new AsyncRelayCommand(
            () => ExportAsync(CurrentExportRequest!),
            () => CurrentExportRequest is not null && !IsRunning && _exportUseCase is not null);
        CancelCommand = new AsyncRelayCommand(CancelAsync, () => IsRunning);
    }

    public ImportRequest? CurrentImportRequest
    {
        get => _currentImportRequest;
        set
        {
            if (SetProperty(ref _currentImportRequest, value))
                ImportCommand.NotifyCanExecuteChanged();
        }
    }

    public ExportRequest? CurrentExportRequest
    {
        get => _currentExportRequest;
        set
        {
            if (SetProperty(ref _currentExportRequest, value))
                ExportCommand.NotifyCanExecuteChanged();
        }
    }

    public IAsyncRelayCommand ImportCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                ImportCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                CancelCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Stage
    {
        get => _stage;
        private set => SetProperty(ref _stage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ImportResult? ImportResult
    {
        get => _importResult;
        private set => SetProperty(ref _importResult, value);
    }

    public long RowsRead
    {
        get => _rowsRead;
        private set => SetProperty(ref _rowsRead, value);
    }

    public long RowsWritten
    {
        get => _rowsWritten;
        private set => SetProperty(ref _rowsWritten, value);
    }

    public event Action<ImportProgress>? ImportProgressReceived;
    public event Action<ExportProgress>? ExportProgressReceived;

    public async Task<ImportResult?> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_importUseCase is null)
            throw new InvalidOperationException("No import use case is configured.");
        CancellationTokenSource operationCancellation = await BeginOperationAsync(cancellationToken);
        try
        {
            await foreach (ImportProgress progress in _importUseCase
                .ImportAsync(request, operationCancellation.Token)
                .WithCancellation(operationCancellation.Token)
                )
            {
                await _uiDispatcher.InvokeOnUiAsync(() =>
                {
                    Apply(progress);
                    ImportProgressReceived?.Invoke(progress);
                }, operationCancellation.Token);
            }
            return ImportResult;
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            await SetErrorAsync("Import cancelled.");
            return ImportResult;
        }
        catch (Exception exception)
        {
            await SetErrorAsync(SqlSensitiveDataRedactor.Redact(exception.Message));
            return ImportResult;
        }
        finally
        {
            await EndOperationAsync(operationCancellation);
        }
    }

    public async Task ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_exportUseCase is null)
            throw new InvalidOperationException("No result export use case is configured.");
        CancellationTokenSource operationCancellation = await BeginOperationAsync(cancellationToken);
        try
        {
            await foreach (ExportProgress progress in _exportUseCase
                .ExportAsync(request, operationCancellation.Token)
                .WithCancellation(operationCancellation.Token)
                )
            {
                await _uiDispatcher.InvokeOnUiAsync(() =>
                {
                    Stage = progress.Stage;
                    RowsWritten = progress.RowsWritten;
                    ErrorMessage = progress.ErrorMessage;
                    ExportProgressReceived?.Invoke(progress);
                }, operationCancellation.Token);
            }
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            await SetErrorAsync("Export cancelled.");
        }
        catch (Exception exception)
        {
            await SetErrorAsync(SqlSensitiveDataRedactor.Redact(exception.Message));
        }
        finally
        {
            await EndOperationAsync(operationCancellation);
        }
    }

    public Task CancelAsync()
    {
        _operationCancellation?.Cancel();
        return Task.CompletedTask;
    }

    private async Task<CancellationTokenSource> BeginOperationAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _operationActive, 1, 0) != 0)
            throw new InvalidOperationException("An import or export is already running.");
        try
        {
            CancellationTokenSource operationCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _operationCancellation = operationCancellation;
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRunning = true;
                Stage = string.Empty;
                ErrorMessage = null;
                ImportResult = null;
                RowsRead = 0;
                RowsWritten = 0;
            }, CancellationToken.None);
            return operationCancellation;
        }
        catch
        {
            Interlocked.Exchange(ref _operationActive, 0);
            throw;
        }
    }

    private void Apply(ImportProgress progress)
    {
        Stage = progress.Stage;
        RowsRead = progress.RowsRead;
        RowsWritten = progress.RowsImported;
        ErrorMessage = progress.ErrorMessage;
        if (progress.Result is not null)
            ImportResult = progress.Result;
    }

    private async Task EndOperationAsync(CancellationTokenSource cancellation)
    {
        if (ReferenceEquals(_operationCancellation, cancellation))
            _operationCancellation = null;
        cancellation.Dispose();
        Interlocked.Exchange(ref _operationActive, 0);
        await _uiDispatcher.InvokeOnUiAsync(
            () => IsRunning = false,
            CancellationToken.None);
    }

    private Task SetErrorAsync(string message) => _uiDispatcher.InvokeOnUiAsync(
        () => ErrorMessage = message,
        CancellationToken.None);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        CancellationTokenSource? cancellation = _operationCancellation;
        cancellation?.Cancel();
        if (Volatile.Read(ref _operationActive) == 0)
        {
            cancellation?.Dispose();
            _operationCancellation = null;
        }
        ImportProgressReceived = null;
        ExportProgressReceived = null;
    }
}
