namespace JustData.Application.Editor;

public interface IEditorDialogService
{
    Task<UnsavedDocumentDecision> ConfirmUnsavedDocumentAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default);

    Task<ExternalDocumentChangeDecision> ConfirmExternalChangeAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default);

    Task<string?> PickSavePathAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default);
}
