using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Schema;
using JustData.ViewModels;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Explorer;

/// <summary>Application state for the database explorer; TreeNode and menu details remain in the UI adapter.</summary>
public sealed class DatabaseExplorerViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan ChildBatchDelay = TimeSpan.FromMilliseconds(25);
    private readonly ISchemaRepository _schemaRepository;
    private readonly ISchemaDdlService _ddlService;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly IExplorerBatchScheduler _batchScheduler;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _operationCancellation;
    private long _operationVersion;
    private ExplorerNodeViewModel? _selectedNode;
    private string _filter = string.Empty;
    private string? _connectionName;
    private string _status = string.Empty;
    private string? _lastDdl;
    private bool _isBusy;
    private bool _disposed;

    public DatabaseExplorerViewModel(
        ISchemaRepository schemaRepository,
        ISchemaDdlService ddlService,
        IUiDispatcher? uiDispatcher = null,
        IExplorerBatchScheduler? batchScheduler = null)
    {
        _schemaRepository = schemaRepository ?? throw new ArgumentNullException(nameof(schemaRepository));
        _ddlService = ddlService ?? throw new ArgumentNullException(nameof(ddlService));
        _uiDispatcher = uiDispatcher;
        _batchScheduler = batchScheduler ?? DefaultExplorerBatchScheduler.Instance;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        ExpandCommand = new AsyncRelayCommand<ExplorerNodeViewModel?>(ExpandAsync, node => node is not null && !node.IsLoading && node.HasChildren);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        SwitchConnectionCommand = new AsyncRelayCommand<string?>(SwitchConnectionAsync, connection => !IsBusy && !string.IsNullOrWhiteSpace(connection));
        DdlCommand = new AsyncRelayCommand<ExplorerNodeViewModel?>(LoadDdlAsync, node => node is not null && !IsBusy);
    }

    public ObservableCollection<ExplorerNodeViewModel> RootNodes { get; } = [];
    public ObservableCollection<ExplorerNodeViewModel> SearchResults { get; } = [];
    public ExplorerNodeViewModel? SelectedNode { get => _selectedNode; set => SetProperty(ref _selectedNode, value); }
    public string Filter { get => _filter; set => SetProperty(ref _filter, value ?? string.Empty); }
    public string? ConnectionName { get => _connectionName; set => SetProperty(ref _connectionName, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string? LastDdl { get => _lastDdl; private set => SetProperty(ref _lastDdl, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RefreshCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
            SearchCommand.NotifyCanExecuteChanged();
            SwitchConnectionCommand.NotifyCanExecuteChanged();
            ExpandCommand.NotifyCanExecuteChanged();
            DdlCommand.NotifyCanExecuteChanged();
        }
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<ExplorerNodeViewModel?> ExpandCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand<string?> SwitchConnectionCommand { get; }
    public IAsyncRelayCommand<ExplorerNodeViewModel?> DdlCommand { get; }

    public Task InitializeAsync(string? connectionName = null, bool refresh = true, CancellationToken cancellationToken = default)
    {
        ConnectionName = connectionName;
        return refresh ? RefreshAsync(cancellationToken) : LoadRootsAsync(cancellationToken);
    }

    private async Task LoadRootsAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        IReadOnlyList<SchemaNode> roots = await _schemaRepository.GetRootsAsync(ConnectionName, cancellationToken).ConfigureAwait(false);
        await OnUiAsync(() =>
        {
            RootNodes.Clear();
            foreach (var root in roots) RootNodes.Add(new ExplorerNodeViewModel(root));
            Status = $"{RootNodes.Count} node(s)";
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await RunOperationAsync(async token =>
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => Status = "Refreshing schema…",
                token);
            await _schemaRepository.RefreshAsync(ConnectionName, token).ConfigureAwait(false);
            IReadOnlyList<SchemaNode> roots = await _schemaRepository.GetRootsAsync(ConnectionName, token).ConfigureAwait(false);
            await OnUiAsync(() =>
            {
                RootNodes.Clear();
                foreach (var root in roots)
                    RootNodes.Add(new ExplorerNodeViewModel(root));
                SelectedNode = null;
                SearchResults.Clear();
                Status = $"{RootNodes.Count} node(s)";
            }, token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExpandAsync(ExplorerNodeViewModel? node, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (node is null || !node.HasChildren || node.ChildrenLoaded)
        {
            if (node is not null) node.IsExpanded = true;
            return;
        }

        await RunOperationAsync(async token =>
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => node.IsLoading = true,
                token);
            try
            {
                var children = await _schemaRepository.GetChildrenAsync(node.Model, token).ConfigureAwait(false);
                if (children.Count == 0 && node.Kind is SchemaNodeKind.Connection or SchemaNodeKind.Database or SchemaNodeKind.Schema)
                {
                    // Providers such as Netezza create their session before their
                    // object catalog is downloaded. Expanding an empty level is the
                    // user's explicit request to make that catalog available.
                    await _uiDispatcher.InvokeOnUiAsync(
                        () => Status = "Refreshing schema…",
                        token);
                    await _schemaRepository.RefreshAsync(node.Path.Connection, token).ConfigureAwait(false);
                    children = await _schemaRepository.GetChildrenAsync(node.Model, token).ConfigureAwait(false);
                }

                await _uiDispatcher.InvokeOnUiAsync(
                    () => Status = $"Loading {children.Count} schema object(s)…",
                    token);
                await OnUiAsync(() =>
                {
                    node.BeginChildrenLoad(children);
                    node.IsExpanded = true;
                    node.AppendNextChildrenBatch(ExplorerNodeViewModel.InitialChildBatchSize);
                }, token).ConfigureAwait(false);

                while (node.HasPendingChildren)
                {
                    await _batchScheduler.DelayAsync(token).ConfigureAwait(false);
                    await OnUiAsync(() => node.AppendNextChildrenBatch(ExplorerNodeViewModel.InitialChildBatchSize), token)
                        .ConfigureAwait(false);
                }

                await OnUiAsync(node.CompleteChildrenLoad, token).ConfigureAwait(false);
                await _uiDispatcher.InvokeOnUiAsync(
                    () => Status = $"{children.Count} schema object(s)",
                    token);
            }
            finally
            {
                await OnUiAsync(() =>
                {
                    node.IsLoading = false;
                    ExpandCommand.NotifyCanExecuteChanged();
                }, CancellationToken.None).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await RunOperationAsync(async token =>
        {
            var result = string.IsNullOrWhiteSpace(Filter)
                ? new SchemaSearchResult([])
                : await _schemaRepository.SearchAsync(new(Filter, ConnectionName, IncludeColumns: true, MaxResults: 1_000), token).ConfigureAwait(false);
            await OnUiAsync(() =>
            {
                SearchResults.Clear();
                foreach (var match in result.Nodes)
                    SearchResults.Add(new ExplorerNodeViewModel(match));
                Status = result.IsTruncated ? $"{SearchResults.Count} result(s), results truncated" : $"{SearchResults.Count} result(s)";
            }, token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Cancel() => _operationCancellation?.Cancel();

    public Task SwitchConnectionAsync(string? connectionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName)) return Task.CompletedTask;
        ConnectionName = connectionName;
        return RefreshAsync(cancellationToken);
    }

    public async Task LoadDdlAsync(ExplorerNodeViewModel? node, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (node is null) return;
        await RunOperationAsync(async token =>
        {
            string? ddl = await _ddlService
                .GetDdlAsync(new(node.Model, SchemaDdlKind.Create), token)
                .ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(
                () => LastDdl = ddl,
                token);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunOperationAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        long operationVersion = Interlocked.Increment(ref _operationVersion);
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        _operationCancellation = linked;
        await _uiDispatcher.InvokeOnUiAsync(
            () => IsBusy = true,
            cancellationToken);
        try { await operation(linked.Token).ConfigureAwait(false); }
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

    private Task OnUiAsync(Action action, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return _uiDispatcher.InvokeOnUiAsync(action, token);
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

    private sealed class DefaultExplorerBatchScheduler : IExplorerBatchScheduler
    {
        public static DefaultExplorerBatchScheduler Instance { get; } = new();

        public Task DelayAsync(CancellationToken cancellationToken = default) =>
            Task.Delay(ChildBatchDelay, cancellationToken);
    }
}
