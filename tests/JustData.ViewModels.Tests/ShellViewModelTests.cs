using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Login;
using JustData.ViewModels;

namespace JustData.ViewModels.Tests;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Shell_listens_only_to_contract_messages_and_composes_commands()
    {
        var session = new ApplicationSession();
        var messenger = new WeakReferenceMessenger();
        using var vm = new ShellViewModel(session, messenger);
        int open = 0, refresh = 0, shutdown = 0;
        vm.OpenPreferencesRequested += () => open++;
        vm.RefreshSchemaRequested += () => refresh++;
        vm.ShutdownRequested += () => shutdown++;

        vm.OpenPreferencesCommand.Execute(null);
        vm.RefreshSchemaCommand.Execute(null);
        vm.ShutdownCommand.Execute(null);
        messenger.Send(new ActiveConnectionChangedMessage("conn"));
        messenger.Send(new SchemaRefreshedMessage("conn"));
        messenger.Send(new SettingsSavedMessage());

        Assert.Equal(1, open);
        Assert.Equal(1, refresh);
        Assert.Equal(1, shutdown);
        Assert.Equal("conn", vm.ActiveConnectionName);
        Assert.Equal("conn", vm.LastRefreshedConnectionName);
        Assert.NotNull(vm.LastSchemaRefresh);
    }

    [Fact]
    public void External_open_request_validation_accepts_only_supported_document_formats()
    {
        Assert.True(ExternalOpenRequest.TryCreate("C:\\work\\query.sql", out var sql));
        Assert.Equal("query.sql", Path.GetFileName(sql!.Path));
        Assert.True(ExternalOpenRequest.TryCreate("C:\\work\\bundle.manysql.enc", out _));
        Assert.False(ExternalOpenRequest.TryCreate("C:\\work\\image.png", out _));
        Assert.False(ExternalOpenRequest.TryCreate("\0bad.sql", out _));
    }
}
