using System.Diagnostics;
using FastColoredTextBoxNS;
using JustData.Application.Editor;
using JustyBaseLegacy.UI.Forms;
using JustyBaseLegacy.UI.QuickOpen;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    private async void ShowQuickOpen()
    {
        try
        {
            var searchService = new QuickOpenSearchService(_fileSearchEngine);
            var openDocs = _editorWorkspaceViewModel.Documents
                .Select(document => (
                    document.Id,
                    document.Title,
                    document.FilePath,
                    document.Text))
                .ToArray();

            var candidates = await searchService.CollectCandidatesAsync(
                _filesViewModel.RootPaths.ToArray(),
                _filesViewModel.SearchFiles,
                _gitViewModel.HasRepository ? _gitViewModel.SelectedRepoPath : null,
                openDocs).ConfigureAwait(true);

            using var form = new QuickOpenForm(
                _colorTheme,
                searchService,
                candidates,
                TimeSpan.FromMilliseconds(Math.Max(1_000, _applicationSettingsContext.Config.FileSearchTimeout)));

            form.PositionOver(this);
            if (form.ShowDialog(this) != DialogResult.OK || form.SelectedHit is not { } hit)
                return;

            _ = RunUiEventAsync(nameof(ShowQuickOpen), () => OpenQuickOpenHitAsync(hit));
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Quick Open failed: {ex}");
            _loggerLoud.MessageBox_Show(
                this,
                ex.Message,
                "Quick Open",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task OpenQuickOpenHitAsync(QuickOpenHit hit)
    {
        FastColoredTextBox? editor = null;

        if (hit.DocumentId is EditorDocumentId documentId
            && _editorWorkspaceViewModel.Documents.Any(document => document.Id == documentId))
        {
            _editorWorkspaceViewModel.Activate(documentId);
            if (_documentIdsByTab.FirstOrDefault(item => item.Value == documentId).Key is { } tab)
            {
                _tabManager.SelectTab(tab);
                editor = _tabManager.GetEditor(tab);
            }
        }
        else if (!string.IsNullOrWhiteSpace(hit.FilePath))
        {
            editor = await OpenSqlFileAsync(hit.FilePath).ConfigureAwait(true);
        }

        if (editor is null)
            return;

        if (hit.Kind == QuickOpenHitKind.Content
            && hit.LineNumber is int lineNumber
            && lineNumber > 0
            && hit.MatchIndex is int matchIndex
            && hit.MatchLength is int matchLength
            && matchLength > 0)
        {
            NavigateEditorToMatch(editor, lineNumber, matchIndex, matchLength);
        }
        else
        {
            editor.Focus();
        }
    }

    private static void NavigateEditorToMatch(
        FastColoredTextBox editor,
        int lineNumber1Based,
        int matchIndex,
        int matchLength)
    {
        int line = Math.Clamp(lineNumber1Based - 1, 0, Math.Max(0, editor.LinesCount - 1));
        int lineLength = editor.GetLineLength(line);
        int startChar = Math.Clamp(matchIndex, 0, lineLength);
        int endChar = Math.Clamp(matchIndex + matchLength, startChar, lineLength);

        editor.Selection = new FastColoredTextBoxNS.Range(editor, startChar, line, endChar, line);
        editor.DoSelectionVisible();
        editor.Focus();
    }
}
