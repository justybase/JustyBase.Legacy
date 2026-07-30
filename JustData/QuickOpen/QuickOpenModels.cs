using JustData.Application.Editor;

namespace JustyBaseLegacy.UI.QuickOpen;

[Flags]
public enum QuickOpenSource
{
    None = 0,
    Files = 1,
    Git = 2,
    Open = 4,
}

public enum QuickOpenHitKind
{
    FileName,
    Content,
}

public sealed record QuickOpenCandidate(
    string DisplayName,
    string? FilePath,
    EditorDocumentId? DocumentId,
    string? InMemoryText,
    QuickOpenSource Sources,
    string DisplayPath);

public sealed record QuickOpenHit(
    QuickOpenHitKind Kind,
    string DisplayName,
    string DisplayPath,
    string? FilePath,
    EditorDocumentId? DocumentId,
    QuickOpenSource Sources,
    int Score,
    string? Query,
    int? LineNumber = null,
    int? MatchIndex = null,
    int? MatchLength = null,
    string? Snippet = null);

public sealed record QuickOpenListEntry(
    bool IsHeader,
    string? HeaderText,
    QuickOpenHit? Hit);
