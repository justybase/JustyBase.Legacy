using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.MsSqlDb;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System.Data;
using System.Data.Common;
using System.Drawing;

namespace AppBase.Tests.Database;

public sealed class MsSqlDbConnectionTests
{
    private static MsSqlDb CreateDb(string connectionString, IGeneralDbService? service = null)
    {
        service ??= Substitute.For<IGeneralDbService>();
        return new MsSqlDb(
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<ILogger>(),
            Substitute.For<IImportExportTasks>(),
            service)
        {
            LogErrorStdColor = Color.Empty,
            ConnectionString = connectionString
        };
    }

    [Fact]
    public void GetConnection_with_databaseName_sets_initial_catalog()
    {
        MsSqlDb db = CreateDb(
            "Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;TrustServerCertificate=True;");

        using DbConnection connection = db.GetConnection("OTHER_DB");

        Assert.IsType<SqlConnection>(connection);
        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
        Assert.Equal("OTHER_DB", builder.InitialCatalog);
        Assert.True(builder.TrustServerCertificate);
    }

    [Fact]
    public void GetConnection_without_databaseName_keeps_profile_catalog()
    {
        MsSqlDb db = CreateDb(
            "Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;");

        using DbConnection connection = db.GetConnection();

        Assert.IsType<SqlConnection>(connection);
        Assert.Equal("DEV_DB", new SqlConnectionStringBuilder(connection.ConnectionString).InitialCatalog);
    }

    [Fact]
    public void GetConnection_with_blank_databaseName_falls_back_to_parameterless()
    {
        MsSqlDb db = CreateDb(
            "Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;");

        using DbConnection connection = db.GetConnection(string.Empty);

        Assert.IsType<SqlConnection>(connection);
        Assert.Equal("DEV_DB", new SqlConnectionStringBuilder(connection.ConnectionString).InitialCatalog);
    }

    [Fact]
    public void ResetDynamicCollection_quotes_only_names_outside_mssql_identifier_pattern()
    {
        MsSqlDb db = CreateDb(
            "Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;");
        db._dtDatabases = new DataTable();
        db._schemas = new DataTable();
        db.tables = BuildCatalog(
            ("DEV_DB", "dbo", "orders"),
            ("DEV_DB", "dbo", "order details"),
            ("DEV_DB", "dbo", "_private1"));
        db.views = BuildCatalog(("DEV_DB", "dbo", "vw_orders"));
        db.procedures = BuildCatalog(("DEV_DB", "dbo", "sp_get"));

        db.ResetDynamicCollection();

        Assert.Contains("DEV_DB.dbo.orders", db.AutocompleteSuggestions.TreeWords);
        Assert.Contains("DEV_DB.dbo._private1", db.AutocompleteSuggestions.TreeWords);
        Assert.Contains("DEV_DB.dbo.\"order details\"", db.AutocompleteSuggestions.TreeWords);
        Assert.Contains("DEV_DB.dbo.vw_orders", db.AutocompleteSuggestions.TreeWords);
        Assert.DoesNotContain(db.AutocompleteSuggestions.TreeWords, item => item.Contains("\"orders\"", StringComparison.Ordinal));
    }

    [Fact]
    public void ResetDynamicCollection_quotes_reserved_words()
    {
        var service = Substitute.For<IGeneralDbService>();
        service.ReservedWords.Returns(new HashSet<string> { "ORDER", "USER" });

        MsSqlDb db = CreateDb(
            "Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;",
            service);
        db._dtDatabases = new DataTable();
        db._schemas = new DataTable();
        db.tables = BuildCatalog(
            ("DEV_DB", "dbo", "orders"),
            ("DEV_DB", "dbo", "order"));
        db.views = new DataTable();
        db.procedures = new DataTable();

        db.ResetDynamicCollection();

        Assert.Contains("DEV_DB.dbo.orders", db.AutocompleteSuggestions.TreeWords);
        Assert.Contains("DEV_DB.dbo.\"order\"", db.AutocompleteSuggestions.TreeWords);
    }

    private static DataTable BuildCatalog(params (string Catalog, string Schema, string Name)[] rows)
    {
        var table = new DataTable();
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        foreach ((string catalog, string schema, string name) in rows)
        {
            table.Rows.Add(catalog, schema, name);
        }

        return table;
    }
}
