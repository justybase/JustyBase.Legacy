using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Login;
using JustData.Application;

namespace JustData.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase, IDisposable
{
    private readonly IConnectionProfileRepository _repository;
    private readonly IDatabaseCatalogService _catalog;
    private readonly IApplicationSession _session;
    private readonly IMessenger _messenger;
    private readonly IUiDispatcher _dispatcher;
    private CancellationTokenSource? _databaseCancellation;
    private int _defaultIndex;
    private IReadOnlyDictionary<string, IReadOnlyList<string>> _validationErrors = new Dictionary<string, IReadOnlyList<string>>();

    public BindingList<ConnectionProfile> Profiles { get; } = [];
    public BindingList<string> Databases { get; } = [];
    public IRelayCommand AcceptCommand { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors => _validationErrors;

    [ObservableProperty] private ConnectionProfile? selectedProfile;
    [ObservableProperty] private bool fastLogin;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private LoginSelection? result;
    [ObservableProperty] private bool isCancelled;

    public LoginViewModel(IConnectionProfileRepository repository, IDatabaseCatalogService catalog, IApplicationSession session, IMessenger messenger, IUiDispatcher dispatcher)
    {
        _repository = repository;
        _catalog = catalog;
        _session = session;
        _messenger = messenger;
        _dispatcher = dispatcher;
        AcceptCommand = new RelayCommand(() => Accept(), CanAccept);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        }, cancellationToken).ConfigureAwait(false);
        try
        {
            var loaded = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() =>
            {
                Profiles.RaiseListChangedEvents = false; Profiles.Clear();
                foreach (var profile in loaded.Profiles) Profiles.Add(profile.Clone());
                if (Profiles.Count == 0) Profiles.Add(NewProfile("New"));
                _defaultIndex = Math.Clamp(loaded.DefaultIndex, 0, Profiles.Count - 1);
                SelectedProfile = Profiles[_defaultIndex];
                Profiles.ResetBindings();
                ValidateSelectedProfile();
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                Profiles.Clear();
                Profiles.Add(NewProfile("New"));
                SelectedProfile = Profiles[0];
                ValidateSelectedProfile();
                ErrorMessage = "Saved connections could not be loaded.";
            }, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsBusy = false, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedProfile is null) return;
        await _dispatcher.InvokeAsync(() =>
        {
            ErrorMessage = null;
            IsBusy = true;
        }, cancellationToken).ConfigureAwait(false);
        try { await _repository.SaveAsync(Profiles.ToList(), _defaultIndex, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            await _dispatcher.InvokeAsync(
                () => ErrorMessage = "Connection settings could not be saved.",
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsBusy = false, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    [RelayCommand] private Task SaveAsyncCommand() => SaveAsync();
    [RelayCommand] private void Add() { var profile = NewProfile($"{SelectedProfile?.Name}_1"); Profiles.Add(profile); SelectedProfile = profile; _defaultIndex = Profiles.IndexOf(profile); ValidateSelectedProfile(); }
    [RelayCommand] private void Clone() { if (SelectedProfile is null) return; var profile = SelectedProfile.Clone(); profile.Name += "_Clone"; Profiles.Add(profile); SelectedProfile = profile; ValidateSelectedProfile(); }
    [RelayCommand(CanExecute = nameof(CanDelete))] private void Delete()
    {
        if (SelectedProfile is null || Profiles.Count <= 1) return;
        var index = Profiles.IndexOf(SelectedProfile); Profiles.RemoveAt(index); _defaultIndex = Math.Clamp(_defaultIndex, 0, Profiles.Count - 1); SelectedProfile = Profiles[Math.Min(index, Profiles.Count - 1)]; ValidateSelectedProfile();
    }
    private bool CanDelete() => Profiles.Count > 1 && SelectedProfile is not null;
    [RelayCommand] private void SetDefault() { if (SelectedProfile is not null) _defaultIndex = Profiles.IndexOf(SelectedProfile); }
    public void Reorder(IReadOnlyList<int> order, int defaultIndex)
    {
        if (order.Count != Profiles.Count || order.Distinct().Count() != Profiles.Count || order.Any(index => index < 0 || index >= Profiles.Count)) throw new ArgumentException("Order must contain every profile exactly once.", nameof(order));
        var reordered = order.Select(index => Profiles[index]).ToArray(); Profiles.RaiseListChangedEvents = false; Profiles.Clear(); foreach (var profile in reordered) Profiles.Add(profile); Profiles.ResetBindings(); _defaultIndex = Math.Clamp(defaultIndex, 0, Profiles.Count - 1); SelectedProfile = Profiles[_defaultIndex]; ValidateSelectedProfile();
    }
    [RelayCommand] private async Task FetchDatabasesAsync()
    {
        if (SelectedProfile is null) return;
        _databaseCancellation?.Cancel(); _databaseCancellation?.Dispose(); _databaseCancellation = new CancellationTokenSource();
        var cancellation = _databaseCancellation;
        await _dispatcher.InvokeAsync(() =>
        {
            ErrorMessage = null;
            IsBusy = true;
        }, cancellation.Token).ConfigureAwait(false);
        try { var databases = await _catalog.GetDatabasesAsync(SelectedProfile.Clone(), cancellation.Token).ConfigureAwait(false); await _dispatcher.InvokeAsync(() => { Databases.RaiseListChangedEvents = false; Databases.Clear(); foreach (var database in databases) Databases.Add(database); Databases.ResetBindings(); }, cancellation.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            await _dispatcher.InvokeAsync(
                () => ErrorMessage = "Databases could not be retrieved.",
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsBusy = false, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
    public LoginSelection Accept()
    {
        if (SelectedProfile is null) throw new InvalidOperationException("Select a connection first.");
        if (!ValidateSelectedProfile()) throw new InvalidOperationException("Complete the required connection fields.");
        Result = new LoginSelection(SelectedProfile.Clone(), FastLogin); _session.SetLogin(Result, Profiles); return Result;
    }
    public bool ValidateSelectedProfile()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (SelectedProfile is null) errors["SelectedProfile"] = ["Select a connection."];
        else
        {
            AddRequiredError(errors, nameof(ConnectionProfile.Name), SelectedProfile.Name);
            AddRequiredError(errors, nameof(ConnectionProfile.Driver), SelectedProfile.Driver);
            AddRequiredError(errors, nameof(ConnectionProfile.Server), SelectedProfile.Server);
            AddRequiredError(errors, nameof(ConnectionProfile.UserName), SelectedProfile.UserName);
            AddRequiredError(errors, nameof(ConnectionProfile.Database), SelectedProfile.Database);
        }
        _validationErrors = errors;
        ErrorMessage = errors.Count == 0 ? null : "Complete the required connection fields.";
        AcceptCommand.NotifyCanExecuteChanged();
        return errors.Count == 0;
    }
    private bool CanAccept() => SelectedProfile is not null && _validationErrors.Count == 0;
    private static void AddRequiredError(IDictionary<string, IReadOnlyList<string>> errors, string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[propertyName] = [$"{propertyName} is required."];
    }
    [RelayCommand] private void Cancel() { IsCancelled = true; Result = null; }
    public void Dispose()
    {
        var cancellation = Interlocked.Exchange(ref _databaseCancellation, null);
        if (cancellation is null) return;
        try { cancellation.Cancel(); }
        catch (ObjectDisposedException) { }
        finally { cancellation.Dispose(); }
    }
    private static ConnectionProfile NewProfile(string name) => new() { Name = name, Driver = "NetezzaSQL", Server = "server ip", UserName = "username", Password = "password", Database = "SYSTEM" };
}
