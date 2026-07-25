using JustData.Application.Schema;

namespace AppBase.Tests.JustDataApplication.Schema;

public sealed class SchemaModelsTests
{
    // ── SchemaPath ──

    [Fact]
    public void SchemaPath_ToString_joins_all_parts()
    {
        var path = new SchemaPath("conn", "db", "schema", "table");
        Assert.Equal("conn.db.schema.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_null_database()
    {
        var path = new SchemaPath("conn", null, "schema", "table");
        Assert.Equal("conn.schema.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_null_schema()
    {
        var path = new SchemaPath("conn", "db", null, "table");
        Assert.Equal("conn.db.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_null_object()
    {
        var path = new SchemaPath("conn", "db", "schema", null);
        Assert.Equal("conn.db.schema", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_whitespace_parts()
    {
        var path = new SchemaPath("conn", "  ", "schema", "table");
        Assert.Equal("conn.schema.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_with_only_connection()
    {
        var path = new SchemaPath("conn", null, null, null);
        Assert.Equal("conn", path.ToString());
    }

    // ── SchemaNode ──

    [Fact]
    public void SchemaNode_creates_with_required_fields()
    {
        var path = new SchemaPath("conn");
        var node = new SchemaNode("id1", "my_table", SchemaNodeKind.Table, path, true);

        Assert.Equal("id1", node.Id);
        Assert.Equal("my_table", node.Name);
        Assert.Equal(SchemaNodeKind.Table, node.Kind);
        Assert.Same(path, node.Path);
        Assert.True(node.HasChildren);
    }

    [Fact]
    public void SchemaNode_optional_fields_default_to_null()
    {
        var path = new SchemaPath("conn");
        var node = new SchemaNode("id1", "t", SchemaNodeKind.View, path, false);

        Assert.Null(node.LegacyObjectId);
        Assert.Null(node.ProviderKind);
        Assert.Null(node.DisplayName);
    }

    [Fact]
    public void SchemaNode_with_all_fields()
    {
        var path = new SchemaPath("conn");
        var node = new SchemaNode("id1", "t", SchemaNodeKind.View, path, false,
            LegacyObjectId: 42, ProviderKind: "Netezza", DisplayName: "My View", Description: "A view", Owner: "admin");

        Assert.Equal(42, node.LegacyObjectId);
        Assert.Equal("Netezza", node.ProviderKind);
        Assert.Equal("My View", node.DisplayName);
        Assert.Equal("A view", node.Description);
        Assert.Equal("admin", node.Owner);
    }

    // ── SchemaSearchRequest ──

    [Fact]
    public void SchemaSearchRequest_defaults()
    {
        var req = new SchemaSearchRequest("SELECT");
        Assert.Equal("SELECT", req.Query);
        Assert.Null(req.Connection);
        Assert.False(req.IncludeColumns);
        Assert.Equal(1_000, req.MaxResults);
    }

    // ── SchemaDdlRequest ──

    [Fact]
    public void SchemaDdlRequest_creates_correctly()
    {
        var path = new SchemaPath("conn");
        var node = new SchemaNode("id1", "t", SchemaNodeKind.Table, path, false);
        var req = new SchemaDdlRequest(node, SchemaDdlKind.SelectTop);

        Assert.Same(node, req.Node);
        Assert.Equal(SchemaDdlKind.SelectTop, req.Kind);
    }

    // ── SchemaReference ──

    [Fact]
    public void SchemaReference_creates_correctly()
    {
        var reference = new SchemaReference("users", SchemaNodeKind.Table, 42, "db", "public");
        Assert.Equal("users", reference.Name);
        Assert.Equal(SchemaNodeKind.Table, reference.Kind);
        Assert.Equal(42, reference.Position);
        Assert.Equal("db", reference.Database);
        Assert.Equal("public", reference.Schema);
    }

    // ── Enum values ──

    [Fact]
    public void SchemaNodeKind_has_expected_values()
    {
        Assert.Equal(0, (int)SchemaNodeKind.Connection);
        Assert.Equal(1, (int)SchemaNodeKind.Database);
        Assert.Equal(15, (int)SchemaNodeKind.Unknown);
    }

    [Fact]
    public void SchemaDdlKind_has_expected_values()
    {
        Assert.Equal(0, (int)SchemaDdlKind.Create);
        Assert.Equal(1, (int)SchemaDdlKind.SelectTop);
        Assert.Equal(2, (int)SchemaDdlKind.AddCode);
    }
}
