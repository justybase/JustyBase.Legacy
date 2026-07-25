namespace JustData.Application.Files;

public interface IRecentFileStore
{
    Task<IReadOnlyList<string>> LoadAsync(RecentFileKind kind, CancellationToken cancellationToken = default);

    Task SaveAsync(
        RecentFileKind kind,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default);
}
