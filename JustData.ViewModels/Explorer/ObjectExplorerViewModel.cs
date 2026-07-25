using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Schema;
using JustData.ViewModels;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Explorer;

/// <summary>SQL object references backed by the same schema repository as the database explorer.</summary>
public sealed class ObjectExplorerViewModel : ObservableObject, IDisposable
{
    private readonly ISchemaRepository _schemaRepository;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _operationCancellation;
    private long _operationVersion;
    private string _sqlText = string.Empty;
    private bool _isBusy;
    private string _status = string.Empty;
    private SchemaReference? _selectedReference;
    private bool _disposed;

    public ObjectExplorerViewModel(ISchemaRepository schemaRepository, IUiDispatcher? uiDispatcher = null)
    {
        _schemaRepository = schemaRepository ?? throw new ArgumentNullException(nameof(schemaRepository));
        _uiDispatcher = uiDispatcher;
        RefreshCommand = new AsyncRelayCommand(() => RefreshAsync(), () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
    }

    public ObservableCollection<SchemaReference> References { get; } = [];
    public string SqlText { get => _sqlText; set => SetProperty(ref _sqlText, value ?? string.Empty); }
    public SchemaReference? SelectedReference { get => _selectedReference; set => SetProperty(ref _selectedReference, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RefreshCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public async Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        long operationVersion = Interlocked.Increment(ref _operationVersion);
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        _operationCancellation = linked;
        await _uiDispatcher.InvokeOnUiAsync(
            () => IsBusy = true,
            cancellationToken);
        try
        {
            var references = await _schemaRepository.GetReferencesAsync(SqlText, connectionName, linked.Token).ConfigureAwait(false);
            await UpdateReferencesAsync(references, linked.Token).ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(
                () => Status = $"{References.Count} reference(s)",
                linked.Token);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            if (IsCurrentOperation(linked, operationVersion))
            {
                await _uiDispatcher.InvokeOnUiAsync(
                    () => Status = "Cancelled",
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            if (IsCurrentOperation(linked, operationVersion))
            {
                await _uiDispatcher.InvokeOnUiAsync(
                    () => Status = ex.Message,
                    CancellationToken.None);
            }
            throw;
        }
        finally
        {
            if (IsCurrentOperation(linked, operationVersion))
            {
                _operationCancellation = null;
                await _uiDispatcher.InvokeOnUiAsync(
                    () => IsBusy = false,
                    CancellationToken.None);
            }
        }
    }

    public void Cancel() => _operationCancellation?.Cancel();

    private Task UpdateReferencesAsync(IReadOnlyList<SchemaReference> references, CancellationToken cancellationToken)
    {
        void Update()
        {
            References.Clear();
            foreach (var reference in references) References.Add(reference);
        }

        if (_uiDispatcher is null || _uiDispatcher.CheckAccess())
        {
            Update();
            return Task.CompletedTask;
        }

        return _uiDispatcher.InvokeAsync(Update, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private bool IsCurrentOperation(CancellationTokenSource operation, long version) =>
        version == Volatile.Read(ref _operationVersion)
        && ReferenceEquals(_operationCancellation, operation);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
