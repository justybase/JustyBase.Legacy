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

public sealed class LegacySchemaRepositoryPostgresTreeTests
{
    private sealed class FakePostgresDb : GeneralDb
    {
        [SetsRequiredMembers]
        public FakePostgresDb()
            : base(
                Substitute.For<IDatabaseRuntimeContext>(),
                Substitute.For<ILogger>(),
                Substitute.For<IImportExportTasks>(),
                Substitute.For<IGeneralDbService>())
        {
            LogErrorStdColor = Color.Empty;
            DefaultDatabaseName = "DEV_DB";
            DatabaseList = ["DEV_DB"];
            _users = BuildSchemas("public", "reporting");
            tables = BuildCatalog(
                ("public", "orders"),
                ("reporting", "sales"));
            views = BuildCatalog(("public", "vw_orders"));
            procedures = BuildRoutines(("public", "sp_get_orders"));
        }

        public DataTable _users;

        public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.Postgres;

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
                (new[] { "Id", "Amount" }, Array.Empty<string>(), Array.Empty<short>(), Array.Empty<string>());
        }

        private static DataTable BuildSchemas(params string[] schemas)
        {
            var table = new DataTable();
            table.Columns.Add("table_schema", typeof(string));
            foreach (string schema in schemas)
            {
                table.Rows.Add(schema);
            }

            return table;
        }

        private static DataTable BuildCatalog(params (string Schema, string Name)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("table_schema", typeof(string));
            table.Columns.Add("tableName", typeof(string));
            foreach ((string schema, string name) in rows)
            {
                table.Rows.Add(schema, name);
            }

            return table;
        }

        private static DataTable BuildRoutines(params (string Schema, string Name)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("routine_schema", typeof(string));
            table.Columns.Add("routine_name", typeof(string));
            foreach ((string schema, string name) in rows)
            {
                table.Rows.Add(schema, name);
            }

            return table;
        }
    }

    private static LegacySchemaRepository CreateRepository(FakePostgresDb database, string connectionName)
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
    public async Task Postgres_connection_expands_to_databases()
    {
        var repository = CreateRepository(new FakePostgresDb(), "pg-dev");

        IReadOnlyList<SchemaNode> children = await repository.GetChildrenAsync(ConnectionNode("pg-dev"));

        Assert.Contains(children, node => node.Kind == SchemaNodeKind.Database && node.Name == "DEV_DB");
    }

    [Fact]
    public async Task Postgres_database_expands_to_schemas()
    {
        var repository = CreateRepository(new FakePostgresDb(), "pg-dev");
        var databaseNode = new SchemaNode("pg-dev/DEV_DB", "DEV_DB", SchemaNodeKind.Database,
            new SchemaPath("pg-dev", "DEV_DB"), true);

        IReadOnlyList<SchemaNode> schemas = await repository.GetChildrenAsync(databaseNode);

        Assert.Contains(schemas, node => node.Kind == SchemaNodeKind.Schema && node.Name == "public");
        Assert.Contains(schemas, node => node.Kind == SchemaNodeKind.Schema && node.Name == "reporting");
    }

    [Fact]
    public async Task Postgres_schema_expands_to_object_groups()
    {
        var repository = CreateRepository(new FakePostgresDb(), "pg-dev");
        var schemaNode = new SchemaNode("pg-dev/DEV_DB/public", "public", SchemaNodeKind.Schema,
            new SchemaPath("pg-dev", "DEV_DB", "public"), true);

        IReadOnlyList<SchemaNode> groups = await repository.GetChildrenAsync(schemaNode);

        Assert.Equal(3, groups.Count);
        Assert.Equal("Tables", groups[0].Name);
        Assert.Equal("Views", groups[1].Name);
        Assert.Equal("Procedures", groups[2].Name);
    }

    [Fact]
    public async Task Postgres_object_group_expands_to_objects_of_that_kind()
    {
        var repository = CreateRepository(new FakePostgresDb(), "pg-dev");
        var schemaNode = new SchemaNode("pg-dev/DEV_DB/public", "public", SchemaNodeKind.Schema,
            new SchemaPath("pg-dev", "DEV_DB", "public"), true);
        IReadOnlyList<SchemaNode> groups = await repository.GetChildrenAsync(schemaNode);

        var tablesGroup = Assert.Single(groups, group => group.Name == "Tables");
        IReadOnlyList<SchemaNode> tables = await repository.GetChildrenAsync(tablesGroup);
        Assert.Single(tables);
        Assert.Equal(SchemaNodeKind.Table, tables[0].Kind);
        Assert.Equal("orders", tables[0].Name);

        var proceduresGroup = Assert.Single(groups, group => group.Name == "Procedures");
        IReadOnlyList<SchemaNode> procedures = await repository.GetChildrenAsync(proceduresGroup);
        Assert.Single(procedures);
        Assert.Equal("sp_get_orders", procedures[0].Name);
    }

    [Fact]
    public async Task Postgres_table_expands_to_columns()
    {
        var repository = CreateRepository(new FakePostgresDb(), "pg-dev");
        var tableNode = new SchemaNode("pg-dev/DEV_DB/public/orders", "orders", SchemaNodeKind.Table,
            new SchemaPath("pg-dev", "DEV_DB", "public", "orders"), true);

        IReadOnlyList<SchemaNode> columns = await repository.GetChildrenAsync(tableNode);

        Assert.Contains(columns, node => node.Kind == SchemaNodeKind.Column && node.Name == "Id");
        Assert.Contains(columns, node => node.Kind == SchemaNodeKind.Column && node.Name == "Amount");
    }
}
