using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Services;
using NSubstitute;
using System.Data.Common;
using System.Text;

namespace AppBase.Tests.Sql;

public sealed class NetezzaHelperServiceOnlineDdlTests
{
    private const string ConnectionName = "online-ddl-test";

    [Fact]
    public async Task GetTableCodeById_does_not_call_full_schema_refresh()
    {
        var refreshHost = Substitute.For<INetezzaSchemaRefreshHost>();
        var sessions = new ConnectionSessionRegistry();
        var database = Substitute.For<IGeneralDb, INetezza>();
        database.GetConnection(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => throw new InvalidOperationException("live-connection-opened"));
        sessions.Set(ConnectionName, database);

        var schemaTables = Substitute.For<INetezzaSchemaTableCatalog>();
        schemaTables.TablesByConnection.Returns(new Dictionary<string, Dictionary<int, NetezzaTableInfo>>(StringComparer.OrdinalIgnoreCase)
        {
            [ConnectionName] = new()
            {
                [596829] = new()
                {
                    DATABASE_ID = 1,
                    TABLE_NAME = "DIMACCOUNT",
                    TABLE_DESC = "",
                    TABLE_OWNER = "ADMIN",
                    TABLE_SCHEMA = "ADMIN",
                    TABLE_OBJECT_OWNER = "ADMIN",
                    TABLE_KIND = TypeInDatabase.table,
                    FIRST_COLUMN_ID = 0,
                    COLUMN_COUNT = 1
                }
            }
        });

        var config = Substitute.For<IApplicationConfig>();
        var runtime = Substitute.For<IDatabaseRuntimeContext>();
        runtime.Config.Returns(config);
        runtime.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [ConnectionName] = new() { [1] = new(1, "JUST_DATA", "ADMIN", "ADMIN") }
        });

        var sut = new NetezzaHelperService(sessions, schemaTables);
        sut.Initialize(refreshHost);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetTableCodeById(new StringBuilder(), runtime, ConnectionName, 596829));

        Assert.Equal("live-connection-opened", ex.Message);
        await refreshHost.DidNotReceiveWithAnyArgs()
            .RefreshTableListInternalAsync(default!, default);
    }

    [Fact]
    public async Task GetRecreateTableCodeById_does_not_call_full_schema_refresh()
    {
        var refreshHost = Substitute.For<INetezzaSchemaRefreshHost>();
        var sessions = new ConnectionSessionRegistry();
        var database = Substitute.For<IGeneralDb, INetezza>();
        database.GetConnection(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => throw new InvalidOperationException("live-connection-opened"));
        sessions.Set(ConnectionName, database);

        var schemaTables = Substitute.For<INetezzaSchemaTableCatalog>();
        schemaTables.TablesByConnection.Returns(new Dictionary<string, Dictionary<int, NetezzaTableInfo>>(StringComparer.OrdinalIgnoreCase)
        {
            [ConnectionName] = new()
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
                    COLUMN_COUNT = 1
                }
            }
        });

        var config = Substitute.For<IApplicationConfig>();
        var runtime = Substitute.For<IDatabaseRuntimeContext>();
        runtime.Config.Returns(config);
        runtime.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            [ConnectionName] = new() { [1] = new(1, "DB", "ADMIN", "ADMIN") }
        });

        var sut = new NetezzaHelperService(sessions, schemaTables);
        sut.Initialize(refreshHost);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetRecreateTableCodeById(runtime, ConnectionName, 10));

        Assert.Equal("live-connection-opened", ex.Message);
        await refreshHost.DidNotReceiveWithAnyArgs()
            .RefreshTableListInternalAsync(default!, default);
    }

    [Fact]
    public async Task GetAllTablesDdlAsync_does_not_call_full_schema_refresh()
    {
        var refreshHost = Substitute.For<INetezzaSchemaRefreshHost>();
        var sessions = new ConnectionSessionRegistry();
        var database = Substitute.For<IGeneralDb, INetezza>();
        database.GetConnection(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => throw new InvalidOperationException("live-connection-opened"));
        sessions.Set(ConnectionName, database);

        var schemaTables = Substitute.For<INetezzaSchemaTableCatalog>();
        var sut = new NetezzaHelperService(sessions, schemaTables);
        sut.Initialize(refreshHost);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetAllTablesDdlAsync(ConnectionName, "JUST_DATA"));

        Assert.Equal("live-connection-opened", ex.Message);
        await refreshHost.DidNotReceiveWithAnyArgs()
            .RefreshTableListInternalAsync(default!, default);
    }
}
