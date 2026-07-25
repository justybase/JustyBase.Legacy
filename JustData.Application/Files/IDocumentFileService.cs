namespace JustData.Application.Files;

public interface IDocumentFileService
{
    Task<IReadOnlyList<FileSystemEntry>> EnumerateAsync(
        IReadOnlyList<string> roots,
        FileEnumerationOptions options,
        CancellationToken cancellationToken = default);

    Task<FileSearchResult> SearchAsync(
        IReadOnlyList<string> candidateFiles,
        FileSearchRequest request,
        CancellationToken cancellationToken = default);

    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    Task CreateFileAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task RenameAsync(string path, string newPath, CancellationToken cancellationToken = default);
}
