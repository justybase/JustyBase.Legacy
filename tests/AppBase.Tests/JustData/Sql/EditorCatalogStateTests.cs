using JustyBaseLegacy.UI.Sql;

namespace AppBase.Tests.JustData.Sql;

public sealed class EditorCatalogStateTests
{
    [Fact]
    public void Snapshot_is_scoped_and_cannot_mutate_catalog_state()
    {
        var catalog = new EditorCatalogState();
        catalog.AddConnection("NZ-A");
        catalog.ReplaceDatabases("NZ-A", ["SYSTEM", "ANALYTICS"]);

        EditorCatalogSnapshot snapshot = catalog.Snapshot;
        Assert.Equal(["NZ-A"], snapshot.Connections);
        Assert.Equal(["SYSTEM", "ANALYTICS"], snapshot.DatabasesFor("NZ-A"));

        catalog.RemoveConnection("NZ-A");

        Assert.Equal(["NZ-A"], snapshot.Connections);
        Assert.Empty(catalog.Snapshot.Connections);
        Assert.Empty(catalog.Snapshot.DatabasesFor("NZ-A"));
    }

    [Fact]
    public void Catalog_publishes_only_effective_changes()
    {
        var catalog = new EditorCatalogState();
        var snapshots = new List<EditorCatalogSnapshot>();
        catalog.Changed += snapshots.Add;

        catalog.AddConnection("NZ-A");
        catalog.AddConnection("nz-a");
        catalog.AddDatabase("NZ-A", "SYSTEM");
        catalog.AddDatabase("NZ-A", "system");

        Assert.Equal(2, snapshots.Count);
        Assert.Equal(["NZ-A"], snapshots[^1].Connections);
        Assert.Equal(["SYSTEM"], snapshots[^1].DatabasesFor("NZ-A"));
    }
}
