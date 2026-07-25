namespace JustData.Application.Login;

public interface IConnectionProfileRepository
{
    Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default);
}

public sealed record ConnectionProfilesLoadResult(IReadOnlyList<ConnectionProfile> Profiles, int DefaultIndex, bool RecoveredFromCorruptFile);
