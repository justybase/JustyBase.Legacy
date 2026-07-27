using JustData.Application.Editor;

namespace JustData.ViewModels.Editor;

/// <summary>
/// Get-or-create resolution for editor→document maps without minting orphan IDs.
/// </summary>
public static class EditorWorkspaceDocumentEnsure
{
    public static EditorDocumentViewModel? TryGetByEditorKey<TEditor>(
        EditorWorkspaceViewModel workspace,
        IReadOnlyDictionary<TEditor, EditorDocumentId> idsByEditor,
        TEditor editorKey)
        where TEditor : notnull
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(idsByEditor);

        if (!idsByEditor.TryGetValue(editorKey, out EditorDocumentId mappedId))
            return null;

        return workspace.Documents.FirstOrDefault(item => item.Id == mappedId);
    }

    public static EditorDocumentViewModel? GetOrCreateByEditorKey<TEditor>(
        EditorWorkspaceViewModel workspace,
        IReadOnlyDictionary<TEditor, EditorDocumentId> idsByEditor,
        TEditor editorKey,
        Func<EditorDocumentViewModel?> create)
        where TEditor : notnull
    {
        ArgumentNullException.ThrowIfNull(create);

        return TryGetByEditorKey(workspace, idsByEditor, editorKey) ?? create();
    }
}
