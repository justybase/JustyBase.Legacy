using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Postgres;
using Npgsql;
using NSubstitute;
using System.Data;
using System.Data.Common;
using System.Drawing;

namespace AppBase.Tests.Database;

public sealed class PostgresDbConnectionTests
{
    private static Postgres CreateDb(string connectionString, IGeneralDbService? service = null)
    {
        service ??= Substitute.For<IGeneralDbService>();
        return new Postgres(
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<ILogger>(),
            Substitute.For<IImportExportTasks>(),
            service)
        {
            LogErrorStdColor = Color.Empty,
            ConnectionString = connectionString
        };
    }

    private const string ConnectionString =
        "Host=postgres.local;Port=5432;Username=test-user;Password=test-password;Database=DEV_DB";

    [Fact]
    public void GetConnection_with_databaseName_sets_database()
    {
        Postgres db = CreateDb(ConnectionString);

        using DbConnection connection = db.GetConnection("OTHER_DB");

        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal("OTHER_DB", new NpgsqlConnectionStringBuilder(connection.ConnectionString).Database);
    }

    [Fact]
    public void GetConnection_without_databaseName_keeps_profile_database()
    {
        Postgres db = CreateDb(ConnectionString);

        using DbConnection connection = db.GetConnection();

        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal("DEV_DB", new NpgsqlConnectionStringBuilder(connection.ConnectionString).Database);
    }

    [Fact]
    public void GetConnection_with_blank_databaseName_falls_back_to_parameterless()
    {
        Postgres db = CreateDb(ConnectionString);

        using DbConnection connection = db.GetConnection(string.Empty);

        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal("DEV_DB", new NpgsqlConnectionStringBuilder(connection.ConnectionString).Database);
    }

    [Fact]
    public void ResetDynamicCollection_quotes_reserved_words_and_weird_names_only()
    {
        var service = Substitute.For<IGeneralDbService>();
        service.ReservedWords.Returns(new HashSet<string> { "SELECT", "ORDER", "FROM" });

        Postgres db = CreateDb(ConnectionString, service);
        db._users = BuildSchemas("public", "reporting");
        db.tables = BuildCatalog(
            ("public", "orders"),
            ("public", "order"),
            ("public", "order details"));
        db.views = BuildCatalog(("public", "vw_orders"));

        db.ResetDynamicCollection();

        Assert.Contains("public.orders", db.AutocompleteSuggestions.TwoWords);
        Assert.Contains("public.\"order\"", db.AutocompleteSuggestions.TwoWords);
        Assert.Contains("public.\"order details\"", db.AutocompleteSuggestions.TwoWords);
        Assert.Contains("public.vw_orders", db.AutocompleteSuggestions.TwoWords);
        Assert.DoesNotContain(db.AutocompleteSuggestions.TwoWords, item => item.Contains("\"orders\"", StringComparison.Ordinal));
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
}
