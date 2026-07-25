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

        Assert.Equal(["refresh:warehouse", "roots:warehouse"], repository.Calls);
        Assert.Single(roots);
        Assert.Equal("warehouse", received?.ConnectionName);
    }

    [Fact]
    public async Task RefreshAsync_rejects_an_empty_connection_name()
    {
        var coordinator = new SchemaRefreshCoordinator(new RecordingSchemaRepository(), new WeakReferenceMessenger());

        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.RefreshAsync(" "));
    }

    private sealed class RecordingSchemaRepository : ISchemaRepository
    {
        public List<string> Calls { get; } = [];

        public Task RefreshAsync(string? connectionName = null, CancellationToken cancellationToken = default)
        {
            Calls.Add($"refresh:{connectionName}");
            return Task.CompletedTask;
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
