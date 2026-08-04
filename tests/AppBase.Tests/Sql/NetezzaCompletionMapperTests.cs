using AppBase.Common.Enums;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Ast;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Dialects;
using JustyBase.NetezzaSqlParser.Visitor;
using NSubstitute;
using FastColoredTextBoxNS;

namespace AppBase.Tests.Sql;

public sealed class NetezzaCompletionMapperTests
{
    private static NetezzaSqlCompletionServices CreateServices()
    {
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(new Dictionary<string, Dictionary<int, AppBase.Data.Core.Models.NetezzaTableInfo>>());
        return new NetezzaSqlCompletionServices(catalog);
    }

    [Fact]
    public void InvalidateSchema_BumpsEpochAndRaisesInvalidation()
    {
        var services = CreateServices();
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
    public void Relation_completion_prioritizes_schema_names_after_from()
    {
        var table = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_ORDERS"),
            CompletionIconKind.Table,
            "Table");
        var schema = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE"),
            CompletionIconKind.Schema,
            "Schema");

        IReadOnlyList<AutocompleteItem> result = FctbCompletionMapper
            .PrioritizeSchemasForRelationContext([table, schema], "SELECT * FROM ", "SELECT * FROM ".Length);

        Assert.Equal(["JBL_LIVE", "JBL_ORDERS"], result.Select(item => item.ToString()));
    }

    [Fact]
    public void Relation_completion_keeps_normal_order_outside_relation_context()
    {
        var table = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_ORDERS"),
            CompletionIconKind.Table,
            "Table");
        var schema = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE"),
            CompletionIconKind.Schema,
            "Schema");

        IReadOnlyList<AutocompleteItem> result = FctbCompletionMapper
            .PrioritizeSchemasForRelationContext([table, schema], "SELECT ", "SELECT ".Length);

        Assert.Equal(["JBL_ORDERS", "JBL_LIVE"], result.Select(item => item.ToString()));
    }

    [Fact]
    public void Qualified_relation_completion_orders_tables_views_nicknames_then_other_objects()
    {
        var other = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE.JBL_PROC"),
            CompletionIconKind.Function,
            "procedure");
        var nickname = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE.JBL_NICK"),
            CompletionIconKind.Alias,
            "db2nickname");
        var view = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE.JBL_VIEW"),
            CompletionIconKind.View,
            "View");
        var table = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("JBL_LIVE.JBL_TABLE"),
            CompletionIconKind.Table,
            "Table");

        IReadOnlyList<AutocompleteItem> result = FctbCompletionMapper
            .PrioritizeSchemasForRelationContext(
                [other, nickname, view, table],
                "SELECT * FROM JBL_LIVE.",
                "SELECT * FROM JBL_LIVE.".Length);

        Assert.Equal(
            ["JBL_TABLE", "JBL_VIEW", "JBL_NICK", "JBL_PROC"],
            result.Select(item => item.ToString()));
    }

    [Fact]
    public void EnsureDb2Schema_uses_shared_engine_for_schema_and_database_qualified_aliases()
    {
        var database = Substitute.For<IGeneralDb>();
        database.objectInSchema.Returns(new Dictionary<string, Dictionary<string, TypeInDatabase>>
        {
            ["JBL_LIVE"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["JBL_DEPARTMENTS"] = TypeInDatabase.table
            }
        });
        database.GetColumns("TESTDB", "JBL_LIVE", "JBL_DEPARTMENTS")
            .Returns(["DEPARTMENT_ID", "DEPARTMENT_NAME"]);

        var services = CreateServices();
        services.EnsureDb2Schema(database, "db2-cloud", "TESTDB");
        var engine = services.CreateEngine("db2-doc", SqlDialect.Db2);

        string schemaQualified = "SELECT * FROM JBL_LIVE.JBL_DEPARTMENTS A WHERE A.";
        string databaseQualified = "SELECT * FROM TESTDB.JBL_LIVE.JBL_DEPARTMENTS A WHERE A.";

        var schemaItems = engine.GetCompletions(schemaQualified, schemaQualified.Length);
        var databaseItems = engine.GetCompletions(databaseQualified, databaseQualified.Length);

        Assert.Contains(schemaItems, item => item.Label == "DEPARTMENT_ID" && item.Detail == "A.DEPARTMENT_ID");
        Assert.Contains(databaseItems, item => item.Label == "DEPARTMENT_NAME" && item.Detail == "A.DEPARTMENT_NAME");
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
    public void MapEngineItems_PrefersEngineDocumentationWithoutMetadata()
    {
        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "INTEGER", Documentation: "Product identifier")],
            "FS.",
            schema: null!));

        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Product identifier", item.ToolTipText);
        Assert.Equal("Product identifier", item.DescriptionText);
    }

    [Fact]
    public void MapEngineItems_PrefersEngineDocumentationOverMetadataDescription()
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
                    "PRODUCT_ID", "INTEGER", Description: "Metadata description")])]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "FS.PRODUCT_ID", Documentation: "Engine documentation")],
            "FS.", schema, metadata));

        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Engine documentation", item.ToolTipText);
    }

    [Fact]
    public void MapEngineItems_FallsBackToMetadataWhenDocumentationIsNull()
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
                    "PRODUCT_ID", "INTEGER", Description: "Metadata description")])]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "FS.PRODUCT_ID")],
            "FS.", schema, metadata));

        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Metadata description", item.ToolTipText);
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
    public void MapEngineItems_UsesSchemaProviderColumnDescription()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo(
            "FACT_SALES_2",
            "ADMIN",
            "JUST_DATA_2",
            Columns: [new ColumnInfo("PRODUCT_ID", DataType: "INTEGER", Description: "Provider description")]));

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("PRODUCT_ID", CompletionKind.Column, "FACT_SALES_2.PRODUCT_ID")],
            "FACT_SALES_2.", schema, metadata: null!));

        Assert.Equal("INTEGER", item.ToolTipTitle);
        Assert.Equal("Provider description", item.ToolTipText);
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
    public void MapEngineItems_PrefersEngineDocumentationForTableOverMetadata()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo("FACT_SALES_2", "ADMIN", "JUST_DATA_2"));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2", "ADMIN", "JUST_DATA_2",
                Description: "Metadata table description")]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("FACT_SALES_2", CompletionKind.Table, Documentation: "Engine table documentation")],
            "", schema, metadata));

        Assert.Equal("Table", item.ToolTipTitle);
        Assert.Equal("Engine table documentation", item.ToolTipText);
        Assert.Equal("Engine table documentation", item.DescriptionText);
    }

    [Fact]
    public void MapEngineItems_PrefersEngineDocumentationForViewWithoutMetadata()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo("SALES_VIEW", "ADMIN", "JUST_DATA_2", IsView: true));

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("SALES_VIEW", CompletionKind.View, Documentation: "View definition summary")],
            "", schema, metadata: null!));

        Assert.Equal("View", item.ToolTipTitle);
        Assert.Equal("View definition summary", item.ToolTipText);
    }

    [Fact]
    public void MapEngineItems_FallsBackToMetadataForTableWhenDocumentationIsEmpty()
    {
        var schema = new InMemorySchemaProvider();
        schema.AddTable(new TableInfo("FACT_SALES_2", "ADMIN", "JUST_DATA_2"));
        var metadata = new NetezzaSchemaSnapshot([
            new NetezzaSchemaTable(
                "FACT_SALES_2", "ADMIN", "JUST_DATA_2",
                Description: "Metadata table description")]);

        var item = Assert.Single(FctbCompletionMapper.MapEngineItems(
            [new CompletionItem("FACT_SALES_2", CompletionKind.Table, Documentation: "   ")],
            "", schema, metadata));

        Assert.Equal("Table", item.ToolTipTitle);
        Assert.Equal("Metadata table description", item.ToolTipText);
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
