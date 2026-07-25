using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustData.Application.Schema;
using JustyBaseLegacy.UI.Schema;
using NSubstitute;

namespace AppBase.Tests.Schema;

public sealed class LegacySchemaDdlServiceInjectionTests
{
    [Fact]
    public async Task GetDdlAsync_uses_injected_session_registry_and_schema_catalog()
    {
        const string connectionName = "inj-ddl";
        var database = Substitute.For<IGeneralDb, INetezza>();
        var helper = Substitute.For<INetezzaHelperService>();
        var runtime = Substitute.For<IDatabaseRuntimeContext>();
        runtime.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [connectionName] = new() { [1] = new(1, "DB", "ADMIN", "ADMIN") }
        });
        runtime.ColumnTablesDictionary.Returns(new Dictionary<string, List<NetezzaColumnInfoRow>>
        {
            [connectionName] = []
        });

        var sessions = new ConnectionSessionRegistry();
        sessions.Set(connectionName, database);

        var tables = new Dictionary<string, Dictionary<int, NetezzaTableInfo>>
        {
            [connectionName] = new()
            {
                [10] = new()
                {
                    DATABASE_ID = 1,
                    TABLE_NAME = "T",
                    TABLE_DESC = "",
                    TABLE_OWNER = "ADMIN",
                    TABLE_SCHEMA = "ADMIN",
                    TABLE_OBJECT_OWNER = "ADMIN",
                    TABLE_KIND = TypeInDatabase.table,
                    FIRST_COLUMN_ID = 0,
                    COLUMN_COUNT = 0
                }
            }
        };
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(tables);

        var sut = new LegacySchemaDdlService(helper, runtime, sessions, catalog);
        var node = new SchemaNode(
            $"{connectionName}/DB/Tables/10",
            "T",
            SchemaNodeKind.Table,
            new(connectionName, "DB", "Tables", "T"),
            true,
            LegacyObjectId: 10,
            ProviderKind: TypeInDatabase.table.ToString());

        string sql = await sut.GetDdlAsync(new(node, SchemaDdlKind.SelectTop));

        Assert.Contains("FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DB.ADMIN.T", sql, StringComparison.Ordinal);
        await helper.DidNotReceiveWithAnyArgs().GetTableCodeById(
            default!, default!, default!, default, default, default, default, default, default);
    }
}
