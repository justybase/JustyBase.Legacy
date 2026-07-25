using JustData.Application.Editor;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Editor;

public sealed class WinFormsEditorDialogService : IEditorDialogService
{
    public Task<UnsavedDocumentDecision> ConfirmUnsavedDocumentAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DialogResult result = MessageBox.Show(
            $"Save changes to {document.Title}?",
            "Unsaved document",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);
        return Task.FromResult(result switch
        {
            DialogResult.Yes => UnsavedDocumentDecision.Save,
            DialogResult.No => UnsavedDocumentDecision.Discard,
            _ => UnsavedDocumentDecision.Cancel
        });
    }

    public Task<ExternalDocumentChangeDecision> ConfirmExternalChangeAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DialogResult result = MessageBox.Show(
            $"File was changed: {document.FilePath}{Environment.NewLine}Reload?",
            "File changed",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        return Task.FromResult(result == DialogResult.Yes
            ? ExternalDocumentChangeDecision.Reload
            : ExternalDocumentChangeDecision.KeepOpen);
    }

    public Task<string?> PickSavePathAsync(
        EditorDocumentSnapshot document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var dialog = new SaveFileDialog
        {
            Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(document.FilePath)
                ? document.Title + ".sql"
                : Path.GetFileName(document.FilePath),
            AddExtension = true,
            DefaultExt = "sql"
        };
        return Task.FromResult<string?>(dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null);
    }
}
