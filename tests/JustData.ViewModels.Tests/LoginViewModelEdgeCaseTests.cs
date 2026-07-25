using CommunityToolkit.Mvvm.Messaging;
using JustData.Application;
using JustData.Application.Login;
using JustData.ViewModels;

namespace JustData.ViewModels.Tests;

public sealed class LoginViewModelEdgeCaseTests
{
    [Fact]
    public async Task Accept_throws_when_profile_has_invalid_fields()
    {
        // Initialize with a profile that has empty required fields
        var profile = new ConnectionProfile { Name = "", Driver = "", Server = "", UserName = "", Database = "" };
        using var vm = Create(new FakeRepository([profile], 0));
        await vm.InitializeAsync();

        // Validation fails, so Accept throws
        Assert.Throws<InvalidOperationException>(() => vm.Accept());
    }

    [Fact]
    public async Task Accept_throws_when_validation_fails()
    {
        var profile = new ConnectionProfile { Name = "test" }; // missing required fields
        using var vm = Create(new FakeRepository([profile], 0));
        await vm.InitializeAsync();

        vm.SelectedProfile!.Name = " ";
        vm.ValidateSelectedProfile();

        Assert.False(vm.AcceptCommand.CanExecute(null));
    }

    [Fact]
    public async Task Reorder_throws_on_invalid_order()
    {
        var profiles = new[]
        {
            new ConnectionProfile { Name = "one" },
            new ConnectionProfile { Name = "two" },
        };
        using var vm = Create(new FakeRepository(profiles, 0));
        await vm.InitializeAsync();

        // Wrong count
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.Run(() => vm.Reorder([0], 0)));

        // Duplicate index
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.Run(() => vm.Reorder([0, 0], 0)));

        // Out of range
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.Run(() => vm.Reorder([0, 5], 0)));
    }

    [Fact]
    public async Task Reorder_preserves_all_profiles()
    {
        var profiles = new[]
        {
            new ConnectionProfile { Name = "one" },
            new ConnectionProfile { Name = "two" },
            new ConnectionProfile { Name = "three" },
        };
        using var vm = Create(new FakeRepository(profiles, 0));
        await vm.InitializeAsync();

        vm.Reorder([2, 0, 1], 1);

        Assert.Equal(3, vm.Profiles.Count);
        Assert.Equal("three", vm.Profiles[0].Name);
        Assert.Equal("one", vm.Profiles[1].Name);
        Assert.Equal("two", vm.Profiles[2].Name);
        // defaultIndex=1 means SelectedProfile is the profile at index 1 after reorder
        Assert.Equal("one", vm.SelectedProfile!.Name);
    }

    [Fact]
    public async Task Validation_errors_include_all_required_fields()
    {
        var profile = new ConnectionProfile(); // all empty
        using var vm = Create(new FakeRepository([profile], 0));
        await vm.InitializeAsync();

        bool valid = vm.ValidateSelectedProfile();

        Assert.False(valid);
        Assert.Contains(nameof(ConnectionProfile.Name), vm.ValidationErrors.Keys);
        Assert.Contains(nameof(ConnectionProfile.Driver), vm.ValidationErrors.Keys);
        Assert.Contains(nameof(ConnectionProfile.Server), vm.ValidationErrors.Keys);
        Assert.Contains(nameof(ConnectionProfile.UserName), vm.ValidationErrors.Keys);
        Assert.Contains(nameof(ConnectionProfile.Database), vm.ValidationErrors.Keys);
    }

    [Fact]
    public async Task Validation_passes_when_all_required_fields_present()
    {
        var profile = new ConnectionProfile
        {
            Name = "test", Driver = "NetezzaSQL", Server = "server",
            UserName = "user", Database = "SYSTEM"
        };
        using var vm = Create(new FakeRepository([profile], 0));
        await vm.InitializeAsync();

        bool valid = vm.ValidateSelectedProfile();

        Assert.True(valid);
        Assert.Empty(vm.ValidationErrors);
    }

    [Fact]
    public async Task Clone_profile_creates_independent_copy()
    {
        var profile = new ConnectionProfile { Name = "orig", Password = "secret" };
        using var vm = Create(new FakeRepository([profile], 0));
        await vm.InitializeAsync();

        vm.CloneCommand.Execute(null);

        Assert.Equal(2, vm.Profiles.Count);
        Assert.Equal("orig_Clone", vm.Profiles[1].Name);

        // Modifying clone doesn't affect original
        vm.Profiles[1].Name = "changed";
        Assert.Equal("orig", vm.Profiles[0].Name);
    }

    private static LoginViewModel Create(FakeRepository repository, IDatabaseCatalogService? catalog = null)
    {
        return new LoginViewModel(
            repository,
            catalog ?? new FakeCatalog(),
            new ApplicationSession(),
            new WeakReferenceMessenger(),
            new InlineDispatcher());
    }

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
        public Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ConnectionProfilesLoadResult(profiles, defaultIndex, false));

        public Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeCatalog : IDatabaseCatalogService
    {
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["SYSTEM"]);
    }
}
