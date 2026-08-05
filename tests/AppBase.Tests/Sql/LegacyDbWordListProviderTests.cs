using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS;
using JustyBase.Core.Database;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacyDbWordListProviderTests : IDisposable
{
    private const string ConnectionName = "wordlist-test";
    private readonly Dictionary<string, Dictionary<int, NetezzaTableInfo>> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly INetezzaCompletionContext _context;
    private readonly IGeneralDbService _db;
    private readonly LegacyDbWordListProvider _sut;

    public LegacyDbWordListProviderTests()
    {
        _context = Substitute.For<INetezzaCompletionContext>();
        _db = Substitute.For<IGeneralDbService>();
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(_tables);
        _sut = new LegacyDbWordListProvider(_context, _db, catalog);
        SeedHappyPath();
    }

    public void Dispose()
    {
        _tables.Remove(ConnectionName);
    }

    [Fact]
    public void Constructor_rejects_null_context()
    {
        Assert.Throws<ArgumentNullException>(() => new LegacyDbWordListProvider(
            null!,
            _db,
            Substitute.For<INetezzaSchemaTableCatalog>()));
    }

    [Fact]
    public async Task GetWordsListAsync_requires_connection_and_database()
    {
        var results = new List<SqlWordListItem>();
        await foreach (var item in _sut.GetWordsListAsync(SqlWordListRequest.Empty("EMP")))
            results.Add(item);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetWordsListAsync_netezza_maps_databases_and_tables()
    {
        var results = new List<SqlWordListItem>();
        await foreach (var item in _sut.GetWordsListAsync(
                           SqlWordListRequest.Empty("EMP", ConnectionName, "JUST_DATA")))
            results.Add(item);

        Assert.Contains(results, r => r.Label == "JUST_DATA" && r.Kind == SqlWordListKind.Database);
        Assert.Contains(results, r => r.Label == "EMPLOYEES" && r.Kind == SqlWordListKind.Table);
        Assert.DoesNotContain(results, r => r.Label == "ORDERS");
    }

    [Fact]
    public async Task GetWordsListAsync_db2_maps_schema_objects()
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
            }
        });
        _db.DriverName(db2Connection).Returns("DB2");

        var sessions = new ConnectionSessionRegistry();
        sessions.Set(db2Connection, db2);
        var provider = new LegacyDbWordListProvider(
            _context,
            _db,
            Substitute.For<INetezzaSchemaTableCatalog>(),
            sessions);

        var results = new List<SqlWordListItem>();
        await foreach (var item in provider.GetWordsListAsync(
                           SqlWordListRequest.Empty("JBL_LIVE.", db2Connection, "TESTDB")))
            results.Add(item);

        Assert.Contains(results, r => r.Label == "JBL_LIVE.JBL_ORDERS" && r.Kind == SqlWordListKind.Table);
        Assert.Contains(results, r => r.Label == "JBL_LIVE.JBL_VIEW" && r.Kind == SqlWordListKind.View);
    }

    [Theory]
    [InlineData(CompletionIconKind.Table, SqlWordListKind.Table)]
    [InlineData(CompletionIconKind.View, SqlWordListKind.View)]
    [InlineData(CompletionIconKind.Column, SqlWordListKind.Column)]
    [InlineData(CompletionIconKind.Database, SqlWordListKind.Database)]
    [InlineData(CompletionIconKind.Schema, SqlWordListKind.Schema)]
    [InlineData(CompletionIconKind.Function, SqlWordListKind.Function)]
    [InlineData(CompletionIconKind.Cte, SqlWordListKind.With)]
    [InlineData(CompletionIconKind.Alias, SqlWordListKind.Alias)]
    [InlineData(CompletionIconKind.Keyword, SqlWordListKind.Keyword)]
    [InlineData(CompletionIconKind.Snippet, SqlWordListKind.Snippet)]
    [InlineData(CompletionIconKind.DataType, SqlWordListKind.DataType)]
    [InlineData(CompletionIconKind.Variable, SqlWordListKind.Variable)]
    [InlineData(CompletionIconKind.Reference, SqlWordListKind.Reference)]
    public void ToNeutral_maps_icons_to_kinds(CompletionIconKind icon, SqlWordListKind expected)
    {
        var item = CompletionItemAppearance.Apply(
            new MethodAutocompleteItem2("LABEL"),
            icon,
            "detail",
            "description");

        var neutral = LegacyDbWordListProvider.ToNeutral(item);

        Assert.Equal("LABEL", neutral.Label);
        Assert.Equal(expected, neutral.Kind);
        Assert.Equal("detail", neutral.Detail);
        Assert.Equal("description", neutral.Description);
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
                [1] = new DatabaseInfo(1, "JUST_DATA", "ADMIN", "SYSTEM")
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
                COLUMN_COUNT = 1
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
                FIRST_COLUMN_ID = 1,
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
