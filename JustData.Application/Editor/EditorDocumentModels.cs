namespace JustData.Application.Editor;

public sealed record EditorDocumentSnapshot(
    EditorDocumentId Id,
    string Title,
    string Text,
    string? FilePath,
    bool IsDirty,
    bool IsReadOnly,
    string ConnectionName,
    string DatabaseName,
    bool KeepConnectionOpen,
    bool ContinueOnError,
    bool ExternalChangePending);

public enum EditorFileChangeKind
{
    Changed,
    Deleted,
    Renamed
}

public sealed record EditorFileChange(
    EditorFileChangeKind Kind,
    string Path,
    string? OldPath = null);

public sealed record ManySqlContent(string Title, string Text);

/// <summary>
/// Neutral representation of the legacy .manysql wire format.
/// The adapter is responsible for preserving its JSON property names.
/// </summary>
public sealed record ManySqlBundle(
    IReadOnlyList<string> SqlPaths,
    IReadOnlyList<ManySqlContent> SqlContentList,
    IReadOnlyList<string> TabsOrder,
    int SelectedTabNum);

public enum UnsavedDocumentDecision
{
    Save,
    Discard,
    Cancel
}

public enum ExternalDocumentChangeDecision
{
    Reload,
    KeepOpen,
    Cancel
}
