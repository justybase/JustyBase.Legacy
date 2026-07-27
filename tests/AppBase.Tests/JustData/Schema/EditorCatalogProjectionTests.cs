using AppBase.Common.Interfaces;
using AppBase.Common.Configuration;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Login;
using JustyBaseLegacy.UI.Schema;
using JustyBaseLegacy.UI.Sql;
using NSubstitute;

namespace AppBase.Tests.JustData.Schema;

public sealed class EditorCatalogProjectionTests
{
    [Fact]
    public void SeedFromProfiles_adds_all_profile_connections()
    {
        var catalog = new EditorCatalogState();
        var runtime = Substitute.For<IDatabaseRuntimeContext>();
        runtime.DatabaseDictionary.Returns((IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>>)new Dictionary<string, Dictionary<int, DatabaseInfo>>());
        var messenger = new WeakReferenceMessenger();
        var projection = new EditorCatalogProjection(catalog, runtime, messenger);
        var profiles = Substitute.For<IConnectionProfileCatalog>();
        profiles.ConnectionNames.Returns(["NPS_144", "Warehouse"]);

        projection.SeedFromProfiles(profiles);

        Assert.Equal(["NPS_144", "Warehouse"], catalog.Snapshot.Connections);
        projection.Dispose();
    }

    [Fact]
    public void SchemaRefreshedMessage_updates_database_list_for_connection()
    {
        var catalog = new EditorCatalogState();
        var runtime = Substitute.For<IDatabaseRuntimeContext>();
        runtime.DatabaseDictionary.Returns((IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>>)new Dictionary<string, Dictionary<int, DatabaseInfo>>(StringComparer.OrdinalIgnoreCase)
        {
            ["NPS_144"] = new()
            {
                [1] = new DatabaseInfo(1, "JUST_DATA", "admin", "admin"),
                [2] = new DatabaseInfo(2, "SYSTEM", "admin", "admin"),
            }
        });
        var messenger = new WeakReferenceMessenger();
        var projection = new EditorCatalogProjection(catalog, runtime, messenger);

        messenger.Send(new SchemaRefreshedMessage("NPS_144"));

        Assert.Contains("NPS_144", catalog.Snapshot.Connections);
        Assert.Equal(
            ["JUST_DATA", "SYSTEM"],
            catalog.Snapshot.DatabasesFor("NPS_144").OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
        projection.Dispose();
    }
}
