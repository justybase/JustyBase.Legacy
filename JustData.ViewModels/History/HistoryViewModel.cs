using System.Collections.ObjectModel;
using JustData.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using JustData.Application.History;

namespace JustData.ViewModels.History;

public sealed class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryStore _store;
    private readonly IUiDispatcher? _uiDispatcher;
    private IReadOnlyList<HistoryEntry> _allEntries = [];
    private string _searchText = "";
    private bool _isLoaded;
    private bool _isBusy;
    private string? _errorMessage;
    private HistoryEntry? _selectedEntry;

    public HistoryViewModel(IHistoryStore store, IUiDispatcher? uiDispatcher = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _uiDispatcher = uiDispatcher;
    }

    public ObservableCollection<HistoryEntry> FilteredEntries { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetProperty(ref _isLoaded, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public HistoryEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public async Task LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        }, cancellationToken);
        try
        {
            IReadOnlyList<HistoryEntry> entries = await _store
                .LoadAsync(filePath, cancellationToken)
                .ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                _allEntries = entries;
                IsLoaded = true;
                ApplyFilter();
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
                ErrorMessage = $"Unable to load history: {ex.Message}";
                _allEntries = [];
                ApplyFilter();
            }, CancellationToken.None);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(
                () => IsBusy = false,
                CancellationToken.None);
        }
    }

    public void Filter(string? searchText)
    {
        SearchText = searchText ?? string.Empty;
    }

    private void ApplyFilter()
    {
        FilteredEntries.Clear();

        var filtered = string.IsNullOrWhiteSpace(_searchText)
            ? _allEntries
            : _allEntries.Where(e =>
                e.Sql.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                e.Database.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                e.ConnectionName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
              .ToList();

        foreach (var entry in filtered)
        {
            FilteredEntries.Add(entry);
        }
    }
}
