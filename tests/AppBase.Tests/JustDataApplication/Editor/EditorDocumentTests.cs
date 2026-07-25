using JustData.Application.Editor;

namespace AppBase.Tests.JustDataApplication.Editor;

public sealed class EditorDocumentIdTests
{
    [Fact]
    public void New_creates_non_empty_id()
    {
        var id = EditorDocumentId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_creates_unique_ids()
    {
        var id1 = EditorDocumentId.New();
        var id2 = EditorDocumentId.New();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ToString_returns_32_char_hex()
    {
        var id = EditorDocumentId.New();
        var str = id.ToString();
        Assert.Equal(32, str.Length);
        Assert.DoesNotContain("-", str);
    }

    [Fact]
    public void Same_ids_are_equal()
    {
        var guid = Guid.NewGuid();
        var id1 = new EditorDocumentId(guid);
        var id2 = new EditorDocumentId(guid);
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
    }

    [Fact]
    public void Different_ids_are_not_equal()
    {
        var id1 = EditorDocumentId.New();
        var id2 = EditorDocumentId.New();
        Assert.NotEqual(id1, id2);
        Assert.True(id1 != id2);
    }
}

public sealed class EditorDocumentModelsTests
{
    [Fact]
    public void EditorDocumentSnapshot_creates_correctly()
    {
        var id = EditorDocumentId.New();
        var snapshot = new EditorDocumentSnapshot(
            id, "query.sql", "SELECT 1", @"C:\query.sql",
            false, false, "conn", "db", true, false, false);

        Assert.Equal(id, snapshot.Id);
        Assert.Equal("query.sql", snapshot.Title);
        Assert.Equal("SELECT 1", snapshot.Text);
        Assert.Equal(@"C:\query.sql", snapshot.FilePath);
        Assert.False(snapshot.IsDirty);
        Assert.False(snapshot.IsReadOnly);
        Assert.Equal("conn", snapshot.ConnectionName);
        Assert.Equal("db", snapshot.DatabaseName);
        Assert.True(snapshot.KeepConnectionOpen);
        Assert.False(snapshot.ContinueOnError);
        Assert.False(snapshot.ExternalChangePending);
    }

    [Fact]
    public void EditorFileChange_created()
    {
        var change = new EditorFileChange(EditorFileChangeKind.Changed, @"C:\file.sql");
        Assert.Equal(EditorFileChangeKind.Changed, change.Kind);
        Assert.Equal(@"C:\file.sql", change.Path);
        Assert.Null(change.OldPath);
    }

    [Fact]
    public void EditorFileChange_with_old_path()
    {
        var change = new EditorFileChange(EditorFileChangeKind.Renamed, @"C:\new.sql", @"C:\old.sql");
        Assert.Equal(EditorFileChangeKind.Renamed, change.Kind);
        Assert.Equal(@"C:\old.sql", change.OldPath);
    }

    [Fact]
    public void ManySqlContent_creates_correctly()
    {
        var content = new ManySqlContent("My Bundle", "SELECT 1;");
        Assert.Equal("My Bundle", content.Title);
        Assert.Equal("SELECT 1;", content.Text);
    }

    [Fact]
    public void ManySqlBundle_creates_correctly()
    {
        var bundle = new ManySqlBundle(
            new[] { @"C:\a.sql", @"C:\b.sql" },
            new List<ManySqlContent> { new("Q1", "SELECT 1") },
            new[] { "Q1" },
            0);

        Assert.Equal(2, bundle.SqlPaths.Count);
        Assert.Single(bundle.SqlContentList);
        Assert.Single(bundle.TabsOrder);
        Assert.Equal(0, bundle.SelectedTabNum);
    }

    [Fact]
    public void EditorFileChangeKind_has_expected_values()
    {
        Assert.Equal(0, (int)EditorFileChangeKind.Changed);
        Assert.Equal(1, (int)EditorFileChangeKind.Deleted);
        Assert.Equal(2, (int)EditorFileChangeKind.Renamed);
    }
}
