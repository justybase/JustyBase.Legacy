using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

/// <summary>
/// Adapts <see cref="IConnectionProfileCatalog"/> to the Core credential lookup port
/// used by <c>GeneralDbService</c> without referencing Application from Services.
/// </summary>
public sealed class ConnectionProfileCatalogCredentialLookup(IConnectionProfileCatalog catalog) : IConnectionCredentialLookup
{
    private readonly IConnectionProfileCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public bool TryGet(string connectionName, out ConnectionCredential credential)
    {
        if (!_catalog.TryGetProfile(connectionName, out ConnectionProfile profile))
        {
            credential = null!;
            return false;
        }

        credential = new ConnectionCredential
        {
            Driver = profile.Driver,
            Database = profile.Database,
            UserName = profile.UserName,
            Server = profile.Server,
            Password = profile.Password
        };
        return true;
    }
}
