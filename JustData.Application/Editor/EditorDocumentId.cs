namespace JustData.Application.Editor;

/// <summary>
/// Stable runtime identity for an open editor document.  It is deliberately
/// independent of the document path because unsaved documents have no path
/// and a path can be saved under a new name.
/// </summary>
public readonly record struct EditorDocumentId(Guid Value)
{
    public static EditorDocumentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
