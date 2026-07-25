using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Sql;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Sql;

/// <summary>
/// Document-owned execution state. It observes event streams but deliberately
/// forwards row batches to the presentation adapter instead of retaining them.
/// </summary>
public sealed class SqlExecutionViewModel : ObservableObject, IDisposable
{
    private readonly EditorDocumentId _documentId;
    private readonly ISqlExecutionUseCase _useCase;
    private readonly Func<SqlExecutionMode, SqlOutputMode, string?, SqlExecutionRequest> _requestFactory;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly object _sync = new();
    private CancellationTokenSource? _activeCancellation;
    private int _runActive;
    private SqlExecutionState _state = SqlExecutionState.Idle;
    private SqlExecutionOutcome? _lastOutcome;
    private double? _progress;
    private long _rowCount;
    private long? _affectedRows;
    private bool _isTruncated;
    private ResultSetDescriptor? _selectedResultSet;
    private string? _outputPath;
    private bool _disposed;

    public SqlExecutionViewModel(
        EditorDocumentId documentId,
        Func<SqlExecutionMode, SqlOutputMode, string?, SqlExecutionRequest> requestFactory,
        ISqlExecutionUseCase? useCase = null,
        IUiDispatcher? uiDispatcher = null)
    {
        _documentId = documentId;
        _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
        _useCase = useCase ?? UnavailableSqlExecutionUseCase.Instance;
        _uiDispatcher = uiDispatcher;

        RunCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.Selection, SqlOutputMode.Grid),
            () => !IsBusy);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsBusy);
        RunToCursorCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.RunToCursor, SqlOutputMode.Grid),
            () => !IsBusy);
        SingleBatchCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.SingleBatch, SqlOutputMode.Grid),
            () => !IsBusy);
        ScriptCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.Script, SqlOutputMode.Grid),
            () => !IsBusy);
        CsvCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.Selection, SqlOutputMode.Csv),
            () => !IsBusy);
        XlsxCommand = new AsyncRelayCommand(
            () => ExecuteCommandAsync(SqlExecutionMode.Selection, SqlOutputMode.Xlsx),
            () => !IsBusy);
        PinResultCommand = new RelayCommand<string?>(PinResult);
        UnpinResultCommand = new RelayCommand<string?>(UnpinResult);
        ClearResultCommand = new RelayCommand(ClearResults, () => !IsBusy);
    }

    public SqlExecutionViewModel(EditorDocumentId documentId, ISqlExecutionUseCase useCase)
        : this(
            documentId,
            (mode, outputMode, outputPath) => new SqlExecutionRequest(documentId, string.Empty)
            {
                Mode = mode,
                OutputMode = outputMode,
                OutputPath = outputPath
            },
            useCase,
            null)
    {
    }

    public SqlExecutionState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                RunCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
                RunToCursorCommand.NotifyCanExecuteChanged();
                SingleBatchCommand.NotifyCanExecuteChanged();
                ScriptCommand.NotifyCanExecuteChanged();
                CsvCommand.NotifyCanExecuteChanged();
                XlsxCommand.NotifyCanExecuteChanged();
                ClearResultCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public SqlExecutionOutcome? LastOutcome
    {
        get => _lastOutcome;
        private set => SetProperty(ref _lastOutcome, value);
    }

    public bool IsBusy => State is SqlExecutionState.Running or SqlExecutionState.Cancelling;

    public double? Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public long RowCount
    {
        get => _rowCount;
        private set => SetProperty(ref _rowCount, value);
    }

    public long? AffectedRows
    {
        get => _affectedRows;
        private set => SetProperty(ref _affectedRows, value);
    }

    public bool IsTruncated
    {
        get => _isTruncated;
        private set => SetProperty(ref _isTruncated, value);
    }

    public string? OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public ObservableCollection<SqlLogEntry> Logs { get; } = [];
    public ObservableCollection<SqlDiagnostic> Diagnostics { get; } = [];
    public ObservableCollection<ResultSetDescriptor> ResultSets { get; } = [];

    public ResultSetDescriptor? SelectedResultSet
    {
        get => _selectedResultSet;
        private set => SetProperty(ref _selectedResultSet, value);
    }

    public IAsyncRelayCommand RunCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand RunToCursorCommand { get; }
    public IAsyncRelayCommand SingleBatchCommand { get; }
    public IAsyncRelayCommand ScriptCommand { get; }
    public IAsyncRelayCommand CsvCommand { get; }
    public IAsyncRelayCommand XlsxCommand { get; }
    public IRelayCommand<string?> PinResultCommand { get; }
    public IRelayCommand<string?> UnpinResultCommand { get; }
    public IRelayCommand ClearResultCommand { get; }

    /// <summary>Raised synchronously for every event, including row batches.</summary>
    public event Action<SqlExecutionEvent>? EventReceived;

    public async Task<SqlExecutionOutcome> RunAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (request.DocumentId != _documentId)
            throw new ArgumentException("The execution request belongs to another document.", nameof(request));
        if (Interlocked.Exchange(ref _runActive, 1) != 0)
            throw new InvalidOperationException("Only one SQL execution may run for a document.");

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_sync)
            _activeCancellation = linkedCancellation;

        await _uiDispatcher.InvokeOnUiAsync(BeginRun, linkedCancellation.Token);
        SqlExecutionOutcome outcome = SqlExecutionOutcome.Failed;
        bool completionReceived = false;
        try
        {
            await foreach (SqlExecutionEvent executionEvent in _useCase
                .ExecuteAsync(request, linkedCancellation.Token)
                .WithCancellation(linkedCancellation.Token)
                )
            {
                if (executionEvent.DocumentId != _documentId)
                    continue;

                await _uiDispatcher.InvokeOnUiAsync(
                    () => Apply(executionEvent),
                    linkedCancellation.Token);
                if (executionEvent.Kind == SqlExecutionEventKind.Completed
                    && executionEvent.Outcome is { } completedOutcome)
                {
                    outcome = completedOutcome;
                    completionReceived = true;
                }
            }

            if (linkedCancellation.IsCancellationRequested)
                outcome = SqlExecutionOutcome.Cancelled;
            else if (!completionReceived)
            {
                outcome = SqlExecutionOutcome.Failed;
                await _uiDispatcher.InvokeOnUiAsync(
                    () => Apply(new SqlExecutionEvent(SqlExecutionEventKind.Diagnostic, _documentId)
                    {
                        Diagnostic = new SqlDiagnostic(
                            SqlDiagnosticSeverity.Error,
                            "SQL execution ended without a completion event.",
                            Code: "MissingCompletion",
                            Source: "execution")
                    }),
                    CancellationToken.None);
            }

            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                LastOutcome = outcome;
                State = outcome == SqlExecutionOutcome.Failed
                    ? SqlExecutionState.Failed
                    : outcome is SqlExecutionOutcome.Cancelled or SqlExecutionOutcome.Blocked
                        ? SqlExecutionState.Idle
                        : SqlExecutionState.Succeeded;
            }, CancellationToken.None);
            return outcome;
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            const SqlExecutionOutcome cancelledOutcome = SqlExecutionOutcome.Cancelled;
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                LastOutcome = cancelledOutcome;
                State = SqlExecutionState.Idle;
            }, CancellationToken.None);
            return cancelledOutcome;
        }
        catch (Exception exception)
        {
            // Keep provider exception details out of the VM state. Adapters
            // can publish a redacted diagnostic when a user-facing message is
            // appropriate.
            const SqlExecutionOutcome failedOutcome = SqlExecutionOutcome.Failed;
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                LastOutcome = failedOutcome;
                State = SqlExecutionState.Failed;
                Apply(new SqlExecutionEvent(SqlExecutionEventKind.Diagnostic, _documentId)
                {
                    Diagnostic = new SqlDiagnostic(
                        SqlDiagnosticSeverity.Error,
                        SqlSensitiveDataRedactor.Redact(exception.Message),
                        Code: exception.GetType().Name,
                        Source: "execution")
                });
            }, CancellationToken.None);
            return failedOutcome;
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_activeCancellation, linkedCancellation))
                    _activeCancellation = null;
            }

            Interlocked.Exchange(ref _runActive, 0);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                StopCommand.NotifyCanExecuteChanged();
                RunCommand.NotifyCanExecuteChanged();
            }, CancellationToken.None);
        }
    }

    public Task<SqlExecutionOutcome> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default) => RunAsync(request, cancellationToken);

    public Task<bool> StopAsync()
    {
        ThrowIfDisposed();
        return StopCoreAsync();
    }

    private async Task<bool> StopCoreAsync()
    {
        CancellationTokenSource? cancellation;
        lock (_sync)
            cancellation = _activeCancellation;

        if (cancellation is null || !IsBusy)
            return false;

        await _uiDispatcher.InvokeOnUiAsync(
            () => State = SqlExecutionState.Cancelling,
            CancellationToken.None);
        cancellation.Cancel();
        return true;
    }

    public Task<bool> CancelAsync() => StopAsync();

    public void SelectResult(string? resultSetId)
    {
        ThrowIfDisposed();
        SelectedResultSet = string.IsNullOrEmpty(resultSetId)
            ? null
            : ResultSets.FirstOrDefault(item => item.ResultSetId == resultSetId);
    }

    public void PinResult(string? resultSetId)
    {
        ThrowIfDisposed();
        UpdatePinState(resultSetId, isPinned: true);
    }

    public void UnpinResult(string? resultSetId)
    {
        ThrowIfDisposed();
        UpdatePinState(resultSetId, isPinned: false);
    }

    public void ClearResults()
    {
        ThrowIfDisposed();
        for (int index = ResultSets.Count - 1; index >= 0; index--)
        {
            if (!ResultSets[index].IsPinned)
                ResultSets.RemoveAt(index);
        }

        if (SelectedResultSet is not null && !ResultSets.Contains(SelectedResultSet))
            SelectedResultSet = ResultSets.LastOrDefault();
    }

    public void RemoveResult(string? resultSetId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(resultSetId))
            return;

        ResultSetDescriptor? result = ResultSets.FirstOrDefault(
            item => item.ResultSetId == resultSetId);
        if (result is null)
            return;

        bool wasSelected = ReferenceEquals(SelectedResultSet, result)
            || SelectedResultSet?.ResultSetId == resultSetId;
        ResultSets.Remove(result);
        if (wasSelected)
            SelectedResultSet = ResultSets.LastOrDefault();
    }

    private async Task ExecuteCommandAsync(SqlExecutionMode mode, SqlOutputMode outputMode)
    {
        SqlExecutionRequest request = _requestFactory(mode, outputMode, OutputPath);
        await RunAsync(request);
    }

    private void BeginRun()
    {
        State = SqlExecutionState.Running;
        LastOutcome = null;
        Progress = 0;
        RowCount = 0;
        AffectedRows = null;
        IsTruncated = false;
        Logs.Clear();
        Diagnostics.Clear();
        ClearResults();
    }

    private void Apply(SqlExecutionEvent executionEvent)
    {
        switch (executionEvent.Kind)
        {
            case SqlExecutionEventKind.Started:
                Progress = 0;
                break;
            case SqlExecutionEventKind.StatementStarted:
                if (executionEvent.StatementCount > 0 && executionEvent.StatementIndex >= 0)
                    Progress = (double)executionEvent.StatementIndex / executionEvent.StatementCount;
                break;
            case SqlExecutionEventKind.Log:
                if (executionEvent.Log is not null)
                    Logs.Add(executionEvent.Log);
                else if (!string.IsNullOrWhiteSpace(executionEvent.Message))
                    Logs.Add(new SqlLogEntry(DateTimeOffset.UtcNow, SqlLogLevel.Information, executionEvent.Message));
                break;
            case SqlExecutionEventKind.Diagnostic:
                if (executionEvent.Diagnostic is not null)
                    Diagnostics.Add(executionEvent.Diagnostic);
                break;
            case SqlExecutionEventKind.AffectedRows:
                AffectedRows = executionEvent.AffectedRows;
                break;
            case SqlExecutionEventKind.ResultSet:
                if (executionEvent.ResultSet is not null)
                    AddOrUpdateResult(executionEvent.ResultSet);
                break;
            case SqlExecutionEventKind.Rows:
                RowCount += executionEvent.Rows?.Count ?? executionEvent.RowCount;
                break;
            case SqlExecutionEventKind.Truncated:
                IsTruncated = true;
                Progress = 1;
                break;
            case SqlExecutionEventKind.Completed:
                Progress = 1;
                break;
        }

        EventReceived?.Invoke(executionEvent);
    }

    private void AddOrUpdateResult(ResultSetDescriptor descriptor)
    {
        int index = -1;
        for (int candidate = 0; candidate < ResultSets.Count; candidate++)
        {
            if (ResultSets[candidate].ResultSetId == descriptor.ResultSetId)
            {
                index = candidate;
                break;
            }
        }

        if (index >= 0)
        {
            bool isPinned = ResultSets[index].IsPinned || descriptor.IsPinned;
            ResultSets[index] = descriptor with { IsPinned = isPinned };
        }
        else
        {
            ResultSets.Add(descriptor);
        }

        SelectedResultSet ??= index >= 0 ? ResultSets[index] : descriptor;
    }

    private void UpdatePinState(string? resultSetId, bool isPinned)
    {
        if (string.IsNullOrWhiteSpace(resultSetId))
            return;

        int index = -1;
        for (int candidate = 0; candidate < ResultSets.Count; candidate++)
        {
            if (ResultSets[candidate].ResultSetId == resultSetId)
            {
                index = candidate;
                break;
            }
        }
        if (index < 0 || index >= ResultSets.Count)
            return;

        ResultSets[index] = ResultSets[index] with { IsPinned = isPinned };
        if (SelectedResultSet?.ResultSetId == resultSetId)
            SelectedResultSet = ResultSets[index];
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        lock (_sync)
            _activeCancellation?.Cancel();
        EventReceived = null;
    }

    private sealed class UnavailableSqlExecutionUseCase : ISqlExecutionUseCase
    {
        public static UnavailableSqlExecutionUseCase Instance { get; } = new();

        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Blocked,
                "No SQL execution adapter is configured.");
            await Task.CompletedTask;
        }
    }
}
