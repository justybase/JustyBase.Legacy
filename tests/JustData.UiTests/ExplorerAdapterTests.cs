using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustData.Application.Schema;
using JustyBaseLegacy.UI.Schema;
using JustyBaseLegacy.UI.Controls;
using NSubstitute;
using System.Text;

namespace JustData.UiTests;

public sealed class ExplorerAdapterTests
{
    [Theory]
    [InlineData(TypeInDatabase.table, SchemaNodeKind.Table)]
    [InlineData(TypeInDatabase.view, SchemaNodeKind.View)]
    [InlineData(TypeInDatabase.procedure, SchemaNodeKind.Procedure)]
    [InlineData(TypeInDatabase.function, SchemaNodeKind.Function)]
    [InlineData(TypeInDatabase.db2alias, SchemaNodeKind.Alias)]
    [InlineData(TypeInDatabase.synonym, SchemaNodeKind.Synonym)]
    [InlineData(TypeInDatabase.sequence, SchemaNodeKind.Sequence)]
    [InlineData(TypeInDatabase.baseTables, SchemaNodeKind.Table)]
    public void Legacy_provider_object_kinds_map_to_the_same_clean_contract(TypeInDatabase legacy, SchemaNodeKind expected)
    {
        Assert.Equal(expected, LegacySchemaTypeMapper.Map(legacy));
    }

    [Fact]
    public void New_reference_adapter_preserves_positions_and_ignores_comment_text()
    {
        var references = LegacySqlReferenceParser.Parse(
            "-- SELECT ignored\nSELECT * FROM app.orders WHERE id = 1;\nDROP TABLE app.old_orders;");

        Assert.Equal(["Select", "From", "WHERE", "app.old_orders"], references.Select(reference => reference.Name));
        Assert.True(references[0].Position < references[1].Position);
        Assert.Equal(SchemaNodeKind.Table, references[^1].Kind);
    }

    [Fact]
    public void Netezza_catalog_search_finds_exact_DIMDATE_name_and_preserves_navigation_identity()
    {
        var tables = new Dictionary<int, NetezzaTableInfo>
        {
            [42] = new()
            {
                DATABASE_ID = 7,
                TABLE_NAME = "DIMDATE",
                TABLE_DESC = "Calendar dimension",
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 1
            }
        };
        var databases = new Dictionary<int, DatabaseInfo>
        {
            [7] = new(7, "JUST_DATA", "ADMIN", "ADMIN")
        };
        NetezzaColumnInfoRow[] columns =
        [
            new() { TABLE_ID = 42, DATABASE_ID = 7, COLUMN_NAME = "DATE_KEY", DATA_TYPE = "INTEGER" }
        ];

        SchemaSearchResult result = LegacySchemaRepository.SearchNetezzaCatalog(
            "test_nz_connection", "dimdate", includeColumns: true, maxResults: 1_000, tables, databases, columns);

        SchemaNode match = Assert.Single(result.Nodes);
        Assert.Equal("DIMDATE", match.Name);
        Assert.Equal(42, match.LegacyObjectId);
        Assert.Equal("JUST_DATA", match.Path.Database);
        Assert.Equal("Tables", match.Path.Schema);
        Assert.Equal("ADMIN", match.Owner);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void Netezza_catalog_search_matches_column_names_without_returning_a_fake_column_object()
    {
        var tables = new Dictionary<int, NetezzaTableInfo>
        {
            [42] = new()
            {
                DATABASE_ID = 7,
                TABLE_NAME = "DIMDATE",
                TABLE_DESC = string.Empty,
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 1
            }
        };
        var databases = new Dictionary<int, DatabaseInfo> { [7] = new(7, "JUST_DATA", "ADMIN", "ADMIN") };
        NetezzaColumnInfoRow[] columns =
        [
            new() { TABLE_ID = 42, DATABASE_ID = 7, COLUMN_NAME = "CALENDAR_DATE", DATA_TYPE = "DATE" }
        ];

        SchemaNode match = Assert.Single(LegacySchemaRepository.SearchNetezzaCatalog(
            "test_nz_connection", "calendar_date", true, 1_000, tables, databases, columns).Nodes);

        Assert.Equal(SchemaNodeKind.Table, match.Kind);
        Assert.Equal(42, match.LegacyObjectId);
    }

    [Fact]
    public async Task Netezza_DDL_uses_legacy_object_id_instead_of_the_category_as_a_schema_name()
    {
        const string connectionName = "ddl-test";
        var database = Substitute.For<IGeneralDb, INetezza>();
        var netezzaDdl = Substitute.For<INetezzaHelperService>();
        var helpers = Substitute.For<AppBase.Common.Interfaces.IDatabaseRuntimeContext>();
        helpers.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [connectionName] = new() { [7] = new(7, "JUST_DATA", "ADMIN", "ADMIN") }
        });
        helpers.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>
        {
            [connectionName] = []
        });
        var tables = new Dictionary<string, Dictionary<int, NetezzaTableInfo>>
        {
            [connectionName] = new()
            {
                [42] = new()
                {
                    DATABASE_ID = 7,
                    TABLE_NAME = "DIMDATE",
                    TABLE_DESC = string.Empty,
                    TABLE_OWNER = "ADMIN",
                    TABLE_SCHEMA = "ADMIN",
                    TABLE_OBJECT_OWNER = "ADMIN",
                    TABLE_KIND = TypeInDatabase.table
                }
            }
        };
        var generated = new NzGetTableCodeResult(new StringBuilder("CREATE TABLE JUST_DATA.ADMIN.DIMDATE"));
        netezzaDdl.GetTableCodeById(
                Arg.Any<StringBuilder>(), helpers, connectionName, 42,
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<List<string>?>(), Arg.Any<bool>())
            .Returns(new ValueTask<NzGetTableCodeResult>(generated));
        var sessions = new ConnectionSessionRegistry();
        sessions.Set(connectionName, database);

        var schemaTables = Substitute.For<INetezzaSchemaTableCatalog>();
        schemaTables.TablesByConnection.Returns(tables);

        var service = new LegacySchemaDdlService(
            netezzaDdl,
            helpers,
            sessions,
            schemaTables);
        var node = new SchemaNode(
            "ddl-test/JUST_DATA/Tables/42",
            "DIMDATE",
            SchemaNodeKind.Table,
            new(connectionName, "JUST_DATA", "Tables", "DIMDATE"),
            true,
            LegacyObjectId: 42,
            ProviderKind: TypeInDatabase.table.ToString());

        string ddl = await service.GetDdlAsync(new(node, SchemaDdlKind.Create));
        string select = await service.GetDdlAsync(new(node, SchemaDdlKind.SelectTop));

        Assert.Equal("CREATE TABLE JUST_DATA.ADMIN.DIMDATE", ddl);
        Assert.Contains("FROM\r\n    JUST_DATA.ADMIN.DIMDATE", select);
        await netezzaDdl.Received(1).GetTableCodeById(
            Arg.Any<StringBuilder>(), helpers, connectionName, 42,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<List<string>?>(), Arg.Any<bool>());
    }

    [Fact]
    public void Netezza_table_context_menu_preserves_the_legacy_action_surface()
    {
        var node = new SchemaNode(
            "test_nz_connection/JUST_DATA/Tables/42", "DIMDATE", SchemaNodeKind.Table,
            new("test_nz_connection", "JUST_DATA", "Tables", "DIMDATE"), true,
            LegacyObjectId: 42, ProviderKind: TypeInDatabase.table.ToString());

        string[] labels = Flatten(SchemaContextMenuCatalog.GetEntries(node))
            .Where(label => label != "-")
            .ToArray();

        Assert.Equal(
        [
            "User Scripts", "Others", "Groom table", "Add comment to clipboard", "Drop Table",
            "Generate statistics", "Empty table", "DDL to new query window", "DDL to clipboard",
            "Select Top 100 to clipboard", "Select Top 100 to new query window",
            "Select duplicates to clipboard", "Select deleted rows", "Grant to clipboard",
            "Show distribution", "Change distribution", "Recreate to new tab",
            "Add key to clipboard", "Add unique constraint to clipboard", "Import Data", "Export Data"
        ], labels);
    }

    [Theory]
    [InlineData(TypeInDatabase.view, "Drop view")]
    [InlineData(TypeInDatabase.thisExternal, "DDL to new query window")]
    [InlineData(TypeInDatabase.thisExternal, "DDL to clipboard")]
    [InlineData(TypeInDatabase.procedure, "DDL to new query window")]
    [InlineData(TypeInDatabase.sequence, "Select from sequence")]
    [InlineData(TypeInDatabase.synonym, "DDL synonym to clipboard")]
    public void Netezza_object_context_menus_expose_their_legacy_specific_action(
        TypeInDatabase providerKind,
        string expectedLabel)
    {
        var node = new SchemaNode(
            $"test_nz_connection/JUST_DATA/{providerKind}/42", "OBJECT_1", LegacySchemaTypeMapper.Map(providerKind),
            new("test_nz_connection", "JUST_DATA", providerKind.ToString(), "OBJECT_1"), false,
            LegacyObjectId: 42, ProviderKind: providerKind.ToString());

        Assert.Contains(expectedLabel, Flatten(SchemaContextMenuCatalog.GetEntries(node)));
    }

    [Fact]
    public void Netezza_column_context_menu_keeps_edit_drop_and_add_actions()
    {
        var node = new SchemaNode(
            "test_nz_connection/JUST_DATA/Tables/DIMDATE/DATE_KEY", "DATE_KEY", SchemaNodeKind.Column,
            new("test_nz_connection", "JUST_DATA", "Tables", "DIMDATE"), false,
            LegacyObjectId: 9, ProviderKind: "INTEGER");

        Assert.Equal(["Edit Comment", "Drop Column", "Add Column"],
            Flatten(SchemaContextMenuCatalog.GetEntries(node)));
    }

    [Theory]
    [InlineData("test_nz_connection", 1, "test_nz_connection", false)]
    [InlineData(null, 1, "test_nz_connection", true)]
    [InlineData("Other", 1, "test_nz_connection", true)]
    [InlineData("test_nz_connection", 0, "test_nz_connection", true)]
    public void Selecting_the_current_connection_does_not_rebuild_and_collapse_the_tree(
        string? currentConnection,
        int rootCount,
        string requestedConnection,
        bool expectedReload)
    {
        Assert.Equal(expectedReload,
            MvvmDatabaseExplorerControl.RequiresConnectionReload(currentConnection, rootCount, requestedConnection));
    }

    private static IEnumerable<string> Flatten(IEnumerable<SchemaContextMenuEntry> entries)
    {
        foreach (SchemaContextMenuEntry entry in entries)
        {
            yield return entry.Text;
            if (entry.Children is not null)
            {
                foreach (string child in Flatten(entry.Children))
                    yield return child;
            }
        }
    }
}
