using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Login;

namespace JustData.ViewModels;

/// <summary>
/// Composes session state and shell actions. It listens only to the three
/// application events defined by the migration contract.
/// </summary>
public sealed class ShellViewModel : ViewModelBase, IDisposable
{
    private readonly IApplicationSession _session;
    private readonly IMessenger _messenger;
    private bool _disposed;
    private string? _activeConnectionName;
    private string? _lastRefreshedConnectionName;
    private DateTimeOffset? _lastSchemaRefresh;

    public ShellViewModel(IApplicationSession session, IMessenger messenger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        OpenPreferencesCommand = new RelayCommand(() => OpenPreferencesRequested?.Invoke());
        RefreshSchemaCommand = new RelayCommand(() => RefreshSchemaRequested?.Invoke());
        ShutdownCommand = new RelayCommand(() => ShutdownRequested?.Invoke());
        _messenger.Register<SettingsSavedMessage>(this, OnSettingsSaved);
        _messenger.Register<ActiveConnectionChangedMessage>(this, OnActiveConnectionChanged);
        _messenger.Register<SchemaRefreshedMessage>(this, OnSchemaRefreshed);
    }

    public LoginSelection? CurrentLogin => _session.CurrentLogin;
    public string? ActiveConnectionName => _activeConnectionName;
    public string? LastRefreshedConnectionName => _lastRefreshedConnectionName;
    public DateTimeOffset? LastSchemaRefresh => _lastSchemaRefresh;

    public IRelayCommand OpenPreferencesCommand { get; }
    public IRelayCommand RefreshSchemaCommand { get; }
    public IRelayCommand ShutdownCommand { get; }

    public event Action? OpenPreferencesRequested;
    public event Action? RefreshSchemaRequested;
    public event Action? ShutdownRequested;

    private void OnSettingsSaved(object recipient, SettingsSavedMessage message) => OnPropertyChanged(nameof(CurrentLogin));

    private void OnActiveConnectionChanged(object recipient, ActiveConnectionChangedMessage message)
    {
        _activeConnectionName = message.ConnectionName;
        OnPropertyChanged(nameof(ActiveConnectionName));
    }

    private void OnSchemaRefreshed(object recipient, SchemaRefreshedMessage message)
    {
        _lastRefreshedConnectionName = message.ConnectionName;
        _lastSchemaRefresh = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(LastRefreshedConnectionName));
        OnPropertyChanged(nameof(LastSchemaRefresh));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _messenger.UnregisterAll(this);
        OpenPreferencesRequested = null;
        RefreshSchemaRequested = null;
        ShutdownRequested = null;
    }
}
