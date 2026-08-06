using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Data;
using NSubstitute;

namespace AppBase.Tests;

/// <summary>
/// Behavioral tests for <see cref="NetezzaHelpers.InitializeConnectionSchemaData"/> after its
/// rewrite onto the shared <c>NetezzaSchemaLoader</c> (snapshot to host-store mapping).
/// </summary>
public sealed class NetezzaHelpersSchemaMappingTests
{
    private static object?[] Obj(int id, string name, string? desc, string schema, string type, string? owner = "DBA", DateTime? created = null)
        => [id, name, desc, schema, type, owner, created];

    private static object?[] Col(int objId, string name, string? desc, string type, object notNull, string? defaultValue = null)
        => [objId, name, desc, type, notNull, defaultValue];

    private static object?[] Db(int id, int defSchemaId, string name, string? owner, string defSchema)
        => [id, defSchemaId, name, owner, defSchema];

    [Fact]
    public void InitializeConnectionSchemaData_MapsSnapshotToHostStores()
    {
        var runtime = Substitute.For<IDatabaseRuntimeContext, IDatabaseRuntimeCatalogWriter>();
        runtime.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            ["CONN1"] = new()
            {
                [1] = new DatabaseInfo(1, "SALES", "owner1", "PUBLIC"),
            },
        });

        var columnTable = new List<NetezzaColumnInfoRow>();
        var runtimeWriter = (IDatabaseRuntimeCatalogWriter)runtime;
        runtimeWriter.When(x => x.SetColumnTable("CONN1", Arg.Any<List<NetezzaColumnInfoRow>>()))
            .Do(x => columnTable = x.Arg<List<NetezzaColumnInfoRow>>());

        Dictionary<string, Dictionary<string, (string owner, int tableId)>>? schemaLookup = null;
        runtimeWriter.When(x => x.SetSchemaLookup("CONN1", Arg.Any<Dictionary<string, Dictionary<string, (string owner, int tableId)>>>()))
            .Do(x => schemaLookup = x.Arg<Dictionary<string, Dictionary<string, (string owner, int tableId)>>>());

        Dictionary<string, Dictionary<string, string>>? owners = null;
        runtimeWriter.When(x => x.SetOwners("CONN1", Arg.Any<Dictionary<string, Dictionary<string, string>>>()))
            .Do(x => owners = x.Arg<Dictionary<string, Dictionary<string, string>>>());

        var addedBaseTables = new List<int>();
        runtimeWriter.When(x => x.AddBaseTable("CONN1", 1, Arg.Any<int>()))
            .Do(x => addedBaseTables.Add(x.ArgAt<int>(2)));

        var schemaCatalog = Substitute.For<INetezzaSchemaTableCatalog, INetezzaSchemaTableCatalogWriter>();
        Dictionary<int, NetezzaTableInfo>? replaced = null;
        var schemaWriter = (INetezzaSchemaTableCatalogWriter)schemaCatalog;        schemaWriter.When(x => x.ReplaceConnection("CONN1", Arg.Any<Dictionary<int, NetezzaTableInfo>>()))
            .Do(x => replaced = x.Arg<Dictionary<int, NetezzaTableInfo>>());

        using var connection = new FakeCatalogConnection(
            databaseRows: [Db(1, 10, "SALES", "owner1", "PUBLIC")],
            objectRows:
            [
                Obj(1, "CUSTOMERS", "main", "PUBLIC", "TABLE", "owner1"),
                Obj(2, "V_ACTIVE", null, "PUBLIC", "VIEW", "owner1"),
                Obj(3, "GET_P1", null, "PUBLIC", "PROCEDURE", "owner1"),
            ],
            columnRows:
            [
                Col(1, "ID", null, "INTEGER", true),
                Col(1, "NAME", "nm", "VARCHAR(20)", false, "''"),
                Col(2, "C1", null, "TIMESTAMP", false),
            ],
            distOrgRows:
            [
                [1, "ID", (sbyte)1, null],
                [1, "NAME", (sbyte)2, (sbyte)1],
            ]);

        var nz = Substitute.For<IGeneralDb, INetezza>();
        ((INetezza)nz).GetConnection().Returns(connection);

        var registry = Substitute.For<IConnectionSessionRegistry>();
        registry.TryGetValue("CONN1", out Arg.Any<IGeneralDb>())
            .Returns(call =>
            {
                call[1] = nz;
                return true;
            });


        bool ok = NetezzaHelpers.InitializeConnectionSchemaData(runtime, registry, schemaCatalog, null, "CONN1");

        Assert.True(ok);
        Assert.NotNull(replaced);
        Assert.Equal(3, replaced!.Count);
        Assert.Equal([1, 2, 3], addedBaseTables);

        var customers = replaced[1];
        Assert.Equal("CUSTOMERS", customers.TABLE_NAME);
        Assert.Equal("PUBLIC", customers.TABLE_SCHEMA);
        Assert.Equal(TypeInDatabase.table, customers.TABLE_KIND);
        Assert.Equal("main", customers.TABLE_DESC);
        Assert.Equal("owner1", customers.TABLE_OWNER);
        Assert.Equal(0, customers.FIRST_COLUMN_ID);
        Assert.Equal(2, customers.COLUMN_COUNT);

        var view = replaced[2];
        Assert.Equal(TypeInDatabase.view, view.TABLE_KIND);
        Assert.Equal(2, view.FIRST_COLUMN_ID);
        Assert.Equal(1, view.COLUMN_COUNT);

        var proc = replaced[3];
        Assert.Equal(TypeInDatabase.procedure, proc.TABLE_KIND);
        Assert.Equal(-1, proc.FIRST_COLUMN_ID);
        Assert.Equal(0, proc.COLUMN_COUNT);

        Assert.Equal(3, columnTable.Count);
        Assert.Equal("ID", columnTable[0].COLUMN_NAME);
        Assert.False(columnTable[0].IS_NULLABLE);
        Assert.Equal("INTEGER", columnTable[0].DATA_TYPE);
        Assert.Equal(1, columnTable[0].TABLE_ID);
        Assert.Equal(1, columnTable[0].DATABASE_ID);
        Assert.Equal((sbyte)1, columnTable[0].DISTSEQNO);
        Assert.Null(columnTable[0].ORGSEQNO);
        Assert.Equal("nm", columnTable[1].COLUMN_DESCRIPTION);
        Assert.Equal("''", columnTable[1].COLDEFAULT);
        Assert.True(columnTable[1].IS_NULLABLE);
        Assert.Equal((sbyte)2, columnTable[1].DISTSEQNO);
        Assert.Equal((sbyte)1, columnTable[1].ORGSEQNO);
        Assert.Equal("C1", columnTable[2].COLUMN_NAME);
        Assert.Equal(2, columnTable[2].TABLE_ID);
        Assert.Null(columnTable[2].DISTSEQNO);

        Assert.NotNull(schemaLookup);
        Assert.True(schemaLookup!["SALES"].ContainsKey("CUSTOMERS"));
        Assert.Equal(("owner1", 1), schemaLookup["SALES"]["CUSTOMERS"]);
        Assert.True(schemaLookup["SALES"].ContainsKey("GET_P1"));

        Assert.NotNull(owners);
        Assert.Equal(new Dictionary<string, string> { ["owner1"] = "owner1" }, owners!["SALES"]);
    }

    [Fact]
    public void InitializeConnectionSchemaData_ReturnsFalse_WithoutSessionConnection()
    {
        var runtime = Substitute.For<IDatabaseRuntimeContext, IDatabaseRuntimeCatalogWriter>();
        var schemaCatalog = Substitute.For<INetezzaSchemaTableCatalog, INetezzaSchemaTableCatalogWriter>();
        var nz = Substitute.For<IGeneralDb, INetezza>();
        nz.GetConnection().Returns((System.Data.Common.DbConnection?)null);

        var registry = Substitute.For<IConnectionSessionRegistry>();
        registry.TryGetValue("CONN1", out Arg.Any<IGeneralDb>())
            .Returns(call =>
            {
                call[1] = nz;
                return true;
            });

        bool ok = NetezzaHelpers.InitializeConnectionSchemaData(runtime, registry, schemaCatalog, null, "CONN1");

        Assert.False(ok);
    }
}
