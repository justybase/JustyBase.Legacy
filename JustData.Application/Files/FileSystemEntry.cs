namespace JustData.Application.Files;

public sealed record FileSystemEntry(
    string Path,
    bool IsDirectory,
    DateTime? LastWriteTimeUtc = null);

public sealed record FileEnumerationOptions(
    IReadOnlyList<string> Extensions,
    bool SortByLastWrite,
    bool SortByName);

public sealed record FileSearchRequest(
    string Query,
    IReadOnlyList<string> ExtensionPatterns,
    bool MatchWholeWord = false,
    bool MatchCase = false,
    bool UseRegex = false,
    int MaxFiles = 200,
    int MaxMatchesPerFile = 50,
    TimeSpan? Timeout = null);

public sealed record FileSearchMatch(
    int LineNumber,
    string LineText,
    int MatchIndex,
    int MatchLength);

public sealed record FileSearchFileResult(
    string Path,
    IReadOnlyList<FileSearchMatch> Matches,
    bool IsTruncated);

public sealed record FileSearchResult(
    IReadOnlyList<FileSearchFileResult> Files,
    bool WasCancelled,
    bool WasTruncated,
    int MatchCount);

public enum FileChangeKind
{
    Created,
    Deleted,
    Renamed
}

public sealed record FileChange(
    FileChangeKind Kind,
    string Path,
    string? OldPath = null);

public enum RecentFileKind
{
    Single,
    ManySql
}
