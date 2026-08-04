using AppBase.Data;
using JustData.Application.Login;

#if INCLUDE_DB2
using App.Data.DB2;
#endif

namespace JustyBaseLegacy.UI.Login;

internal sealed class LegacyDatabaseCatalogService : IDatabaseCatalogService
{
    private const string DefaultNetezzaPort = "5480";
    private const string DefaultDb2Port = "50000";
    private readonly int _connectionTimeout;
    private readonly Func<int, string, string, string, string, string, List<string>> _db2DatabaseList;

    public LegacyDatabaseCatalogService(int connectionTimeout)
        : this(connectionTimeout, GetDb2DatabaseList)
    {
    }

    internal LegacyDatabaseCatalogService(
        int connectionTimeout,
        Func<int, string, string, string, string, string, List<string>> db2DatabaseList)
    {
        _connectionTimeout = connectionTimeout;
        _db2DatabaseList = db2DatabaseList ?? throw new ArgumentNullException(nameof(db2DatabaseList));
    }

    public Task<IReadOnlyList<string>> GetDatabasesAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run<IReadOnlyList<string>>(() => profile.Driver switch
        {
            "NetezzaSQL" => GetNetezzaDatabases(profile),
            "DB2" => GetDb2Databases(profile),
            _ => []
        }, cancellationToken);
    }

    private List<string> GetNetezzaDatabases(ConnectionProfile profile)
    {
        SplitHostAndPort(profile.Server, out string host, out string port);
        return Netezza.GetDatabaseList(_connectionTimeout, host, profile.UserName, port, profile.Password);
    }

    private List<string> GetDb2Databases(ConnectionProfile profile)
    {
        SplitHostAndPort(profile.Server, DefaultDb2Port, out string host, out string port);
        return _db2DatabaseList(
            _connectionTimeout,
            host,
            profile.UserName,
            port,
            profile.Password,
            profile.Database);
    }

    private static List<string> GetDb2DatabaseList(
        int connectionTimeout,
        string server,
        string user,
        string port,
        string password,
        string databaseName)
    {
#if INCLUDE_DB2
        return DB2.GetDatabaseList(connectionTimeout, server, user, port, password, databaseName);
#else
        return [];
#endif
    }

    /// <summary>
    /// Same host:port convention as <c>GeneralDbService.ConnectionStringForNz</c> —
    /// <c>Server</c> may be <c>host</c> or <c>host:port</c>; default port is 5480.
    /// </summary>
    internal static void SplitHostAndPort(string? fullServer, out string host, out string port)
        => SplitHostAndPort(fullServer, DefaultNetezzaPort, out host, out port);

    private static void SplitHostAndPort(
        string? fullServer,
        string defaultPort,
        out string host,
        out string port)
    {
        fullServer ??= string.Empty;
        host = fullServer;
        port = defaultPort;
        int index = fullServer.LastIndexOf(':');
        if (index != -1 && index < fullServer.Length - 1)
        {
            host = fullServer[..index];
            port = fullServer[(index + 1)..];
        }
    }
}
