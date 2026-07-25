using DatabaseDataGridView.WinForms;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.Services;
using System.Data;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Minimal WinForms surface needed by the document result presenter. Keeping
/// this boundary small prevents the SQL event consumer from owning the main
/// form or reaching into its document and DockSuite state.
/// </summary>
internal interface IWinFormsSqlResultView
{
    bool CanPresentSqlResult(EditorDocumentId documentId);
    bool InvokeRequired { get; }
    void BeginInvoke(Action action);
    TabPagePicture CreatePresentedResultTab(EditorDocumentId documentId, ResultSetDescriptor descriptor);
    CustomDataGridView CreatePresentedResultGrid(
        EditorDocumentId documentId,
        TabPagePicture tab,
        ResultSetDescriptor descriptor,
        List<object[]> rows);
    void RegisterPresentedResultGrid(TabPage tab, CustomDataGridView grid);
}

/// <summary>
/// WinForms-only consumer of the document SQL event stream.  It owns the
/// transient grid buffers; the execution VM retains only result metadata.
/// </summary>
internal sealed class WinFormsSqlResultPresenter : IDisposable
{
    private readonly IWinFormsSqlResultView _view;
    private readonly Dictionary<string, PendingResult> _pending = new(StringComparer.Ordinal);
    private bool _disposed;

    public WinFormsSqlResultPresenter(IWinFormsSqlResultView view) => _view = view;

    public void Handle(SqlExecutionEvent executionEvent)
    {
        if (_disposed || !_view.CanPresentSqlResult(executionEvent.DocumentId))
            return;

        if (_view.InvokeRequired)
        {
            _view.BeginInvoke(() => Handle(executionEvent));
            return;
        }

        switch (executionEvent.Kind)
        {
            case SqlExecutionEventKind.ResultSet when executionEvent.ResultSet is not null:
                _pending[executionEvent.ResultSet.ResultSetId] = new PendingResult(executionEvent.DocumentId, executionEvent.ResultSet);
                break;
            case SqlExecutionEventKind.Rows when executionEvent.Rows is not null:
                AppendRows(executionEvent);
                break;
            case SqlExecutionEventKind.Completed:
                FinalizeDocument(executionEvent.DocumentId);
                break;
        }
    }

    public void RemoveDocument(EditorDocumentId documentId)
    {
        foreach (string key in _pending
            .Where(pair => pair.Value.DocumentId == documentId)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _pending.Remove(key);
        }
    }

    private void AppendRows(SqlExecutionEvent executionEvent)
    {
        string? id = executionEvent.ResultSetId;
        if (string.IsNullOrWhiteSpace(id) || !_pending.TryGetValue(id, out PendingResult? pending))
            return;

        pending.EnsureGrid(_view);
        foreach (IReadOnlyList<object?> row in executionEvent.Rows!)
        {
            var target = new object[pending.Descriptor.Columns.Count + CustomDataGridView.TechColsNum];
            for (int index = 0; index < pending.Descriptor.Columns.Count && index < row.Count; index++)
                target[index] = row[index] ?? DBNull.Value;
            pending.Rows.Add(target);
        }

        if (!pending.Initialized && pending.Rows.Count >= 500)
        {
            pending.Grid!.InitGrid(true);
            pending.Initialized = true;
        }
    }

    private void FinalizeDocument(EditorDocumentId documentId)
    {
        foreach (PendingResult pending in _pending.Values.Where(value => value.DocumentId == documentId).ToArray())
        {
            // An empty result set still deserves a visible result tab.
            pending.EnsureGrid(_view);
            pending.Grid!.EnsureColumnList();
            pending.Grid.InitGrid(false);
            _view.RegisterPresentedResultGrid(pending.Tab!, pending.Grid);
        }
        RemoveDocument(documentId);
    }

    public void Dispose()
    {
        _disposed = true;
        _pending.Clear();
    }

    private sealed class PendingResult(EditorDocumentId documentId, ResultSetDescriptor descriptor)
    {
        public EditorDocumentId DocumentId { get; } = documentId;
        public ResultSetDescriptor Descriptor { get; } = descriptor;
        public List<object[]> Rows { get; } = [];
        public TabPagePicture? Tab { get; private set; }
        public CustomDataGridView? Grid { get; private set; }
        public bool Initialized { get; set; }

        public void EnsureGrid(IWinFormsSqlResultView view)
        {
            if (Grid is not null)
                return;
            Tab = view.CreatePresentedResultTab(DocumentId, Descriptor);
            Grid = view.CreatePresentedResultGrid(DocumentId, Tab, Descriptor, Rows);
        }
    }
}
