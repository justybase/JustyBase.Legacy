using JustData.Application.Editor;
using System.Text;

namespace JustyBaseLegacy.UI.Editor;

public sealed class WinFormsEditorFileService : IEditorFileService
{
    public async Task<string> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(
        string path,
        string contents,
        bool useUtf8WithoutBom,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: !useUtf8WithoutBom);
        await File.WriteAllTextAsync(path, contents ?? string.Empty, encoding, cancellationToken).ConfigureAwait(false);
    }
}
