namespace JustData.Application.Login;

/// <summary>
/// Read-only catalog of the current session connection profiles.
/// Source of truth for connection names and profile fields in the UI layer.
/// </summary>
public interface IConnectionProfileCatalog
{
    IReadOnlyList<string> ConnectionNames { get; }

    bool TryGetProfile(string connectionName, out ConnectionProfile profile);
}
