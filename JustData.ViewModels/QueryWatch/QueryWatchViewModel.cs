using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.QueryWatch;

namespace JustData.ViewModels.QueryWatch;

public sealed class QueryWatchViewModel : ViewModelBase
{
    private readonly IQueryWatchService _service;
    private readonly Func<QueryWatchContext> _contextFactory;
    private readonly IUiDispatcher? _uiDispatcher;
    private bool _isBusy;
    private bool _autoRefreshEnabled;
    private string? _errorMessage;
    private DateTime? _lastRefreshed;
    private string _connectionLabel = "";
    private IReadOnlyList<string> _columnNames = [];

    public QueryWatchViewModel(
        IQueryWatchService service,
        Func<QueryWatchContext> contextFactory,
        IUiDispatcher? uiDispatcher = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _uiDispatcher = uiDispatcher;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        DropSessionCommand = new AsyncRelayCommand<QueryWatchRow?>(DropSessionAsync, CanDropSession);
    }

    public ObservableCollection<QueryWatchRow> Rows { get; } = [];

    public IReadOnlyList<string> ColumnNames
    {
        get => _columnNames;
        private set => SetProperty(ref _columnNames, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand<QueryWatchRow?> DropSessionCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommand.NotifyCanExecuteChanged();
                DropSessionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set => SetProperty(ref _autoRefreshEnabled, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public DateTime? LastRefreshed
    {
        get => _lastRefreshed;
        private set => SetProperty(ref _lastRefreshed, value);
    }

    public string ConnectionLabel
    {
        get => _connectionLabel;
        private set => SetProperty(ref _connectionLabel, value);
    }

    /// <summary>
    /// Returns the drop SQL for a row when available. Confirmation stays in the UI layer.
    /// </summary>
    public string? RequestDropSession(QueryWatchRow? row) =>
        row is { CanDrop: true, DropSessionSql: { Length: > 0 } sql } ? sql : null;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        QueryWatchContext context = _contextFactory();
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            ConnectionLabel = FormatConnectionLabel(context);
        }, cancellationToken);

        try
        {
            IReadOnlyList<QueryWatchRow> rows = await _service
                .RefreshAsync(context, cancellationToken)
                .ConfigureAwait(false);

            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                Rows.Clear();
                foreach (QueryWatchRow row in rows)
                {
                    Rows.Add(row);
                }

                ColumnNames = rows.Count > 0
                    ? rows[0].Values.Keys.ToList()
                    : [];
                LastRefreshed = DateTime.Now;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                ErrorMessage = $"Unable to refresh active queries: {ex.Message}";
                Rows.Clear();
            }, CancellationToken.None);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => IsBusy = false,
                CancellationToken.None);
        }
    }

    public async Task DropSessionAsync(QueryWatchRow? row, CancellationToken cancellationToken = default)
    {
        string? dropSql = RequestDropSession(row);
        if (dropSql is null || IsBusy)
        {
            return;
        }

        QueryWatchContext context = _contextFactory();
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        }, cancellationToken);

        try
        {
            await _service.DropSessionAsync(dropSql, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                ErrorMessage = $"Unable to drop session: {ex.Message}";
            }, CancellationToken.None);
            return;
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => IsBusy = false,
                CancellationToken.None);
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    private bool CanDropSession(QueryWatchRow? row) => !IsBusy && row is { CanDrop: true };

    private static string FormatConnectionLabel(QueryWatchContext context)
    {
        if (string.IsNullOrWhiteSpace(context.DatabaseName))
        {
            return context.ConnectionName;
        }

        return $"{context.ConnectionName} · {context.DatabaseName}";
    }
}
