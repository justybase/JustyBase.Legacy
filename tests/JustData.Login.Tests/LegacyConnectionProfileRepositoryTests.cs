using AppBase.Common;
using AppBase.Data.Core.Interfaces;
using AppBase.Services;
using JustyBaseLegacy.UI.Login;
using NSubstitute;

namespace JustData.Login.Tests;

public sealed class LegacyConnectionProfileRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustData-LoginRepositoryTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Encrypted_repository_round_trips_the_legacy_LoginData_json_shape()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "credentials.json");
        var validator = Substitute.For<ILoginDataValidator>();
        validator.Normalize(Arg.Any<IEnumerable<LoginData>>()).Returns(args => args.Arg<IEnumerable<LoginData>>()!.ToList());
        validator.ClampDefaultIndex(Arg.Any<IReadOnlyList<LoginData>>(), Arg.Any<int>()).Returns(args => args.ArgAt<int>(1));
        var repository = new LegacyConnectionProfileRepository(new CredentialStore(), path, validator);
        var profile = new JustData.Application.Login.ConnectionProfile { Name = "legacy", Driver = "NetezzaSQL", Server = "host", UserName = "user", Password = "secret", Database = "SYSTEM" };

        await repository.SaveAsync([profile], 0);
        var loaded = await repository.LoadAsync();

        Assert.False(loaded.RecoveredFromCorruptFile);
        Assert.Equal("legacy", loaded.Profiles[0].Name);
        Assert.Equal("secret", loaded.Profiles[0].Password);
        Assert.False(File.ReadAllText(path + ".enc").Contains("secret", StringComparison.Ordinal));
        Assert.Contains("\"DefaultIndex\":0", new CredentialStore().Read(path + ".enc").Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Corrupt_encrypted_file_is_moved_to_timestamped_backup_and_load_recovers()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "credentials.json");
        File.WriteAllText(path + ".enc", "broken encrypted file");

        var validator = Substitute.For<ILoginDataValidator>();
        validator.Normalize(Arg.Any<IEnumerable<LoginData>>()).Returns(args => args.Arg<IEnumerable<LoginData>>()!.ToList());
        validator.ClampDefaultIndex(Arg.Any<IReadOnlyList<LoginData>>(), Arg.Any<int>()).Returns(args => args.ArgAt<int>(1));
        var result = await new LegacyConnectionProfileRepository(new CredentialStore(), path, validator).LoadAsync();

        Assert.True(result.RecoveredFromCorruptFile);
        Assert.Empty(result.Profiles);
        Assert.False(File.Exists(path + ".enc"));
        Assert.Single(Directory.GetFiles(_directory, "credentials.json.enc.corrupt-*.bak"));
    }

    [Fact]
    public void Mapper_preserves_every_legacy_LoginData_field()
    {
        var source = new LoginData { Name = "name", Driver = "driver", Server = "server", UserName = "user", Password = "secret", Database = "db", DefaultIndex = 9 };
        var roundTrip = LegacyConnectionProfileRepository.Map(LegacyConnectionProfileRepository.Map(source));

        Assert.Equal(source.Name, roundTrip.Name); Assert.Equal(source.Driver, roundTrip.Driver); Assert.Equal(source.Server, roundTrip.Server);
        Assert.Equal(source.UserName, roundTrip.UserName); Assert.Equal(source.Password, roundTrip.Password); Assert.Equal(source.Database, roundTrip.Database);
    }

    [Fact]
    public void Session_adapter_replaces_the_legacy_dictionary_from_session_profiles()
    {
        var service = Substitute.For<IGeneralDbService>();
        var dictionary = new Dictionary<string, LoginData>
        {
            ["stale"] = new LoginData { Name = "stale" }
        };
        service.LoginDataDic.Returns(dictionary);
        var session = new JustData.Application.Login.ApplicationSession();
        session.SetLogin(
            new JustData.Application.Login.LoginSelection(new JustData.Application.Login.ConnectionProfile { Name = "selected" }, false),
            [new JustData.Application.Login.ConnectionProfile { Name = "selected", Driver = "NetezzaSQL", Password = "secret" }]);

        new GeneralDbSessionAdapter(service).Apply(session);

        Assert.DoesNotContain("stale", dictionary.Keys);
        Assert.Equal("secret", dictionary["selected"].Password);
    }

    [Fact]
    public async Task Database_catalog_adapter_preserves_the_legacy_non_provider_behavior()
    {
        var catalog = new LegacyDatabaseCatalogService(5);

        var db2 = await catalog.GetDatabasesAsync(new JustData.Application.Login.ConnectionProfile { Driver = "DB2" });
        var unknown = await catalog.GetDatabasesAsync(new JustData.Application.Login.ConnectionProfile { Driver = "Other" });

        Assert.Equal(["DB2 support not included"], db2);
        Assert.Empty(unknown);
    }

    [Theory]
    [InlineData("warehouse.local", "warehouse.local", "5480")]
    [InlineData("warehouse.local:5480", "warehouse.local", "5480")]
    [InlineData("10.0.0.1:1234", "10.0.0.1", "1234")]
    [InlineData(null, "", "5480")]
    public void Database_catalog_splits_netezza_host_and_port(string? server, string expectedHost, string expectedPort)
    {
        LegacyDatabaseCatalogService.SplitHostAndPort(server, out string host, out string port);

        Assert.Equal(expectedHost, host);
        Assert.Equal(expectedPort, port);
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
