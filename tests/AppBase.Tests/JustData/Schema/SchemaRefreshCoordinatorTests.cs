using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Schema;
using JustyBaseLegacy.UI.Schema;

namespace AppBase.Tests.JustData.Schema;

public sealed class SchemaRefreshCoordinatorTests
{
    [Fact]
    public async Task RefreshAsync_refreshes_provider_before_publishing_roots()
    {
        var repository = new RecordingSchemaRepository();
        var messenger = new WeakReferenceMessenger();
        SchemaRefreshedMessage? received = null;
        messenger.Register<SchemaRefreshedMessage>(this, (_, message) => received = message);
        var coordinator = new SchemaRefreshCoordinator(repository, messenger);

        IReadOnlyList<SchemaNode> roots = await coordinator.RefreshAsync("warehouse");

        Assert.Equal(["refresh:warehouse:default", "roots:warehouse"], repository.Calls);
        Assert.Single(roots);
        Assert.Equal("warehouse", received?.ConnectionName);
    }

    [Fact]
    public async Task RefreshAsync_rejects_an_empty_connection_name()
    {
        var coordinator = new SchemaRefreshCoordinator(new RecordingSchemaRepository(), new WeakReferenceMessenger());

        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.RefreshAsync(" "));
    }

    [Fact]
    public async Task RefreshAsync_forwards_mode_to_repository_and_publishes_message()
    {
        var repository = new RecordingSchemaRepository();
        var messenger = new WeakReferenceMessenger();
        SchemaRefreshedMessage? received = null;
        messenger.Register<SchemaRefreshedMessage>(this, (_, message) => received = message);
        var coordinator = new SchemaRefreshCoordinator(repository, messenger);

        await coordinator.RefreshAsync(
            "warehouse",
            new SchemaRefreshRequest(SchemaRefreshMode.Full));

        Assert.Equal(["refresh:warehouse:Full", "roots:warehouse"], repository.Calls);
        Assert.Equal("warehouse", received?.ConnectionName);
    }

    [Fact]
    public async Task AttachDatabaseAsync_publishes_message_after_attach()
    {
        var repository = new RecordingSchemaRepository();
        var messenger = new WeakReferenceMessenger();
        SchemaRefreshedMessage? received = null;
        messenger.Register<SchemaRefreshedMessage>(this, (_, message) => received = message);
        var coordinator = new SchemaRefreshCoordinator(repository, messenger);

        await coordinator.AttachDatabaseAsync("warehouse", "SALES");

        Assert.Equal(["attach:warehouse:SALES", "roots:warehouse"], repository.Calls);
        Assert.Equal("warehouse", received?.ConnectionName);
    }

    [Fact]
    public async Task NotifyRefreshedAsync_publishes_message_without_provider_refresh()
    {
        var repository = new RecordingSchemaRepository();
        var messenger = new WeakReferenceMessenger();
        SchemaRefreshedMessage? received = null;
        messenger.Register<SchemaRefreshedMessage>(this, (_, message) => received = message);
        var coordinator = new SchemaRefreshCoordinator(repository, messenger);

        IReadOnlyList<SchemaNode> roots = await coordinator.NotifyRefreshedAsync("warehouse");

        Assert.Equal(["roots:warehouse"], repository.Calls);
        Assert.Single(roots);
        Assert.Equal("warehouse", received?.ConnectionName);
    }

    private sealed class RecordingSchemaRepository : ISchemaRepository
    {
        public List<string> Calls { get; } = [];

        public Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default, SchemaRefreshRequest? request = null)
        {
            Calls.Add($"refresh:{connectionName}:{request?.Mode.ToString() ?? "default"}");
            return Task.CompletedTask;
        }

        public Task<bool> AttachDatabaseAsync(string connectionName, string databaseName, CancellationToken cancellationToken = default)
        {
            Calls.Add($"attach:{connectionName}:{databaseName}");
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(string? connectionName = null, CancellationToken cancellationToken = default)
        {
            Calls.Add($"roots:{connectionName}");
            return Task.FromResult<IReadOnlyList<SchemaNode>>([new(connectionName!, connectionName!, SchemaNodeKind.Connection, new(connectionName!), true)]);
        }

        public Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(SchemaNode parent, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaNode>>([]);

        public Task<SchemaSearchResult> SearchAsync(SchemaSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchemaSearchResult([]));

        public Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(string sql, string? connectionName = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaReference>>([]);
    }
}
