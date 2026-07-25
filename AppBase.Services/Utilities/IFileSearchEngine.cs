namespace AppBase.Services.Utilities;

public interface IFileSearchEngine
{
    IReadOnlyList<string> GetDefaultExtensionPatterns();
    IReadOnlyList<string> NormalizeExtensionPatterns(string? value);
    Task<FileSearchOutcome> SearchAsync(
        IEnumerable<string> paths,
        FileSearchOptions options,
        CancellationToken cancellationToken = default);
}
