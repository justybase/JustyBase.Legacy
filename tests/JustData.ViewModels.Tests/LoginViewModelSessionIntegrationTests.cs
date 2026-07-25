using CommunityToolkit.Mvvm.Messaging;
using JustData.Application;
using JustData.Application.Login;
using JustData.ViewModels;

namespace JustData.ViewModels.Tests;

/// <summary>
/// Integration tests for LoginViewModel x IApplicationSession,
/// covering edge cases not already tested in LoginViewModelTests.
/// </summary>
public sealed class LoginViewModelSessionIntegrationTests
{
    // ── Accept() session integration ──

    [Fact]
    public async Task Accept_calls_SetLogin_and_session_reflects_selection()
    {
        var profile = ValidProfile("dev");
        var session = new ApplicationSession();
        using var vm = CreateVm([profile], session);

        await vm.InitializeAsync();
        vm.Accept();

        Assert.NotNull(session.CurrentLogin);
        Assert.Same(vm.Result, session.CurrentLogin);
        Assert.Equal("dev", session.CurrentLogin!.Profile.Name);
    }

    [Fact]
    public async Task Accept_stores_cloned_profile_in_session()
    {
        var profile = ValidProfile("dev");
        var session = new ApplicationSession();
        using var vm = CreateVm([profile], session);

        await vm.InitializeAsync();
        vm.Accept();

        var sessionProfile = session.CurrentLogin!.Profile;
        Assert.NotSame(profile, sessionProfile);
        Assert.NotSame(vm.SelectedProfile, sessionProfile);
    }

    [Fact]
    public async Task Accept_stores_all_profiles_in_session()
    {
        var profiles = new[] { ValidProfile("dev"), ValidProfile("prod") };
        var session = new ApplicationSession();
        using var vm = CreateVm(profiles, session);

        await vm.InitializeAsync();
        vm.Accept();

        Assert.Equal(2, session.Profiles.Count);
        Assert.Equal("dev", session.Profiles[0].Name);
        Assert.Equal("prod", session.Profiles[1].Name);
    }

    [Fact]
    public async Task Session_profiles_are_cloned_and_independent()
    {
        var profile = ValidProfile("dev");
        var session = new ApplicationSession();
        using var vm = CreateVm([profile], session);

        await vm.InitializeAsync();
        vm.Accept();

        // Modify the original list
        vm.Profiles[0].Name = "modified";

        Assert.Equal("dev", session.Profiles[0].Name);
    }

    [Fact]
    public async Task Multiple_Accept_calls_update_session()
    {
        var profiles = new[] { ValidProfile("first"), ValidProfile("second") };
        var session = new ApplicationSession();
        using var vm = CreateVm(profiles, session);

        await vm.InitializeAsync();

        // First accept
        vm.Accept();
        Assert.Equal("first", session.CurrentLogin!.Profile.Name);

        // Select second and accept again
        vm.SelectedProfile = vm.Profiles[1];
        vm.Accept();

        Assert.Equal("second", session.CurrentLogin!.Profile.Name);
    }

    // ── Accept() edge cases ──

    [Fact]
    public async Task Accept_throws_when_selected_profile_is_null()
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        vm.SelectedProfile = null!;

        var ex = Assert.Throws<InvalidOperationException>(() => vm.Accept());
        Assert.Equal("Select a connection first.", ex.Message);
    }

    [Fact]
    public async Task Accept_throws_when_validation_fails()
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        vm.SelectedProfile!.Name = string.Empty;

        var ex = Assert.Throws<InvalidOperationException>(() => vm.Accept());
        Assert.Equal("Complete the required connection fields.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Accept_throws_when_required_field_is_empty(string? emptyName)
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        vm.SelectedProfile!.Name = emptyName!;

        Assert.Throws<InvalidOperationException>(() => vm.Accept());
    }

    // ── Accept() with FastLogin ──

    [Fact]
    public async Task Accept_respects_fast_login_flag()
    {
        var session = new ApplicationSession();
        using var vm = CreateVm([ValidProfile("dev")], session);

        await vm.InitializeAsync();
        vm.FastLogin = true;
        vm.Accept();

        Assert.True(vm.Result!.FastLogin);
        Assert.True(session.CurrentLogin!.FastLogin);
    }

    [Fact]
    public async Task Accept_without_fast_login()
    {
        var session = new ApplicationSession();
        using var vm = CreateVm([ValidProfile("dev")], session);

        await vm.InitializeAsync();
        vm.FastLogin = false;
        vm.Accept();

        Assert.False(vm.Result!.FastLogin);
        Assert.False(session.CurrentLogin!.FastLogin);
    }

    // ── Reorder edge cases ──

    [Fact]
    public async Task Reorder_with_wrong_count_throws()
    {
        using var vm = CreateVm([ValidProfile("one"), ValidProfile("two")], new ApplicationSession());
        await vm.InitializeAsync();

        Assert.Throws<ArgumentException>(() => vm.Reorder([0], 0));
    }

    [Fact]
    public async Task Reorder_with_duplicates_throws()
    {
        using var vm = CreateVm([ValidProfile("one"), ValidProfile("two")], new ApplicationSession());
        await vm.InitializeAsync();

        Assert.Throws<ArgumentException>(() => vm.Reorder([0, 0], 0));
    }

    [Fact]
    public async Task Reorder_with_out_of_range_index_throws()
    {
        using var vm = CreateVm([ValidProfile("one"), ValidProfile("two")], new ApplicationSession());
        await vm.InitializeAsync();

        Assert.Throws<ArgumentException>(() => vm.Reorder([0, 5], 0));
    }

    [Fact]
    public async Task Reorder_updates_selected_profile_by_default_index()
    {
        var profiles = new[] { ValidProfile("one"), ValidProfile("two"), ValidProfile("three") };
        using var vm = CreateVm(profiles, new ApplicationSession());

        await vm.InitializeAsync();
        // order [2, 0, 1]: Profiles → ["three", "one", "two"]; defaultIndex=1 → Profiles[1]="one"
        vm.Reorder([2, 0, 1], 1);

        Assert.Equal("one", vm.SelectedProfile!.Name);
        Assert.Equal("one", vm.Profiles[1].Name);
        Assert.Equal("three", vm.Profiles[0].Name);
        Assert.Equal("two", vm.Profiles[2].Name);
    }

    [Fact]
    public async Task Reorder_clamps_default_index_to_valid_range()
    {
        var profiles = new[] { ValidProfile("one"), ValidProfile("two") };
        using var vm = CreateVm(profiles, new ApplicationSession());

        await vm.InitializeAsync();
        // order [1, 0]: Profiles → ["two", "one"]; defaultIndex=99 clamped to 1 → Profiles[1]="one"
        vm.Reorder([1, 0], 99);

        Assert.Equal("one", vm.SelectedProfile!.Name);
    }

    // ── Clone edge cases ──

    [Fact]
    public async Task Clone_with_null_selected_profile_does_nothing()
    {
        var session = new ApplicationSession();
        using var vm = CreateVm([ValidProfile("dev")], session);

        await vm.InitializeAsync();
        vm.SelectedProfile = null!;

        // Should not throw
        vm.CloneCommand.Execute(null);

        Assert.Single(vm.Profiles);
    }

    // ── Delete / CanDelete ──

    [Fact]
    public async Task CanDelete_returns_false_when_selected_is_null()
    {
        using var vm = CreateVm([ValidProfile("one")], new ApplicationSession());
        await vm.InitializeAsync();
        vm.SelectedProfile = null!;

        Assert.False(vm.DeleteCommand.CanExecute(null));
    }

    // ── SaveAsync with null selected ──

    [Fact]
    public async Task SaveAsync_does_nothing_when_selected_profile_is_null()
    {
        var profiles = new[] { ValidProfile("dev") };
        var repository = new SaveSpyRepository(profiles);
        using var vm = CreateVm(repository, profiles, new ApplicationSession());

        await vm.InitializeAsync();
        vm.SelectedProfile = null!;

        // Should not throw and not call repository
        await vm.SaveAsync();

        Assert.False(repository.SaveWasCalled);
    }

    // ── SetDefault ──

    [Fact]
    public async Task SetDefault_updates_default_index()
    {
        var profiles = new[] { ValidProfile("one"), ValidProfile("two") };
        var repository = new SaveSpyRepository(profiles);
        using var vm = CreateVm(repository, profiles, new ApplicationSession());

        await vm.InitializeAsync();

        // Select second profile and set as default
        Assert.Equal(2, vm.Profiles.Count);
        vm.SelectedProfile = vm.Profiles[1];
        vm.SetDefaultCommand.Execute(null);

        // Save should reflect the new default index
        await vm.SaveAsync();

        Assert.Equal(1, repository.SavedDefaultIndex);
    }

    [Fact]
    public async Task SetDefault_with_null_selected_does_nothing()
    {
        var profiles = new[] { ValidProfile("one") };
        var repository = new SaveSpyRepository(profiles);
        using var vm = CreateVm(repository, profiles, new ApplicationSession());

        await vm.InitializeAsync();
        vm.SelectedProfile = null!;

        // Should not throw
        vm.SetDefaultCommand.Execute(null);

        Assert.False(repository.SaveWasCalled);
    }

    // ── Initialize with empty profiles creates default ──

    [Fact]
    public async Task Initialize_with_empty_profiles_creates_default_entry()
    {
        var session = new ApplicationSession();
        using var vm = CreateVm([], session);

        await vm.InitializeAsync();

        Assert.Single(vm.Profiles);
        Assert.NotNull(vm.SelectedProfile);
        Assert.Equal("New", vm.SelectedProfile!.Name);
    }

    // ── ErrorMessage is cleared on successful Accept ──

    [Fact]
    public async Task ErrorMessage_is_cleared_on_successful_accept()
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        vm.SelectedProfile!.Name = string.Empty;
        vm.ValidateSelectedProfile();
        Assert.NotNull(vm.ErrorMessage);

        vm.SelectedProfile.Name = "dev";
        vm.Accept();

        Assert.Null(vm.ErrorMessage);
    }

    // ── ValidationErrors after operations ──

    [Fact]
    public async Task ValidationErrors_are_empty_after_successful_accept()
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        vm.Accept();

        Assert.Empty(vm.ValidationErrors);
    }

    [Fact]
    public async Task AcceptCommand_can_execute_when_valid()
    {
        using var vm = CreateVm([ValidProfile("dev")], new ApplicationSession());
        await vm.InitializeAsync();

        Assert.True(vm.AcceptCommand.CanExecute(null));
    }

    // ── Helpers ──

    private static LoginViewModel CreateVm(IReadOnlyList<ConnectionProfile> profiles, IApplicationSession session)
        => CreateVm(new FakeRepository(profiles, 0), profiles, session);

    private static LoginViewModel CreateVm(IConnectionProfileRepository repository, IReadOnlyList<ConnectionProfile> profiles, IApplicationSession session)
        => new(repository, new FakeCatalog([]), session, new WeakReferenceMessenger(), new InlineDispatcher());

    private static ConnectionProfile ValidProfile(string name) => new()
    {
        Name = name,
        Driver = "NetezzaSQL",
        Server = "server",
        UserName = "user",
        Password = "secret",
        Database = "SYSTEM"
    };

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex) : IConnectionProfileRepository
    {
        public Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ConnectionProfilesLoadResult(profiles, defaultIndex, false));

        public Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SaveSpyRepository(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex = 0) : IConnectionProfileRepository
    {
        public bool SaveWasCalled { get; private set; }
        public int SavedDefaultIndex { get; private set; }

        public Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ConnectionProfilesLoadResult(profiles, defaultIndex, false));

        public Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default)
        {
            SaveWasCalled = true;
            SavedDefaultIndex = defaultIndex;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatalog(IReadOnlyList<string> databases) : IDatabaseCatalogService
    {
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
            => Task.FromResult(databases);
    }
}
