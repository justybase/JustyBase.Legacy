using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Visitor;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacySchemaSyncTests
{
    private const string ConnectionName = "schema-sync-test";
    private readonly Dictionary<string, Dictionary<int, NetezzaTableInfo>> _tables = new(StringComparer.OrdinalIgnoreCase);

    private INetezzaSchemaTableCatalog CreateCatalog()
    {
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(_tables);
        return catalog;
    }

    [Fact]
    public void SyncConnection_returns_empty_for_null_or_missing_args()
    {
        var catalog = CreateCatalog();
        Assert.Same(NetezzaSchemaSnapshot.Empty, LegacySchemaSync.SyncConnection(null!, Substitute.For<INetezzaCompletionContext>(), catalog, "x"));
        Assert.Same(NetezzaSchemaSnapshot.Empty, LegacySchemaSync.SyncConnection(new InMemorySchemaProvider(), null!, catalog, "x"));
        Assert.Same(NetezzaSchemaSnapshot.Empty, LegacySchemaSync.SyncConnection(new InMemorySchemaProvider(), Substitute.For<INetezzaCompletionContext>(), null!, "x"));
        Assert.Same(NetezzaSchemaSnapshot.Empty, LegacySchemaSync.SyncConnection(new InMemorySchemaProvider(), Substitute.For<INetezzaCompletionContext>(), catalog, ""));
    }

    [Fact]
    public void SyncConnection_maps_cached_tables_and_columns()
    {
        var columns = new List<NetezzaColumnInfoRow>
        {
            new() { COLUMN_NAME = "ID", DATA_TYPE = "INTEGER", IS_NULLABLE = false, COLUMN_DESCRIPTION = "pk" },
            new() { COLUMN_NAME = "NAME", DATA_TYPE = "NVARCHAR", IS_NULLABLE = true, COLUMN_DESCRIPTION = "label" }
        };

        _tables[ConnectionName] = new Dictionary<int, NetezzaTableInfo>
        {
            [42] = new()
            {
                DATABASE_ID = 7,
                TABLE_NAME = "EMPLOYEES",
                TABLE_DESC = "Employee roster",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 2
            },
            [99] = new()
            {
                DATABASE_ID = 7,
                TABLE_NAME = "V_EMP",
                TABLE_DESC = "",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.view,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 0
            }
        };

        var context = Substitute.For<INetezzaCompletionContext>();
        context.DatabaseSchemaLookup.Returns(new Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>>
        {
            [ConnectionName] = new()
            {
                ["JUST_DATA"] = new()
                {
                    ["EMPLOYEES"] = ("ADMIN", 42),
                    ["V_EMP"] = ("ADMIN", 99)
                }
            }
        });
        context.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>
        {
            [ConnectionName] = columns
        });

        var provider = new InMemorySchemaProvider();
        var snapshot = LegacySchemaSync.SyncConnection(provider, context, CreateCatalog(), ConnectionName);

        Assert.Equal(2, snapshot.Tables.Count);
        var employees = Assert.Single(snapshot.Tables, t => t.Name == "EMPLOYEES");
        Assert.Equal("ADMIN", employees.Schema);
        Assert.Equal("JUST_DATA", employees.Database);
        Assert.False(employees.IsView);
        Assert.Equal("Employee roster", employees.Description);
        Assert.NotNull(employees.Columns);
        Assert.Equal(2, employees.Columns.Count);
        Assert.Equal("ID", employees.Columns[0].Name);
        Assert.Equal("INTEGER", employees.Columns[0].DataType);

        var view = Assert.Single(snapshot.Tables, t => t.Name == "V_EMP");
        Assert.True(view.IsView);
        Assert.NotNull(view.Columns);
        Assert.Empty(view.Columns);
        Assert.True(provider.HasTables());
    }

    [Fact]
    public void SyncSelectedConnection_uses_selected_connection_name()
    {
        _tables[ConnectionName] = new Dictionary<int, NetezzaTableInfo>
        {
            [1] = new()
            {
                DATABASE_ID = 1,
                TABLE_NAME = "T1",
                TABLE_DESC = "d",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 0
            }
        };

        var context = Substitute.For<INetezzaCompletionContext>();
        context.SelectedConnectionName.Returns(ConnectionName);
        context.DatabaseSchemaLookup.Returns(new Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>>
        {
            [ConnectionName] = new()
            {
                ["DB"] = new() { ["T1"] = ("ADMIN", 1) }
            }
        });
        context.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>());

        var provider = new InMemorySchemaProvider();
        LegacySchemaSync.SyncSelectedConnection(provider, context, CreateCatalog());

        Assert.True(provider.HasTables());
    }

    [Fact]
    public void SyncAllLoadedConnections_is_noop_when_lookup_missing()
    {
        var provider = new InMemorySchemaProvider();
        LegacySchemaSync.SyncAllLoadedConnections(provider, null!, CreateCatalog());
        Assert.False(provider.HasTables());
    }
}
