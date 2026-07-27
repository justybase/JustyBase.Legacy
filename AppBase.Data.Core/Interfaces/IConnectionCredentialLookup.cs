namespace AppBase.Data.Core.Interfaces;

/// <summary>
/// Core-level read of connection credentials (driver/db/user/server/password).
/// Application code adapts this from the session profile catalog.
/// </summary>
public interface IConnectionCredentialLookup
{
    bool TryGet(string connectionName, out ConnectionCredential credential);
}

public sealed class ConnectionCredential
{
    public string Driver { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Server { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
