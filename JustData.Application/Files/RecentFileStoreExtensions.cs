namespace JustData.Application.Files;

public static class RecentFileStoreExtensions
{
    public static async Task RecordAsync(
        this IRecentFileStore store,
        RecentFileKind kind,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(path))
            return;

        IReadOnlyList<string> existing = await store.LoadAsync(kind, cancellationToken).ConfigureAwait(false);
        string[] paths = existing
            .Where(item => !string.Equals(item, path, StringComparison.OrdinalIgnoreCase))
            .Prepend(path)
            .Take(20)
            .ToArray();
        await store.SaveAsync(kind, paths, cancellationToken).ConfigureAwait(false);
    }
}
