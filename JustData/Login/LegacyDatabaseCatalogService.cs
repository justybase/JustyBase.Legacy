using AppBase.Data;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

internal sealed class LegacyDatabaseCatalogService(int connectionTimeout) : IDatabaseCatalogService
{
    public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run<IReadOnlyList<string>>(() => profile.Driver switch
        {
            "NetezzaSQL" => Netezza.GetDatabaseList(connectionTimeout, null, profile.UserName, null, profile.Password),
            "DB2" => ["DB2 support not included"],
            _ => []
        }, cancellationToken);
    }
}
