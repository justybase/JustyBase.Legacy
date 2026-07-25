using AppBase.Data;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

internal sealed class LegacyDatabaseCatalogService(int connectionTimeout) : IDatabaseCatalogService
{
    private const string DefaultNetezzaPort = "5480";

    public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run<IReadOnlyList<string>>(() => profile.Driver switch
        {
            "NetezzaSQL" => GetNetezzaDatabases(profile),
            "DB2" => ["DB2 support not included"],
            _ => []
        }, cancellationToken);
    }

    private List<string> GetNetezzaDatabases(ConnectionProfile profile)
    {
        SplitHostAndPort(profile.Server, out string host, out string port);
        return Netezza.GetDatabaseList(connectionTimeout, host, profile.UserName, port, profile.Password);
    }

    /// <summary>
    /// Same host:port convention as <c>GeneralDbService.ConnectionStringForNz</c> —
    /// <c>Server</c> may be <c>host</c> or <c>host:port</c>; default port is 5480.
    /// </summary>
    internal static void SplitHostAndPort(string? fullServer, out string host, out string port)
    {
        fullServer ??= string.Empty;
        host = fullServer;
        port = DefaultNetezzaPort;
        int index = fullServer.LastIndexOf(':');
        if (index != -1 && index < fullServer.Length - 1)
        {
            host = fullServer[..index];
            port = fullServer[(index + 1)..];
        }
    }
}
