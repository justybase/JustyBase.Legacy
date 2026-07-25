using JustData.Application.Variables;
using JustData.ViewModels.Variables;

namespace JustData.ViewModels.Tests;

public sealed class VariablesViewModelTests
{
    [Fact]
    public void Refresh_projects_session_rows_before_global_rows()
    {
        FakeStore store = new();
        store.Session["query.sql"] = new() { ["session_id"] = "42" };
        store.Global["global_name"] = "value";
        using VariablesViewModel vm = new(store);

        vm.Refresh("query.sql");

        Assert.Collection(
            vm.Entries,
            entry =>
            {
                Assert.Equal("session_id", entry.Name);
                Assert.True(entry.IsSession);
            },
            entry =>
            {
                Assert.Equal("global_name", entry.Name);
                Assert.False(entry.IsSession);
            });
    }

    [Fact]
    public void Clear_command_clears_globals_and_insert_command_emits_only_the_name()
    {
        FakeStore store = new();
        store.Global["global_name"] = "value";
        using VariablesViewModel vm = new(store);
        string? inserted = null;
        vm.InsertVariableRequested += value => inserted = value;
        vm.Refresh(null);

        vm.InsertVariableCommand.Execute(vm.Entries[0]);
        vm.ClearGlobalsCommand.Execute(null);

        Assert.Equal("global_name", inserted);
        Assert.Empty(store.Global);
        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void Dispose_unsubscribes_from_store_changes()
    {
        FakeStore store = new();
        using VariablesViewModel vm = new(store);
        vm.Refresh(null);
        vm.Dispose();
        store.Global["late"] = "value";

        store.RaiseChanged();

        Assert.Empty(vm.Entries);
    }

    private sealed class FakeStore : ISessionVariableStore
    {
        public Dictionary<string, Dictionary<string, string>> Session { get; } = new();
        public Dictionary<string, string> Global { get; } = new();
        public event EventHandler? Changed;

        public IReadOnlyDictionary<string, string> GlobalVariables => Global;

        public IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey) =>
            Session.TryGetValue(documentKey, out Dictionary<string, string>? values)
                ? values
                : new Dictionary<string, string>();

        public void ClearGlobalVariables() => Global.Clear();

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
