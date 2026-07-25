namespace JustData.Application.Editor;

public interface IEditorFileService
{
    Task<string> ReadAsync(string path, CancellationToken cancellationToken = default);

    Task WriteAsync(
        string path,
        string contents,
        bool useUtf8WithoutBom,
        CancellationToken cancellationToken = default);
}
