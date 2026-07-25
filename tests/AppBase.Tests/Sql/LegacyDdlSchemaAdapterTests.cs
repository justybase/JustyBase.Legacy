using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Data.Ddl;
using JustyBase.NetezzaDdl.Models;
using NSubstitute;

namespace AppBase.Tests.Sql;

public sealed class LegacyDdlSchemaAdapterTests
{
    private const string ConnectionName = "ddl-adapter-test";
    private readonly Dictionary<string, Dictionary<int, NetezzaTableInfo>> _tables = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void BuildTableInput_throws_when_object_missing()
    {
        var helpers = Substitute.For<IDatabaseRuntimeContext>();
        var schemaTables = CreateSchemaTables();
        var sessions = Substitute.For<IConnectionSessionRegistry>();

        Assert.Throws<InvalidOperationException>(
            () => LegacyDdlSchemaAdapter.BuildTableInput(
                helpers,
                schemaTables,
                sessions,
                ConnectionName,
                objectId: 99));
    }

    [Fact]
    public void BuildTableInput_maps_columns_distribution_and_override_name()
    {
        SeedTable();
        var helpers = CreateHelpers();
        var schemaTables = CreateSchemaTables();
        var sessions = Substitute.For<IConnectionSessionRegistry>();

        var input = LegacyDdlSchemaAdapter.BuildTableInput(
            helpers,
            schemaTables,
            sessions,
            ConnectionName,
            objectId: 42,
            overrideTableName: "EMPLOYEES_COPY",
            middleCode: "/* mid */",
            endingCode: "/* end */");

        Assert.Equal("JUST_DATA", input.Database);
        Assert.Equal("ADMIN", input.Schema);
        Assert.Equal("EMPLOYEES", input.TableName);
        Assert.Equal("EMPLOYEES_COPY", input.OverrideTableName);
        Assert.Contains(input.Columns!, c => c.Name == "EMP_ID");
        Assert.Contains(input.Columns!, c => c.Name == "EMP_NAME");
        Assert.Equal(["EMP_ID"], input.DistributeColumns);
        Assert.Equal(["EMP_NAME", "EMP_ID"], input.OrganizeColumns);
        Assert.Equal("/* mid */", input.MiddleCode);
        Assert.Equal("/* end */", input.EndingCode);
    }

    [Fact]
    public void BuildViewInput_maps_definition_and_description()
    {
        SeedTable(isView: true, description: "Employee view");
        var helpers = CreateHelpers();
        var schemaTables = CreateSchemaTables();

        var input = LegacyDdlSchemaAdapter.BuildViewInput(
            helpers,
            schemaTables,
            ConnectionName,
            objectId: 42,
            viewDefinition: "SELECT 1");

        Assert.Equal("JUST_DATA", input.Database);
        Assert.Equal("ADMIN", input.Schema);
        Assert.Equal("EMPLOYEES", input.ViewName);
        Assert.Equal("SELECT 1", input.ViewDefinition);
        Assert.Equal("Employee view", input.ViewComment);
    }

    [Fact]
    public void BuildExternalInput_uses_schema_table_and_options()
    {
        SeedTable();
        var helpers = CreateHelpers();
        var schemaTables = CreateSchemaTables();
        var options = new NetezzaExternalTableOptions { DataObject = @"\\share\file.txt" };

        var input = LegacyDdlSchemaAdapter.BuildExternalInput(helpers, schemaTables, ConnectionName, objectId: 42, options);

        Assert.Equal("JUST_DATA", input.Database);
        Assert.Equal("ADMIN", input.Schema);
        Assert.Equal("EMPLOYEES", input.TableName);
        Assert.Equal(@"\\share\file.txt", input.Options.DataObject);
    }

    private INetezzaSchemaTableCatalog CreateSchemaTables()
    {
        var schemaTables = Substitute.For<INetezzaSchemaTableCatalog>();
        schemaTables.TablesByConnection.Returns(_tables);
        return schemaTables;
    }

    private static IDatabaseRuntimeContext CreateHelpers()
    {
        var helpers = Substitute.For<IDatabaseRuntimeContext>();
        helpers.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [ConnectionName] = new() { [7] = new(7, "JUST_DATA", "ADMIN", "ADMIN") }
        });
        helpers.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>
        {
            [ConnectionName] =
            [
                new()
                {
                    COLUMN_NAME = "EMP_ID",
                    DATA_TYPE = "INTEGER",
                    IS_NULLABLE = false,
                    COLUMN_DESCRIPTION = "pk",
                    DISTSEQNO = 1,
                    ORGSEQNO = 2
                },
                new()
                {
                    COLUMN_NAME = "EMP_NAME",
                    DATA_TYPE = "NVARCHAR",
                    IS_NULLABLE = true,
                    COLUMN_DESCRIPTION = "name",
                    DISTSEQNO = null,
                    ORGSEQNO = 1
                }
            ]
        });
        return helpers;
    }

    private void SeedTable(bool isView = false, string description = "Employee roster")
    {
        _tables[ConnectionName] = new Dictionary<int, NetezzaTableInfo>
        {
            [42] = new()
            {
                DATABASE_ID = 7,
                TABLE_NAME = "EMPLOYEES",
                TABLE_DESC = description,
                TABLE_OWNER = "ADMIN",
                TABLE_SCHEMA = "ADMIN",
                TABLE_OBJECT_OWNER = "ADMIN",
                TABLE_KIND = isView ? TypeInDatabase.view : TypeInDatabase.table,
                FIRST_COLUMN_ID = 0,
                COLUMN_COUNT = 2
            }
        };
    }
}
