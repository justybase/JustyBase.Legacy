using AppBase.Services.Utilities;
using JustData.Application.Files;

namespace JustyBaseLegacy.UI.Files;

public sealed class WinFormsDocumentFileService : IDocumentFileService
{
    private readonly IFileSearchEngine _fileSearchEngine;

    public WinFormsDocumentFileService(IFileSearchEngine fileSearchEngine)
    {
        _fileSearchEngine = fileSearchEngine ?? throw new ArgumentNullException(nameof(fileSearchEngine));
    }

    public async Task<IReadOnlyList<FileSystemEntry>> EnumerateAsync(
        IReadOnlyList<string> roots,
        FileEnumerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<FileSystemEntry>();
        var extensions = options.Extensions.Count == 0
            ? _fileSearchEngine.GetDefaultExtensionPatterns()
            : options.Extensions;

        await Task.Run(() =>
        {
            foreach (var root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(root)) continue;
                var directories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Where(path => !path.Contains(Path.DirectorySeparatorChar + ".", StringComparison.Ordinal));
                entries.Add(new FileSystemEntry(root, true));
                foreach (var directory in directories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(new FileSystemEntry(directory, true));
                }

                IEnumerable<string> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Where(path => extensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)));
                if (options.SortByLastWrite)
                    files = files.OrderByDescending(path => File.GetLastWriteTimeUtc(path));
                else if (options.SortByName)
                    files = files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(new FileSystemEntry(file, false, File.GetLastWriteTimeUtc(file)));
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        return entries;
    }

    public async Task<FileSearchResult> SearchAsync(
        IReadOnlyList<string> candidateFiles,
        FileSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _fileSearchEngine.SearchAsync(
            candidateFiles,
            new FileSearchOptions
            {
                Query = request.Query,
                ExtensionPatterns = request.ExtensionPatterns,
                MatchWholeWord = request.MatchWholeWord,
                MatchCase = request.MatchCase,
                UseRegex = request.UseRegex,
                MaxFiles = request.MaxFiles,
                MaxMatchesPerFile = request.MaxMatchesPerFile,
                Timeout = request.Timeout ?? TimeSpan.FromSeconds(10)
            }, cancellationToken).ConfigureAwait(false);

        return new FileSearchResult(
            result.Files.Select(file => new JustData.Application.Files.FileSearchFileResult(
                file.Path,
                file.Matches.Select(match => new JustData.Application.Files.FileSearchMatch(
                    match.LineNumber, match.LineText, match.MatchIndex, match.MatchLength)).ToArray(),
                file.IsTruncated)).ToArray(),
            result.WasCancelled,
            result.WasTruncated,
            result.MatchCount);
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task CreateFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.Create(path);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Directory.Exists(path)) Directory.Delete(path, true);
        else if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task RenameAsync(string path, string newPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Directory.Exists(path)) Directory.Move(path, newPath);
        else File.Move(path, newPath);
        return Task.CompletedTask;
    }
}
