using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application.Variables;

namespace JustData.ViewModels.Variables;

public sealed class VariablesViewModel : ObservableObject, IDisposable
{
    private readonly ISessionVariableStore _store;
    private readonly List<VariableEntry> _entries = [];
    private bool _disposed;
    private string _documentKey = string.Empty;

    public VariablesViewModel(ISessionVariableStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ClearGlobalsCommand = new RelayCommand(ClearGlobals);
        InsertVariableCommand = new RelayCommand<VariableEntry?>(InsertVariable);
        _store.Changed += Store_Changed;
    }

    public IReadOnlyList<VariableEntry> Entries => _entries;

    /// <summary>
    /// Raised once after <see cref="Refresh"/> replaces the entries. Replaces
    /// <c>BindingList&lt;T&gt;.ListChanged</c>, which relies on reflection
    /// (PropertyDescriptors) and is not trimming/Native AOT safe.
    /// </summary>
    public event EventHandler? EntriesChanged;

    public string DocumentKey
    {
        get => _documentKey;
        private set => SetProperty(ref _documentKey, value);
    }

    public IRelayCommand ClearGlobalsCommand { get; }

    public IRelayCommand<VariableEntry?> InsertVariableCommand { get; }

    public event Action<string>? InsertVariableRequested;

    public void Refresh(string? documentKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DocumentKey = documentKey ?? string.Empty;
        _entries.Clear();

        if (!string.IsNullOrWhiteSpace(DocumentKey))
        {
            foreach (KeyValuePair<string, string> item in _store.GetSessionVariables(DocumentKey))
            {
                _entries.Add(new VariableEntry(item.Key, item.Value, IsSession: true));
            }
        }

        foreach (KeyValuePair<string, string> item in _store.GlobalVariables)
        {
            _entries.Add(new VariableEntry(item.Key, item.Value, IsSession: false));
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _store.Changed -= Store_Changed;
        InsertVariableRequested = null;
        EntriesChanged = null;
        _entries.Clear();
    }

    private void Store_Changed(object? sender, EventArgs e)
    {
        if (!_disposed)
        {
            Refresh(DocumentKey);
        }
    }

    private void ClearGlobals()
    {
        _store.ClearGlobalVariables();
        Refresh(DocumentKey);
    }

    private void InsertVariable(VariableEntry? entry)
    {
        if (entry is not null && !string.IsNullOrEmpty(entry.Name))
        {
            InsertVariableRequested?.Invoke(entry.Name);
        }
    }
}
