using JustData.ViewModels.Editor;
using JustData.Application.Editor;
using JustyBaseLegacy.UI.ImportExport;
using JustyBaseLegacy.UI.Sql;

namespace JustyBaseLegacy.UI.Editor;

/// <summary>Owns execution-resource cleanup for editor document lifecycle.</summary>
internal sealed class DocumentExecutionLifecyclePresenter : IDisposable
{
    private readonly EditorWorkspaceViewModel _workspace;
    private readonly ISqlExecutionSessionRegistry _sessions;
    private readonly WinFormsSqlResultPresenter _results;
    private readonly IDocumentResultGridRegistry _grids;

    public DocumentExecutionLifecyclePresenter(EditorWorkspaceViewModel workspace, ISqlExecutionSessionRegistry sessions,
        WinFormsSqlResultPresenter results, IDocumentResultGridRegistry grids)
    {
        _workspace = workspace;
        _sessions = sessions;
        _results = results;
        _grids = grids;
        _workspace.DocumentClosed += OnDocumentClosed;
    }

    private void OnDocumentClosed(EditorDocumentViewModel document) => _ = CleanupAsync(document.Id);

    private async Task CleanupAsync(EditorDocumentId documentId)
    {
        try { await _sessions.CancelAsync(documentId); }
        finally { _sessions.Cleanup(documentId); }
        _results.RemoveDocument(documentId);
        _grids.RemoveDocument(documentId);
    }

    public void Dispose() => _workspace.DocumentClosed -= OnDocumentClosed;
}
