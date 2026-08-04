using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Models;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacyDbCompletionFallbackTests : IDisposable
{
    private const string ConnectionName = "fallback-test";
    private readonly Dictionary<string, Dictionary<int, NetezzaTableInfo>> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly LegacyDbCompletionFallback _sut;
    private readonly INetezzaCompletionContext _context;
    private readonly IGeneralDbService _db;

    public LegacyDbCompletionFallbackTests()
    {
        _context = Substitute.For<INetezzaCompletionContext>();
        _db = Substitute.For<IGeneralDbService>();
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(_tables);
        _sut = new LegacyDbCompletionFallback(_context, _db, catalog);
        SeedHappyPath();
    }

    public void Dispose()
    {
        _sut.ResetCache();
        _tables.Remove(ConnectionName);
    }

    [Fact]
    public void Constructor_rejects_null_context()
    {
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        Assert.Throws<ArgumentNullException>(() => new LegacyDbCompletionFallback(null!, _db, catalog));
    }

    [Fact]
    public void GetCompletions_returns_empty_when_schema_not_refreshed()
    {
        _context.SchemaRefreshed.Returns(false);

        Assert.Empty(_sut.GetCompletions("EMP"));
    }

    [Fact]
    public void GetCompletions_returns_empty_for_non_netezza_driver()
    {
        _db.DriverName(ConnectionName).Returns("Postgres");

        Assert.Empty(_sut.GetCompletions("EMP"));
    }

    [Fact]
    public void GetCompletions_dotCount0_returns_databases_and_matching_tables()
    {
        var items = _sut.GetCompletions("EMP").ToList();

        Assert.Contains(items, i => i.ToString() == "JUST_DATA");
        Assert.Contains(items, i => i.ToString() == "EMPLOYEES");
        Assert.DoesNotContain(items, i => i.ToString() == "ORDERS");
    }

    [Fact]
    public void GetCompletions_one_dot_after_schema_returns_tables()
    {
        var items = _sut.GetCompletions("ADMIN.").ToList();

        Assert.Contains(items, i => i.ToString()!.Contains("EMPLOYEES", StringComparison.Ordinal));
        Assert.Contains(items, i => i.ToString()!.Contains("ORDERS", StringComparison.Ordinal));
    }

    [Fact]
    public void GetCompletions_one_dot_after_table_returns_columns()
    {
        var items = _sut.GetCompletions("EMPLOYEES.").ToList();

        Assert.Contains(items, i => i.ToString()!.Contains("EMP_ID", StringComparison.Ordinal));
        Assert.Contains(items, i => i.ToString()!.Contains("EMP_NAME", StringComparison.Ordinal));
    }

    [Fact]
    public void GetCompletions_two_dots_database_schema_returns_tables()
    {
        var items = _sut.GetCompletions("JUST_DATA.ADMIN.").ToList();

        Assert.Contains(items, i => i.ToString()!.Contains("EMPLOYEES", StringComparison.Ordinal));
        Assert.Contains(items, i => i.ToString()!.Contains("ORDERS", StringComparison.Ordinal));
    }

    [Fact]
    public void ResetCache_clears_instance_caches()
    {
        _sut.ResetCache();
        Assert.NotNull(_sut.GetCompletions("EMP"));
    }

    [Fact]
    public void GetCompletions_db2_uses_the_requested_connection_and_schema()
    {
        const string db2Connection = "db2-cloud";
        var db2 = Substitute.For<IGeneralDb>();
        db2.DatabaseType.Returns(DatabaseTypeEnum.DB2);
        db2.DefaultDatabaseName.Returns("TESTDB");
        db2.objectInSchema.Returns(new Dictionary<string, Dictionary<string, TypeInDatabase>>
        {
            ["JBL_LIVE"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["JBL_ORDERS"] = TypeInDatabase.table,
                ["JBL_VIEW"] = TypeInDatabase.view
            },
            ["OTHER_SCHEMA"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["OTHER_TABLE"] = TypeInDatabase.table
            }
        });
        db2.GetColumns("TESTDB", "JBL_LIVE", "JBL_ORDERS")
            .Returns(["ORDER_ID", "CUSTOMER_ID"]);
        _db.DriverName(db2Connection).Returns("DB2");

        var sessions = new ConnectionSessionRegistry();
        sessions.Set(db2Connection, db2);
        var db2Fallback = new LegacyDbCompletionFallback(_context, _db, Substitute.For<INetezzaSchemaTableCatalog>(), sessions);

        var objects = db2Fallback.GetCompletions("JBL_LIVE.", db2Connection, "TESTDB").ToList();
        var columns = db2Fallback.GetCompletions("JBL_LIVE.JBL_ORDERS.", db2Connection, "TESTDB").ToList();
        var schemaAliasColumns = db2Fallback.GetCompletions(
            "A.",
            db2Connection,
            "TESTDB",
            "SELECT * FROM JBL_LIVE.JBL_ORDERS A WHERE A.").ToList();
        var databaseAliasColumns = db2Fallback.GetCompletions(
            "A.",
            db2Connection,
            "TESTDB",
            "SELECT * FROM TESTDB.JBL_LIVE.JBL_ORDERS A WHERE A.").ToList();

        Assert.Contains(objects, item => item.ToString()!.Contains("JBL_ORDERS", StringComparison.Ordinal));
        Assert.Contains(objects, item => item.ToString()!.Contains("JBL_VIEW", StringComparison.Ordinal));
        Assert.DoesNotContain(objects, item => item.ToString()!.Contains("OTHER_TABLE", StringComparison.Ordinal));
        Assert.Contains(columns, item => item.ToString()!.Contains("ORDER_ID", StringComparison.Ordinal));
        Assert.Contains(columns, item => item.ToString()!.Contains("CUSTOMER_ID", StringComparison.Ordinal));
        Assert.Contains(schemaAliasColumns, item => item.ToString()!.Contains("ORDER_ID", StringComparison.Ordinal));
        Assert.Contains(databaseAliasColumns, item => item.ToString()!.Contains("CUSTOMER_ID", StringComparison.Ordinal));
        db2.Received().GetColumns("TESTDB", "JBL_LIVE", "JBL_ORDERS");
    }

    private void SeedHappyPath()
    {
        _context.SchemaRefreshed.Returns(true);
        _context.SelectedConnectionName.Returns(ConnectionName);
        _context.SelectedDatabase.Returns("JUST_DATA");
        _db.DriverName(ConnectionName).Returns("NetezzaSQL");

        _context.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [ConnectionName] = new()
            {
                [1] = new DatabaseInfo(1, "JUST_DATA", "ADMIN", "SYSTEM"),
                [2] = new DatabaseInfo(2, "OTHER_DB", "ADMIN", "SYSTEM")
            }
        });

        _tables[ConnectionName] = new Dictionary<int, NetezzaTableInfo>
        {
            [10] = new()
            {
                DATABASE_ID = 1,
                TABLE_NAME = "EMPLOYEES",
                TABLE_DESC = "emps",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 2
            },
            [20] = new()
            {
                DATABASE_ID = 1,
                TABLE_NAME = "ORDERS",
                TABLE_DESC = "ords",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 2,
                COLUMN_COUNT = 1
            }
        };

        _context.DatabaseSchemaLookup.Returns(new Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>>
        {
            [ConnectionName] = new()
            {
                ["JUST_DATA"] = new()
                {
                    ["EMPLOYEES"] = ("ADMIN", 10),
                    ["ORDERS"] = ("ADMIN", 20)
                }
            }
        });

        _context.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>
        {
            [ConnectionName] =
            [
                new() { COLUMN_NAME = "EMP_ID", DATA_TYPE = "INTEGER", COLUMN_DESCRIPTION = "id" },
                new() { COLUMN_NAME = "EMP_NAME", DATA_TYPE = "NVARCHAR", COLUMN_DESCRIPTION = "name" },
                new() { COLUMN_NAME = "ORDER_ID", DATA_TYPE = "INTEGER", COLUMN_DESCRIPTION = "oid" }
            ]
        });

        _context.DatabaseOwners.Returns(new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        {
            [ConnectionName] = new()
            {
                ["JUST_DATA"] = new()
                {
                    ["ADMIN"] = "ADMIN"
                }
            }
        });
    }
}
