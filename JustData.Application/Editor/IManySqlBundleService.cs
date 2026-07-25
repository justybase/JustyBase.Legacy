namespace JustData.Application.Editor;

public interface IManySqlBundleService
{
    Task<ManySqlBundle> LoadAsync(string path, CancellationToken cancellationToken = default);

    Task SaveAsync(
        string path,
        ManySqlBundle bundle,
        CancellationToken cancellationToken = default);
}
