using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;
using JustyBaseLegacy.UI.Login;

namespace JustData.Login.Tests;

public sealed class ApplicationSessionConnectionProfileCatalogTests
{
    [Fact]
    public void ConnectionNames_returns_distinct_sorted_profile_names()
    {
        var session = new ApplicationSession();
        session.SetLogin(
            new LoginSelection(new ConnectionProfile { Name = "A" }, false),
            [
                new ConnectionProfile { Name = "b" },
                new ConnectionProfile { Name = "A" },
                new ConnectionProfile { Name = "a" }
            ]);

        var catalog = new ApplicationSessionConnectionProfileCatalog(session);

        Assert.Equal(["A", "b"], catalog.ConnectionNames);
    }

    [Fact]
    public void TryGetProfile_is_case_insensitive()
    {
        var session = new ApplicationSession();
        session.SetLogin(
            new LoginSelection(new ConnectionProfile { Name = "warehouse" }, false),
            [new ConnectionProfile { Name = "Warehouse", Driver = "NetezzaSQL", Database = "SYSTEM" }]);
        var catalog = new ApplicationSessionConnectionProfileCatalog(session);

        bool found = catalog.TryGetProfile("warehouse", out ConnectionProfile profile);

        Assert.True(found);
        Assert.Equal("NetezzaSQL", profile.Driver);
        Assert.Equal("SYSTEM", profile.Database);
    }

    [Fact]
    public void Credential_lookup_adapter_maps_profile_fields()
    {
        var session = new ApplicationSession();
        session.SetLogin(
            new LoginSelection(new ConnectionProfile { Name = "dev" }, false),
            [new ConnectionProfile
            {
                Name = "dev",
                Driver = "NetezzaSQL",
                Server = "host:5480",
                UserName = "nz",
                Password = "secret",
                Database = "SYSTEM"
            }]);
        var lookup = new ConnectionProfileCatalogCredentialLookup(new ApplicationSessionConnectionProfileCatalog(session));

        Assert.True(lookup.TryGet("DEV", out var credential));
        Assert.Equal("NetezzaSQL", credential.Driver);
        Assert.Equal("host:5480", credential.Server);
        Assert.Equal("nz", credential.UserName);
        Assert.Equal("secret", credential.Password);
        Assert.Equal("SYSTEM", credential.Database);
        Assert.False(lookup.TryGet("missing", out _));
    }
}
