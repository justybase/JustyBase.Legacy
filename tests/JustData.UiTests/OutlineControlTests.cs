using JustData.Application.Schema;
using JustData.ViewModels.Explorer;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.ObjectExplorer;

namespace JustData.UiTests;

public sealed class OutlineControlTests
{
    [Theory]
    [InlineData(false, true, false, 10_000, 100_000, false)]
    [InlineData(true, false, false, 100, 10_000, false)]
    [InlineData(true, true, true, 9_188, 244_544, false)]
    [InlineData(true, true, false, 200, 20_000, true)]
    public void Outline_refresh_policy_does_not_charge_hidden_or_large_documents(
        bool enabled,
        bool visible,
        bool largeDocument,
        int lineCount,
        int characterCount,
        bool expected)
    {
        Assert.Equal(expected, OutlineRefreshPolicy.ShouldRefresh(
            enabled, visible, largeDocument, lineCount, characterCount));
    }

    [Fact]
    public async Task Activating_a_row_publishes_the_selected_reference()
    {
        using var viewModel = new ObjectExplorerViewModel(new ReferenceRepository())
        {
            SqlText = "SELECT * FROM orders"
        };
        await viewModel.RefreshAsync();

        using var control = new MvvmObjectExplorerControl(viewModel);
        SchemaReference? activated = null;
        control.ReferenceActivated += reference => activated = reference;

        Assert.True(control.ActivateReference(0));
        Assert.NotNull(activated);
        Assert.Equal("orders", activated!.Name);
        Assert.Same(activated, viewModel.SelectedReference);
        Assert.False(control.ActivateReference(1));
    }

    private sealed class ReferenceRepository : ISchemaRepository
    {
        public Task<IReadOnlyList<SchemaNode>> GetRootsAsync(
            string? connectionName = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaNode>>([]);

        public Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(
            SchemaNode parent,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaNode>>([]);

        public Task<SchemaSearchResult> SearchAsync(
            SchemaSearchRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchemaSearchResult([]));

        public Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(
            string sql,
            string? connectionName = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchemaReference>>(
            [new SchemaReference("orders", SchemaNodeKind.Table, 14)]);

        public Task RefreshAsync(
            string? connectionName = null,
            CancellationToken cancellationToken = default,
            SchemaRefreshRequest? request = null) => Task.CompletedTask;

        public Task<bool> AttachDatabaseAsync(
            string connectionName,
            string databaseName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
