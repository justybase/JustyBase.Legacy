using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;
using JustData.Application.Schema;
using JustyBaseLegacy.UI.Schema;
using NSubstitute;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace AppBase.Tests.JustData.Schema;

public sealed class LegacySchemaRepositoryMsSqlTreeTests
{
    private sealed class FakeMsSqlDb : GeneralDb
    {
        [SetsRequiredMembers]
        public FakeMsSqlDb()
            : base(
                Substitute.For<IDatabaseRuntimeContext>(),
                Substitute.For<ILogger>(),
                Substitute.For<IImportExportTasks>(),
                Substitute.For<IGeneralDbService>())
        {
            LogErrorStdColor = Color.Empty;
            DefaultDatabaseName = "DEV_DB";
            DatabaseList = ["DEV_DB"];
            tables = BuildCatalog("TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME",
                ("Dev_Db", "dbo", "Orders"));
            views = BuildCatalog("TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME",
                ("Dev_Db", "dbo", "OrdersView"),
                ("Dev_Db", "sales", "TopCustomers"));
            procedures = BuildCatalog("ROUTINE_CATALOG", "ROUTINE_SCHEMA", "ROUTINE_NAME",
                ("Dev_Db", "dbo", "sp_GetOrders"));
        }

        public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.MsSqlDb;

        public bool InitDbCalled { get; private set; }

        public override void InitDb()
        {
            InitDbCalled = true;
            base.InitDb();
        }

        public override void ResetDynamicCollection()
        {
        }

        public override string SearchInViewsSource(string txtToSearch) => string.Empty;

        public override string SearchInProcedureSource(string txtToSearch) => string.Empty;

        public override DbConnection GetConnection() => throw new NotSupportedException();

        public override DbConnection GetConnection(string databaseName, bool usePool = true) => throw new NotSupportedException();

        protected override void AddToCache(string dbname, string schema, string tablename)
        {
            columnsOfTables[dbname + "_" + schema + "\\" + tablename] =
                (new[] { "OrderId", "Amount" }, Array.Empty<string>(), Array.Empty<short>(), Array.Empty<string>());
        }

        private static DataTable BuildCatalog(string catalogColumn, string schemaColumn, string nameColumn, params (string Catalog, string Schema, string Name)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add(catalogColumn, typeof(string));
            table.Columns.Add(schemaColumn, typeof(string));
            table.Columns.Add(nameColumn, typeof(string));
            foreach ((string catalog, string schema, string name) in rows)
            {
                table.Rows.Add(catalog, schema, name);
            }

            return table;
        }
    }

    private static LegacySchemaRepository CreateRepository(FakeMsSqlDb database, string connectionName)
    {
        var sessions = new ConnectionSessionRegistry();
        sessions.Set(connectionName, database);

        return new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            Substitute.For<IConnectionProfileCatalog>());
    }

    private static SchemaNode ConnectionNode(string connectionName) =>
        new(connectionName, connectionName, SchemaNodeKind.Connection, new SchemaPath(connectionName), true);

    [Fact]
    public async Task MsSql_connection_expands_to_databases()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");

        IReadOnlyList<SchemaNode> children = await repository.GetChildrenAsync(ConnectionNode("mssql-dev"));

        Assert.Contains(children, node => node.Kind == SchemaNodeKind.Database && node.Name == "DEV_DB");
    }

    [Fact]
    public async Task MsSql_database_expands_to_schemas_of_that_database()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");
        var databaseNode = new SchemaNode("mssql-dev/DEV_DB", "DEV_DB", SchemaNodeKind.Database,
            new SchemaPath("mssql-dev", "DEV_DB"), true);

        IReadOnlyList<SchemaNode> schemas = await repository.GetChildrenAsync(databaseNode);

        Assert.Contains(schemas, node => node.Kind == SchemaNodeKind.Schema && node.Name == "dbo");
        Assert.Contains(schemas, node => node.Kind == SchemaNodeKind.Schema && node.Name == "sales");
    }

    [Fact]
    public async Task MsSql_schema_expands_to_object_groups()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");
        var schemaNode = new SchemaNode("mssql-dev/DEV_DB/dbo", "dbo", SchemaNodeKind.Schema,
            new SchemaPath("mssql-dev", "DEV_DB", "dbo"), true);

        IReadOnlyList<SchemaNode> groups = await repository.GetChildrenAsync(schemaNode);

        Assert.Equal(3, groups.Count);
        Assert.Equal("Tables", groups[0].Name);
        Assert.Equal("Views", groups[1].Name);
        Assert.Equal("Procedures", groups[2].Name);
        Assert.All(groups, group => Assert.Equal(SchemaNodeKind.ObjectGroup, group.Kind));
    }

    [Fact]
    public async Task MsSql_object_group_expands_to_objects_of_that_kind()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");
        var schemaNode = new SchemaNode("mssql-dev/DEV_DB/dbo", "dbo", SchemaNodeKind.Schema,
            new SchemaPath("mssql-dev", "DEV_DB", "dbo"), true);
        IReadOnlyList<SchemaNode> groups = await repository.GetChildrenAsync(schemaNode);

        var tablesGroup = Assert.Single(groups, group => group.Name == "Tables");
        IReadOnlyList<SchemaNode> tables = await repository.GetChildrenAsync(tablesGroup);
        var table = Assert.Single(tables);
        Assert.Equal(SchemaNodeKind.Table, table.Kind);
        Assert.Equal("Orders", table.Name);

        var viewsGroup = Assert.Single(groups, group => group.Name == "Views");
        IReadOnlyList<SchemaNode> views = await repository.GetChildrenAsync(viewsGroup);
        Assert.Single(views);
        Assert.Equal("OrdersView", views[0].Name);

        var proceduresGroup = Assert.Single(groups, group => group.Name == "Procedures");
        IReadOnlyList<SchemaNode> procedures = await repository.GetChildrenAsync(proceduresGroup);
        Assert.Single(procedures);
        Assert.Equal("sp_GetOrders", procedures[0].Name);
    }

    [Fact]
    public async Task MsSql_schema_does_not_leak_objects_from_other_databases()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");
        var schemaNode = new SchemaNode("mssql-dev/DEV_DB/sales", "sales", SchemaNodeKind.Schema,
            new SchemaPath("mssql-dev", "DEV_DB", "sales"), true);

        IReadOnlyList<SchemaNode> groups = await repository.GetChildrenAsync(schemaNode);

        var viewsGroup = Assert.Single(groups);
        Assert.Equal("Views", viewsGroup.Name);
        IReadOnlyList<SchemaNode> objects = await repository.GetChildrenAsync(viewsGroup);
        Assert.Single(objects);
        Assert.Equal("TopCustomers", objects[0].Name);
    }

    [Fact]
    public async Task MsSql_table_expands_to_columns()
    {
        var repository = CreateRepository(new FakeMsSqlDb(), "mssql-dev");
        var tableNode = new SchemaNode("mssql-dev/DEV_DB/dbo/Orders", "Orders", SchemaNodeKind.Table,
            new SchemaPath("mssql-dev", "DEV_DB", "dbo", "Orders"), true);

        IReadOnlyList<SchemaNode> columns = await repository.GetChildrenAsync(tableNode);

        Assert.Contains(columns, node => node.Kind == SchemaNodeKind.Column && node.Name == "OrderId");
        Assert.Contains(columns, node => node.Kind == SchemaNodeKind.Column && node.Name == "Amount");
    }

    [Fact]
    public async Task MsSql_connection_without_session_defers_initialization_to_refresh()
    {
        var fakeDb = new FakeMsSqlDb { DatabaseList = ["DEV_DB", "ANOTHER_DB"] };
        var sessions = new ConnectionSessionRegistry();
        var service = Substitute.For<IGeneralDbService>();
        service.GetGeneralDb(
                Arg.Any<IDatabaseRuntimeContext>(),
                Arg.Any<ILogger>(),
                Arg.Any<IImportExportTasks>(),
                "mssql-dev",
                out _)
            .Returns(callInfo =>
            {
                callInfo[4] = "DEV_DB";
                return fakeDb;
            });
        var profile = new ConnectionProfile { Database = "master" };
        var profiles = Substitute.For<IConnectionProfileCatalog>();
        profiles.TryGetProfile("mssql-dev", out _)
            .Returns(callInfo =>
            {
                callInfo[1] = profile;
                return true;
            });

        var repository = new LegacySchemaRepository(
            service,
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            profiles,
            Substitute.For<ILogger>(),
            Substitute.For<IImportExportTasks>());

        IReadOnlyList<SchemaNode> children = await repository.GetChildrenAsync(ConnectionNode("mssql-dev"));

        Assert.Empty(children);
        Assert.False(sessions.ContainsKey("mssql-dev"));
        Assert.False(fakeDb.InitDbCalled);
    }

    [Fact]
    public async Task MsSql_connection_without_session_and_services_defers_to_refresh()
    {
        var sessions = new ConnectionSessionRegistry();
        var profile = new ConnectionProfile { Database = "master" };
        var profiles = Substitute.For<IConnectionProfileCatalog>();
        profiles.TryGetProfile("mssql-dev", out _)
            .Returns(callInfo =>
            {
                callInfo[1] = profile;
                return true;
            });

        var repository = new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            profiles);

        IReadOnlyList<SchemaNode> children = await repository.GetChildrenAsync(ConnectionNode("mssql-dev"));

        Assert.Empty(children);
        Assert.False(sessions.ContainsKey("mssql-dev"));
    }
}
