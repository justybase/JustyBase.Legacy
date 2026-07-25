using AppBase.Data.Completion;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Ast;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Visitor;

namespace AppBase.Tests.Sql;

public sealed class NetezzaCompletionMapperTests
{
    [Fact]
    public void InvalidateSchema_BumpsEpochAndRaisesInvalidation()
    {
        var services = new NetezzaSqlCompletionServices();
        var invalidated = false;
        services.SchemaInvalidated += () => invalidated = true;
        services.SchemaProvider.AddTable(new TableInfo("EMPLOYEES"));
        var previousEpoch = services.SchemaProvider.MetadataEpoch;

        services.InvalidateSchema();

        Assert.True(invalidated);
        Assert.True(services.SchemaProvider.MetadataEpoch > previousEpoch);
        Assert.False(services.SchemaProvider.HasTables());
    }

    [Fact]
    public void MapEngineItems_QualifiesColumnWithCurrentFragment()
    {
        var items = FctbCompletionMapper.MapEngineItems(
                [new CompletionItem("EMPLOYEE_ID", CompletionKind.Column, "INT4")],
                "e.",
                schema: null!)
            .ToList();

        var item = Assert.Single(items);
        Assert.Equal("EMPLOYEE_ID", item.ToString());
        Assert.Equal("e.EMPLOYEE_ID", item.GetTextForReplace());
        Assert.Equal("INT4", item.ToolTipTitle);
    }

    [Fact]
    public void MapDatabaseDoubleDotTables_FiltersAndQualifiesTables()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo("EMPLOYEES", "ADMIN", "TESTDB"));
        schema.AddTable(new TableInfo("ORDERS", "ADMIN", "TESTDB"));
        schema.AddTable(new TableInfo("EMPLOYEES", "ADMIN", "OTHERDB"));

        var items = FctbCompletionMapper.MapDatabaseDoubleDotTables("TESTDB..EMP", schema);

        var item = Assert.Single(items!);
        Assert.Equal("EMPLOYEES", item.ToString());
        Assert.Equal("TESTDB..EMPLOYEES", item.GetTextForReplace());
        Assert.Equal("Table", item.ToolTipTitle);
    }

    [Fact]
    public void MapEngineItems_DoesNotDuplicateQualifiedLabels()
    {
        var items = FctbCompletionMapper.MapEngineItems(
                [
                    new CompletionItem("EMPLOYEE_ID", CompletionKind.Column),
                    new CompletionItem("EMPLOYEE_ID", CompletionKind.Column)
                ],
                "e.",
                schema: null!)
            .ToList();

        var item = Assert.Single(items);
        Assert.Equal("EMPLOYEE_ID", item.ToString());
        Assert.Equal("e.EMPLOYEE_ID", item.GetTextForReplace());
    }

    [Fact]
    public void MapEngineItems_ShowsColumnTypeAndDescription()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo(
            "FACT_SALES_2",
            "ADMIN",
            "JUST_DATA_2",
            Columns: [new ColumnInfo("PRODUCT_ID", DataType: "INTEGER")]));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2",
                "ADMIN",
                "JUST_DATA_2",
                Columns: [new NetezzaSchemaColumn(
                    "PRODUCT_ID", "INTEGER", Description: "Product identifier")],
                Description: "Sales fact table")]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "FS.PRODUCT_ID")],
            "FS.", schema, metadata));

        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Product identifier", item.ToolTipText);
    }

    [Fact]
    public void MapEngineItems_ShowsTableDescription()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo("FACT_SALES_2", "ADMIN", "JUST_DATA_2"));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2", "ADMIN", "JUST_DATA_2",
                Description: "Sales fact table")]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("FACT_SALES_2", CompletionKind.Table)],
            "", schema, metadata));

        Assert.Equal("Table", item.ToolTipTitle);
        Assert.Equal("Sales fact table", item.ToolTipText);
    }

    [Fact]
    public void MapEngineItems_AssignsIconsAndInlineMetadataByKind()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo(
            "FACT_SALES_2",
            "ADMIN",
            "JUST_DATA_2",
            Columns: [new ColumnInfo("PRODUCT_ID", DataType: "INTEGER")]));
        schema.AddTable(new TableInfo("SALES_VIEW", "ADMIN", "JUST_DATA_2", IsView: true));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2", "ADMIN", "JUST_DATA_2",
                Description: "Sales fact table",
                Columns: [new NetezzaSchemaColumn("PRODUCT_ID", "INTEGER", Description: "Product identifier")]),
            new NetezzaSchemaTable("SALES_VIEW", "ADMIN", "JUST_DATA_2", IsView: true)]);

        var items = FctbCompletionMapper.MapEngineItems(
            [
                new CompletionItem("FACT_SALES_2", CompletionKind.Table),
                new CompletionItem("SALES_VIEW", CompletionKind.View),
                new CompletionItem("PRODUCT_ID", CompletionKind.Column, "FACT_SALES_2.PRODUCT_ID")
            ],
            "", schema, metadata).ToList();

        var table = Assert.Single(items, item => item.ToString() == "FACT_SALES_2");
        var view = Assert.Single(items, item => item.ToString() == "SALES_VIEW");
        var column = Assert.Single(items, item => item.ToString() == "PRODUCT_ID");

        Assert.Equal((int)CompletionIconKind.Table, table.ImageIndex);
        Assert.Equal("Table", table.DetailText);
        Assert.Equal((int)CompletionIconKind.View, view.ImageIndex);
        Assert.Equal("View", view.DetailText);
        Assert.Equal((int)CompletionIconKind.Column, column.ImageIndex);
        Assert.Equal("INTEGER", column.DetailText);
        Assert.Equal("Product identifier", column.DescriptionText);
    }

    [Fact]
    public void MapEngineItems_ResolvesAliasForColumnMetadata()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo(
            "FACT_SALES_2",
            "ADMIN",
            "JUST_DATA_2",
            Columns: [new ColumnInfo("PRODUCT_ID", DataType: "INTEGER")]));
        schema.AddTable(new TableInfo(
            "OTHER_SALES",
            "ADMIN",
            "JUST_DATA_2",
            Columns: [new ColumnInfo("PRODUCT_ID", DataType: "VARCHAR(20)")]));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2", "ADMIN", "JUST_DATA_2",
                Columns: [new NetezzaSchemaColumn("PRODUCT_ID", "INTEGER", Description: "Product identifier")]),
            new NetezzaSchemaTable(
                "OTHER_SALES", "ADMIN", "JUST_DATA_2",
                Columns: [new NetezzaSchemaColumn("PRODUCT_ID", "VARCHAR(20)", Description: "Other product")])]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "S.PRODUCT_ID")],
            "S.PRO",
            schema,
            metadata,
            "SELECT * FROM JUST_DATA_2..FACT_SALES_2 S JOIN OTHER_SALES O ON S.PRODUCT_ID = O.PRODUCT_ID WHERE S.PRO"));

        Assert.Equal("INTEGER", item.DetailText);
        Assert.Equal("Product identifier", item.DescriptionText);
        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Product identifier", item.ToolTipText);
    }
}
