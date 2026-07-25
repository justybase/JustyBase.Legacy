using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Login;
using JustData.ViewModels;

namespace JustData.ViewModels.Tests;

public sealed class ShellViewModelExtendedTests
{
    [Fact]
    public void Dispose_unregisters_from_messenger_and_clears_events()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        var vm = new ShellViewModel(session, messenger);
        int open = 0, refresh = 0, shutdown = 0;
        vm.OpenPreferencesRequested += () => open++;
        vm.RefreshSchemaRequested += () => refresh++;
        vm.ShutdownRequested += () => shutdown++;

        vm.Dispose();
        messenger.Send(new ActiveConnectionChangedMessage("conn"));
        messenger.Send(new SchemaRefreshedMessage("conn"));
        messenger.Send(new SettingsSavedMessage());

        // Commands still execute but events are null
        vm.OpenPreferencesCommand.Execute(null);
        vm.RefreshSchemaCommand.Execute(null);
        vm.ShutdownCommand.Execute(null);

        Assert.Equal(0, open);
        Assert.Equal(0, refresh);
        Assert.Equal(0, shutdown);
        Assert.Null(vm.ActiveConnectionName);
        Assert.Null(vm.LastRefreshedConnectionName);
    }

    [Fact]
    public void Dispose_can_be_called_multiple_times()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        var vm = new ShellViewModel(session, messenger);

        vm.Dispose();
        vm.Dispose(); // should not throw
    }

    [Fact]
    public void CurrentLogin_reflects_session_state()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);

        Assert.Null(vm.CurrentLogin);

        var profile = new ConnectionProfile { Name = "prod" };
        var selection = new LoginSelection(profile, fastLogin: false);
        session.SetLogin(selection, new List<ConnectionProfile> { profile });

        Assert.Equal("prod", vm.CurrentLogin!.Profile.Name);
    }

    [Fact]
    public void Settings_saved_message_updates_current_login()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);

        var profile = new ConnectionProfile { Name = "dev" };
        var selection = new LoginSelection(profile, fastLogin: false);
        session.SetLogin(selection, new List<ConnectionProfile> { profile });

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        messenger.Send(new SettingsSavedMessage());

        Assert.Contains(nameof(ShellViewModel.CurrentLogin), changed);
    }

    [Fact]
    public void Active_connection_changed_updates_active_connection_name()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);

        messenger.Send(new ActiveConnectionChangedMessage("production"));

        Assert.Equal("production", vm.ActiveConnectionName);
    }

    [Fact]
    public void Schema_refreshed_updates_refresh_state()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);

        var before = DateTimeOffset.UtcNow;
        messenger.Send(new SchemaRefreshedMessage("db1"));
        var after = DateTimeOffset.UtcNow;

        Assert.Equal("db1", vm.LastRefreshedConnectionName);
        Assert.NotNull(vm.LastSchemaRefresh);
        Assert.InRange(vm.LastSchemaRefresh.Value, before, after);
    }

    [Fact]
    public void Commands_fire_events_independently()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);
        int open = 0, refresh = 0, shutdown = 0;
        vm.OpenPreferencesRequested += () => open++;
        vm.RefreshSchemaRequested += () => refresh++;
        vm.ShutdownRequested += () => shutdown++;

        vm.OpenPreferencesCommand.Execute(null);
        Assert.Equal(1, open);
        Assert.Equal(0, refresh);
        Assert.Equal(0, shutdown);

        vm.RefreshSchemaCommand.Execute(null);
        Assert.Equal(1, open);
        Assert.Equal(1, refresh);
        Assert.Equal(0, shutdown);

        vm.ShutdownCommand.Execute(null);
        Assert.Equal(1, open);
        Assert.Equal(1, refresh);
        Assert.Equal(1, shutdown);
    }
}
