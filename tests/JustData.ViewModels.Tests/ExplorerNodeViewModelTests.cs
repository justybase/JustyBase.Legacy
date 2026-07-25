using JustData.Application.Schema;
using JustData.ViewModels.Explorer;

namespace JustData.ViewModels.Tests;

public sealed class ExplorerNodeViewModelTests
{
    [Fact]
    public void Constructor_throws_when_model_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ExplorerNodeViewModel(null!));
    }

    [Fact]
    public void Properties_delegate_to_model()
    {
        var path = new SchemaPath("conn", "db", "schema", "table");
        var model = new SchemaNode("id-1", "orders", SchemaNodeKind.Table, path, HasChildren: true);
        var node = new ExplorerNodeViewModel(model);

        Assert.Equal("id-1", node.Id);
        Assert.Equal("orders", node.Name);
        Assert.Equal(SchemaNodeKind.Table, node.Kind);
        Assert.Equal(path, node.Path);
        Assert.True(node.HasChildren);
        Assert.False(node.ChildrenLoaded);
        Assert.False(node.HasPendingChildren);
        Assert.Empty(node.Children);
        Assert.False(node.IsExpanded);
        Assert.False(node.IsLoading);
    }

    [Fact]
    public void BeginChildrenLoad_clears_children_and_sets_pending_state()
    {
        var node = CreateNode();
        IReadOnlyList<SchemaNode> children =
        [
            new SchemaNode("c1", "child1", SchemaNodeKind.Column, new SchemaPath("conn"), false),
            new SchemaNode("c2", "child2", SchemaNodeKind.Column, new SchemaPath("conn"), false),
        ];

        node.BeginChildrenLoad(children);

        Assert.Empty(node.Children);
        Assert.False(node.ChildrenLoaded);
        Assert.True(node.HasPendingChildren);
    }

    [Fact]
    public void AppendNextChildrenBatch_adds_children_in_batches_and_fires_event()
    {
        var node = CreateNode();
        var allChildren = Enumerable.Range(0, 150)
            .Select(i => new SchemaNode($"c{i}", $"child{i}", SchemaNodeKind.Column, new SchemaPath("conn"), false))
            .ToArray();
        var appendedBatches = new List<ExplorerChildrenAppendedEventArgs>();
        node.ChildrenAppended += (_, e) => appendedBatches.Add(e);

        node.BeginChildrenLoad(allChildren);

        // First batch of 100
        node.AppendNextChildrenBatch(ExplorerNodeViewModel.InitialChildBatchSize);
        Assert.Equal(100, node.Children.Count);
        Assert.True(node.HasPendingChildren);
        Assert.Equal(100, appendedBatches[0].Children.Count);

        // Second batch of 50
        node.AppendNextChildrenBatch(ExplorerNodeViewModel.InitialChildBatchSize);
        Assert.Equal(150, node.Children.Count);
        Assert.False(node.HasPendingChildren);
        Assert.Equal(50, appendedBatches[1].Children.Count);

        // No-op after complete
        node.AppendNextChildrenBatch(ExplorerNodeViewModel.InitialChildBatchSize);
        Assert.Equal(150, node.Children.Count);
        Assert.Equal(2, appendedBatches.Count);
    }

    [Fact]
    public void AppendNextChildrenBatch_noop_when_batchSize_zero_or_negative()
    {
        var node = CreateNode();
        node.BeginChildrenLoad([new SchemaNode("c1", "child", SchemaNodeKind.Column, new SchemaPath("conn"), false)]);

        node.AppendNextChildrenBatch(0);
        node.AppendNextChildrenBatch(-1);

        Assert.Empty(node.Children);
        Assert.True(node.HasPendingChildren);
    }

    [Fact]
    public void CompleteChildrenLoad_finalizes_children_loaded_state()
    {
        var node = CreateNode();
        node.BeginChildrenLoad([
            new SchemaNode("c1", "child1", SchemaNodeKind.Column, new SchemaPath("conn"), false),
        ]);

        node.AppendNextChildrenBatch(100);
        node.CompleteChildrenLoad();

        Assert.Single(node.Children);
        Assert.True(node.ChildrenLoaded);
        Assert.False(node.HasPendingChildren);
    }

    [Fact]
    public void IsExpanded_and_IsLoading_fire_property_changed()
    {
        var node = CreateNode();
        var changed = new List<string>();
        node.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        node.IsExpanded = true;
        node.IsLoading = true;

        Assert.Contains(nameof(ExplorerNodeViewModel.IsExpanded), changed);
        Assert.Contains(nameof(ExplorerNodeViewModel.IsLoading), changed);
    }

    [Fact]
    public void IsExpanded_set_to_same_value_does_not_fire_property_changed()
    {
        var node = CreateNode();
        var changed = new List<string>();
        node.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        node.IsExpanded = false; // same as default
        node.IsLoading = false; // same as default

        Assert.DoesNotContain(nameof(ExplorerNodeViewModel.IsExpanded), changed);
        Assert.DoesNotContain(nameof(ExplorerNodeViewModel.IsLoading), changed);
    }

    [Fact]
    public void BeginChildrenLoad_resets_Loaded_flag_when_called_again()
    {
        var node = CreateNode();
        node.BeginChildrenLoad([
            new SchemaNode("c1", "child1", SchemaNodeKind.Column, new SchemaPath("conn"), false),
        ]);
        node.AppendNextChildrenBatch(100);
        node.CompleteChildrenLoad();
        Assert.True(node.ChildrenLoaded);

        // Re-load
        node.BeginChildrenLoad([
            new SchemaNode("c2", "child2", SchemaNodeKind.Column, new SchemaPath("conn"), false),
        ]);

        Assert.False(node.ChildrenLoaded);
        Assert.Empty(node.Children);
        Assert.True(node.HasPendingChildren);
    }

    private static ExplorerNodeViewModel CreateNode() =>
        new(new SchemaNode("root", "root", SchemaNodeKind.Connection, new SchemaPath("conn"), true));
}
