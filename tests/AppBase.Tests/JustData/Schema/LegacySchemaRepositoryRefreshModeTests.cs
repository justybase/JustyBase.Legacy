using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;
using JustData.Application.Schema;
using JustyBaseLegacy.UI.Schema;
using NSubstitute;

namespace AppBase.Tests.JustData.Schema;

public sealed class LegacySchemaRepositoryRefreshModeTests
{
    [Theory]
    [InlineData(SchemaRefreshMode.Full, NetezzaRefreshMode.full)]
    [InlineData(SchemaRefreshMode.Partial, NetezzaRefreshMode.partial)]
    [InlineData(SchemaRefreshMode.PartialOnlyTables, NetezzaRefreshMode.partialOnlyTables)]
    public async Task RefreshAsync_forwards_mode_to_netezza_download(
        SchemaRefreshMode requestMode,
        NetezzaRefreshMode expectedProviderMode)
    {
        var netezza = Substitute.For<IGeneralDb, INetezza>();
        netezza.When(x => x.InitDb()).Do(_ => { });
        ((INetezza)netezza)
            .DownloadSchemaNetezza(
                Arg.Any<string>(),
                Arg.Any<NetezzaRefreshMode>(),
                Arg.Any<List<string>>(),
                Arg.Any<bool>(),
                Arg.Any<Action?>())
            .Returns(false);

        var sessions = new ConnectionSessionRegistry();
        sessions.Set("NPS_144", netezza);

        var repository = new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            Substitute.For<IConnectionProfileCatalog>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.RefreshAsync("NPS_144", request: new SchemaRefreshRequest(requestMode)));

        await ((INetezza)netezza).Received(1).DownloadSchemaNetezza(
            "NPS_144",
            expectedProviderMode,
            Arg.Any<List<string>>(),
            false,
            Arg.Any<Action?>());
    }

    [Fact]
    public async Task RefreshAsync_throws_when_netezza_download_fails()
    {
        var netezza = Substitute.For<IGeneralDb, INetezza>();
        netezza.When(x => x.InitDb()).Do(_ => { });
        ((INetezza)netezza)
            .DownloadSchemaNetezza(
                Arg.Any<string>(),
                Arg.Any<NetezzaRefreshMode>(),
                Arg.Any<List<string>>(),
                Arg.Any<bool>(),
                Arg.Any<Action?>())
            .Returns(false);

        var sessions = new ConnectionSessionRegistry();
        sessions.Set("NPS_144", netezza);

        var repository = new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            Substitute.For<IConnectionProfileCatalog>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.RefreshAsync("NPS_144", request: new SchemaRefreshRequest(SchemaRefreshMode.Partial)));
    }
}
