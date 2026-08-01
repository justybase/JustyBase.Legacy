using JustData.Application.Schema;

namespace JustData.ViewModels.Tests.Sql;

public sealed class SqlOutlineParserTests
{
    [Fact]
    public void Select_is_parsed_as_ast_outline()
    {
        SqlOutline outline = SqlOutlineParser.Parse("SELECT * FROM orders");
        Assert.NotEmpty(outline.Nodes);
        Assert.Equal(OutlineNodeKind.Select, outline.Nodes[0].Kind);
        Assert.Contains(outline.Nodes[0].Children, n => n.Name.Equals("orders", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cte_and_join_are_nested_under_select()
    {
        SqlOutline outline = SqlOutlineParser.Parse("WITH sales AS (SELECT * FROM orders JOIN customers ON orders.id=customers.id) SELECT * FROM sales");
        Assert.Equal(OutlineNodeKind.Select, outline.Nodes[0].Kind);
        OutlineNode cte = Assert.Single(outline.Nodes[0].Children, n => n.Kind == OutlineNodeKind.Cte);
        Assert.Contains(cte.Children.SelectMany(n => n.Children), n => n.Kind == OutlineNodeKind.Join);
    }

    [Fact]
    public void Multiple_statements_keep_source_order()
    {
        SqlOutline outline = SqlOutlineParser.Parse("SELECT * FROM orders; SELECT * FROM customers");
        Assert.True(outline.Nodes.Count >= 2);
        Assert.True(outline.Nodes[0].Position < outline.Nodes[1].Position);
    }

    [Fact]
    public void Cte_definition_positions_point_to_their_names()
    {
        const string sql = "WITH CTE1 AS (\n    SELECT * FROM DIMDATE\n)\n, CTE2 AS (\n    SELECT * FROM DIMDATE\n)\n\nSELECT * FROM CTE1 C\nWHERE C.CALENDARQUARTER > 0";
        SqlOutline outline = SqlOutlineParser.Parse(sql);
        OutlineNode select = Assert.Single(outline.Nodes);
        OutlineNode cte1 = Assert.Single(select.Children, node => node.Kind == OutlineNodeKind.Cte && node.Name.Equals("CTE1", StringComparison.OrdinalIgnoreCase));
        OutlineNode cte2 = Assert.Single(select.Children, node => node.Kind == OutlineNodeKind.Cte && node.Name.Equals("CTE2", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(sql.IndexOf("CTE1", StringComparison.Ordinal), cte1.Position);
        Assert.Equal(sql.IndexOf("CTE2", StringComparison.Ordinal), cte2.Position);
    }
}
