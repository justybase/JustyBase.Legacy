using CommunityToolkit.Mvvm.Messaging;
using JustData.Application;
using JustData.Application.Login;
using JustData.ViewModels;

namespace JustData.ViewModels.Tests;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task Initialize_accept_and_cancel_preserve_the_login_outcome_without_exposing_passwords()
    {
        var profile = new ConnectionProfile { Name = "local", Driver = "NetezzaSQL", Server = "server", UserName = "user", Password = "secret", Database = "SYSTEM" };
        var session = new ApplicationSession();
        using var vm = Create(new FakeRepository([profile], 0), session);

        await vm.InitializeAsync();
        vm.FastLogin = true;
        vm.AcceptCommand.Execute(null);

        Assert.Equal("local", vm.Result!.Profile.Name);
        Assert.True(vm.Result.FastLogin);
        Assert.Equal("local", session.CurrentLogin!.Profile.Name);
        Assert.DoesNotContain("secret", vm.Result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("secret", vm.Result.Profile.ToString(), StringComparison.Ordinal);

        vm.CancelCommand.Execute(null);
        Assert.True(vm.IsCancelled);
        Assert.Null(vm.Result);
    }

    [Fact]
    public async Task Add_clone_delete_reorder_default_and_save_follow_legacy_profile_rules()
    {
        var repository = new FakeRepository([new ConnectionProfile { Name = "one", Password = "secret" }], 0);
        using var vm = Create(repository, new ApplicationSession());
        await vm.InitializeAsync();

        Assert.False(vm.DeleteCommand.CanExecute(null));
        vm.AddCommand.Execute(null);
        vm.CloneCommand.Execute(null);
        vm.Reorder([2, 0, 1], 1);
        vm.SetDefaultCommand.Execute(null);
        await vm.SaveAsync();

        Assert.Equal(3, repository.SavedProfiles!.Count);
        Assert.InRange(repository.SavedDefaultIndex, 0, 2);
        vm.DeleteCommand.Execute(null);
        vm.DeleteCommand.Execute(null);
        Assert.Single(vm.Profiles);
        Assert.False(vm.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Initialize_preserves_the_saved_default_index()
    {
        var profiles = new[]
        {
            new ConnectionProfile { Name = "one" },
            new ConnectionProfile { Name = "two" }
        };
        using var vm = Create(new FakeRepository(profiles, 1), new ApplicationSession());

        await vm.InitializeAsync();

        Assert.Equal("two", vm.SelectedProfile!.Name);
    }

    [Fact]
    public async Task Fetch_databases_handles_failure_cancellation_and_disposal()
    {
        var catalog = new FakeCatalog(["SYSTEM"]);
        using var vm = Create(new FakeRepository([new ConnectionProfile { Name = "one" }], 0), new ApplicationSession(), catalog);
        await vm.InitializeAsync();
        await vm.FetchDatabasesCommand.ExecuteAsync(null);
        Assert.Equal(["SYSTEM"], vm.Databases);

        catalog.Exception = new InvalidOperationException();
        await vm.FetchDatabasesCommand.ExecuteAsync(null);
        Assert.Equal("Databases could not be retrieved.", vm.ErrorMessage);
        vm.Dispose();
    }

    [Fact]
    public async Task Initialize_error_uses_safe_fallback_and_save_error_is_user_safe()
    {
        var repository = new FakeRepository([], 0) { LoadException = new IOException() };
        using var vm = Create(repository, new ApplicationSession());

        await vm.InitializeAsync();

        Assert.Equal("Saved connections could not be loaded.", vm.ErrorMessage);
        Assert.Single(vm.Profiles);
        Assert.True(vm.AcceptCommand.CanExecute(null));

        repository.SaveException = new UnauthorizedAccessException();
        await vm.SaveAsync();

        Assert.Equal("Connection settings could not be saved.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Validation_blocks_accept_until_required_fields_are_restored()
    {
        var profile = new ConnectionProfile { Name = "local", Driver = "NetezzaSQL", Server = "server", UserName = "user", Database = "SYSTEM" };
        using var vm = Create(new FakeRepository([profile], 0), new ApplicationSession());
        await vm.InitializeAsync();

        vm.SelectedProfile!.Name = " ";
        Assert.False(vm.ValidateSelectedProfile());
        Assert.False(vm.AcceptCommand.CanExecute(null));
        Assert.Contains(nameof(ConnectionProfile.Name), vm.ValidationErrors.Keys);

        vm.SelectedProfile.Name = "local";
        Assert.True(vm.ValidateSelectedProfile());
        Assert.True(vm.AcceptCommand.CanExecute(null));
    }

    [Fact]
    public async Task Fetch_databases_restarts_and_disposes_the_previous_request()
    {
        var catalog = new RestartableCatalog();
        using var vm = Create(new FakeRepository([new ConnectionProfile { Name = "one" }], 0), new ApplicationSession(), catalog);
        await vm.InitializeAsync();

        Task first = vm.FetchDatabasesCommand.ExecuteAsync(null);
        await catalog.FirstStarted.Task;
        Task second = vm.FetchDatabasesCommand.ExecuteAsync(null);
        await catalog.SecondStarted.Task;
        catalog.CompleteSecond(["SECOND"]);
        await Task.WhenAll(first, second);

        Assert.Equal(["SECOND"], vm.Databases);
        vm.Dispose();
        Assert.True(catalog.FirstCancellationObserved);
    }

    private static LoginViewModel Create(FakeRepository repository, ApplicationSession session, IDatabaseCatalogService? catalog = null) => new(repository, catalog ?? new FakeCatalog([]), session, new WeakReferenceMessenger(), new InlineDispatcher());

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); action(); return Task.CompletedTask; }
    }
    private sealed class FakeRepository(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex) : IConnectionProfileRepository
    {
        public Exception? LoadException { get; set; }
        public Exception? SaveException { get; set; }
        public IReadOnlyList<ConnectionProfile>? SavedProfiles { get; private set; }
        public int SavedDefaultIndex { get; private set; }
        public Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default) => LoadException is null ? Task.FromResult(new ConnectionProfilesLoadResult(profiles, defaultIndex, false)) : Task.FromException<ConnectionProfilesLoadResult>(LoadException);
        public Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default)
        {
            if (SaveException is not null) return Task.FromException(SaveException);
            SavedProfiles = profiles.Select(profile => profile.Clone()).ToArray(); SavedDefaultIndex = defaultIndex; return Task.CompletedTask;
        }
    }
    private sealed class FakeCatalog(IReadOnlyList<string> databases) : IDatabaseCatalogService
    {
        public Exception? Exception { get; set; }
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => Exception is null ? Task.FromResult(databases) : Task.FromException<IReadOnlyList<string>>(Exception);
    }

    private sealed class RestartableCatalog : IDatabaseCatalogService
    {
        private int _calls;
        private readonly TaskCompletionSource<IReadOnlyList<string>> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool FirstCancellationObserved { get; private set; }
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1) return WaitFirstAsync(cancellationToken);
            SecondStarted.TrySetResult(true);
            return _second.Task.WaitAsync(cancellationToken);
        }
        private async Task<IReadOnlyList<string>> WaitFirstAsync(CancellationToken cancellationToken)
        {
            FirstStarted.TrySetResult(true);
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
            catch (OperationCanceledException) { FirstCancellationObserved = true; throw; }
            return [];
        }
        public void CompleteSecond(IReadOnlyList<string> databases) => _second.TrySetResult(databases);
    }
}
