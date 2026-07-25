using AppBase.Common;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Services;
using System.Reflection;

namespace AppBase.Tests.Database;

public sealed class GeneralDbServiceConnectionStringTests
{
    [Fact]
    public void ConnectionStringForNz_UsesConfiguredHostPortAndDatabaseOverride()
    {
        GeneralDbService service = CreateService("warehouse.local:5490");

        string result = service.ConnectionStringForNz(17, "development", "REPORTING");

        Assert.Equal(
            "USERNAME=test-user;PASSWORD=test-password;PORT=5490;HOST=warehouse.local;DATABASE=REPORTING;TIMEOUT=17;",
            result);
    }

    [Fact]
    public void ConnectionStringForNz_UsesDefaultPortAndProfileDatabase()
    {
        GeneralDbService service = CreateService("warehouse.local");

        string result = service.ConnectionStringForNz(10, "development");

        Assert.Equal(
            "USERNAME=test-user;PASSWORD=test-password;PORT=5480;HOST=warehouse.local;DATABASE=DEV_DB;TIMEOUT=10;",
            result);
    }

    [Fact]
    public void ConnectionStringForPostgreSql_UsesConfiguredPort()
    {
        GeneralDbService service = CreateService("postgres.local:5544");

        string result = service.ConnectionStringForPostgreSQL("development");

        Assert.Equal(
            "Host=postgres.local;Port=5544;Username=test-user;Password=test-password;Database=DEV_DB",
            result);
    }

    [Fact]
    public void ConnectionStringBuilders_CoverSupportedProviderVariants()
    {
        GeneralDbService service = CreateService("database.local");

        Assert.Equal("Server=database.local;Database=DEV_DB;Connect Timeout=10;UID=test-user;PWD=test-password", service.ConnectionStringForDB2("development"));
        Assert.Equal("Server=database.local;Database=DEV_DB;User Id=test-user;Password=test-password;", service.ConnectionStringForMsSql("development"));
        Assert.Equal("Server=database.local;Database=OTHER;User Id=test-user;Password=test-password;", service.ConnectionStringForMsSql("development", "OTHER"));
        Assert.Equal("Server=database.local;Database=DEV_DB;Trusted_Connection=True;", service.ConnectionStringForMsSqlTrusted("development"));
        Assert.Equal("Server=database.local;Database=OTHER;Trusted_Connection=True;", service.ConnectionStringForMsSqlTrusted("development", "OTHER"));
        Assert.Contains("Initial Catalog=DEV_DB", service.ConnectionStringOleDbForNz(7, "development"));
        Assert.Contains("Initial Catalog=OTHER", service.ConnectionStringOleDbForNz(7, "development", "OTHER"));
        Assert.Contains("database.local/DEV_DB", service.ConnectionStringForOracle("development"));
    }

    [Fact]
    public void PostgreSql_UsesDefaultPort()
    {
        GeneralDbService service = CreateService("postgres.local");

        Assert.Contains("Port=5432", service.ConnectionStringForPostgreSQL("development"));
    }

    [Fact]
    public void MissingProfile_ReturnsNullForOptionalProfileValues()
    {
        GeneralDbService service = CreateService("database.local");

        Assert.Null(service.Server("missing"));
        Assert.Null(service.UserName("missing"));
        Assert.Null(service.Password("missing"));
        Assert.Null(service.DriverName("missing"));
    }

    [Theory]
    [InlineData(DatabaseTypeEnum.Netezza, "NetezzaSQL")]
    [InlineData(DatabaseTypeEnum.Postgres, "Postgres")]
    [InlineData(DatabaseTypeEnum.MsSqlDb, "MsSql")]
    [InlineData(DatabaseTypeEnum.Oracle, "Unknown")]
    public void TypeToName_MapsCoreProviders(DatabaseTypeEnum databaseType, string expected)
    {
        GeneralDbService service = CreateService("database.local");

        Assert.Equal(expected, service.TypeToName(CreateDatabaseProxy(databaseType)));
    }

    [Fact]
    public void ClipToLines_SplitsRowsAndPreservesEmbeddedNewline()
    {
        GeneralDbService service = CreateService("database.local");
        string clipboard = "a;\"multi\nline\";c\r\nd;e\r\nf;g";

        string[]? lines = service.ClipToLines(';', ref clipboard, '\\');

        Assert.NotNull(lines);
        Assert.Equal(3, lines.Length);
        Assert.Contains('\n', lines[0]);
    }

    [Fact]
    public void ClipToLines_ReturnsNullWhenClipboardHasNoRows()
    {
        GeneralDbService service = CreateService("database.local");
        string clipboard = "single row";

        Assert.Null(service.ClipToLines(';', ref clipboard, '\\'));
    }

    private static GeneralDbService CreateService(string server)
    {
        GeneralDbService service = new(CreateProxy<ILogger>());
        service.LoginDataDic["development"] = new LoginData
        {
            Name = "development",
            Driver = "NetezzaSQL",
            Server = server,
            UserName = "test-user",
            Password = "test-password",
            Database = "DEV_DB"
        };
        return service;
    }

    private static T CreateProxy<T>() where T : class =>
        DispatchProxy.Create<T, NullDispatchProxy>();

    private static IGeneralDb CreateDatabaseProxy(DatabaseTypeEnum databaseType)
    {
        IGeneralDb proxy = DispatchProxy.Create<IGeneralDb, DatabaseTypeDispatchProxy>();
        ((DatabaseTypeDispatchProxy)(object)proxy).DatabaseType = databaseType;
        return proxy;
    }

    private class DatabaseTypeDispatchProxy : DispatchProxy
    {
        public DatabaseTypeEnum DatabaseType { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_DatabaseType" ? DatabaseType : null;
    }

    private class NullDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || targetMethod.ReturnType == typeof(void))
            {
                return null;
            }

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
