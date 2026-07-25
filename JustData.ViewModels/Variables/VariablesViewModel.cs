using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application.Variables;
using System.ComponentModel;

namespace JustData.ViewModels.Variables;

public sealed class VariablesViewModel : ObservableObject, IDisposable
{
    private readonly ISessionVariableStore _store;
    private bool _disposed;
    private string _documentKey = string.Empty;

    public VariablesViewModel(ISessionVariableStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Entries = new BindingList<VariableEntry>();
        ClearGlobalsCommand = new RelayCommand(ClearGlobals);
        InsertVariableCommand = new RelayCommand<VariableEntry?>(InsertVariable);
        _store.Changed += Store_Changed;
    }

    public BindingList<VariableEntry> Entries { get; }

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
        Entries.RaiseListChangedEvents = false;
        try
        {
            Entries.Clear();

            if (!string.IsNullOrWhiteSpace(DocumentKey))
            {
                foreach (KeyValuePair<string, string> item in _store.GetSessionVariables(DocumentKey))
                {
                    Entries.Add(new VariableEntry(item.Key, item.Value, IsSession: true));
                }
            }

            foreach (KeyValuePair<string, string> item in _store.GlobalVariables)
            {
                Entries.Add(new VariableEntry(item.Key, item.Value, IsSession: false));
            }
        }
        finally
        {
            Entries.RaiseListChangedEvents = true;
            Entries.ResetBindings();
        }
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
        Entries.Clear();
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
