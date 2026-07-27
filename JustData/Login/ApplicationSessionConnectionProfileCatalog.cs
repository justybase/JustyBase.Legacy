using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

public sealed class ApplicationSessionConnectionProfileCatalog(IApplicationSession session) : IConnectionProfileCatalog
{
    private readonly IApplicationSession _session = session ?? throw new ArgumentNullException(nameof(session));

    public IReadOnlyList<string> ConnectionNames =>
        _session.Profiles
            .Select(profile => profile.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool TryGetProfile(string connectionName, out ConnectionProfile profile)
    {
        profile = null!;
        if (string.IsNullOrWhiteSpace(connectionName))
            return false;

        foreach (ConnectionProfile candidate in _session.Profiles)
        {
            if (string.Equals(candidate.Name, connectionName, StringComparison.OrdinalIgnoreCase))
            {
                profile = candidate;
                return true;
            }
        }

        return false;
    }
}
