using DatabaseDataGridView.WinForms;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;
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
    void RemovePresentedResult(ResultSetKey key, TabPage? pendingTab = null, CustomDataGridView? pendingGrid = null);
}

/// <summary>
/// WinForms-only consumer of the document SQL event stream.  It owns the
/// transient grid buffers; the execution VM retains only result metadata.
/// </summary>
internal sealed class WinFormsSqlResultPresenter : IDisposable
{
    private readonly IWinFormsSqlResultView _view;
    private readonly Dictionary<ResultSetKey, PendingResult> _pending = [];
    private readonly HashSet<ResultSetKey> _removedResults = [];
    private readonly Dictionary<EditorDocumentId, SqlExecutionViewModel> _executions = [];
    private bool _disposed;

    public WinFormsSqlResultPresenter(IWinFormsSqlResultView view) => _view = view;

    public void Attach(SqlExecutionViewModel execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (_executions.TryGetValue(execution.DocumentId, out SqlExecutionViewModel? existing)
            && ReferenceEquals(existing, execution))
            return;

        if (existing is not null)
        {
            existing.ResultRemoved -= OnResultRemoved;
            existing.ResultAdded -= OnResultAdded;
        }

        _executions[execution.DocumentId] = execution;
        execution.ResultRemoved += OnResultRemoved;
        execution.ResultAdded += OnResultAdded;
    }

    public void Handle(SqlExecutionEvent executionEvent)
    {
        // Attach is performed while the editor document is created and is the
        // authoritative lifecycle registration for this UI projection.  The
        // workspace collection can lag the WinForms tab mapping during editor
        // creation, causing the first ResultSet/Rows events to be discarded.
        if (_disposed || !_executions.ContainsKey(executionEvent.DocumentId))
            return;

        if (executionEvent.IsUiProjectionOwned)
            return;

        if (_view.InvokeRequired)
        {
            _view.BeginInvoke(() => Handle(executionEvent));
            return;
        }

        switch (executionEvent.Kind)
        {
            case SqlExecutionEventKind.Started:
                _removedResults.RemoveWhere(key => key.DocumentId == executionEvent.DocumentId);
                break;
            case SqlExecutionEventKind.ResultSet when executionEvent.ResultSet is not null:
            {
                var key = new ResultSetKey(executionEvent.DocumentId, executionEvent.ResultSet.ResultSetId);
                if (_removedResults.Contains(key))
                    break;
                if (!_pending.TryGetValue(key, out PendingResult? pending))
                {
                    pending = new PendingResult(key, executionEvent.ResultSet);
                    _pending[key] = pending;
                }
                // A result-set boundary is enough to show an empty result.
                // Do not wait for a provider-specific first Rows event: some
                // drivers report the schema before dispatching row batches.
                pending.EnsureGrid(_view);
                break;
            }
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
        if (_executions.Remove(documentId, out SqlExecutionViewModel? execution))
        {
            execution.ResultRemoved -= OnResultRemoved;
            execution.ResultAdded -= OnResultAdded;
        }

        RemovePending(documentId);
        _removedResults.RemoveWhere(key => key.DocumentId == documentId);
    }

    public void RemovePendingResult(ResultSetKey key)
    {
        if (_disposed || !key.IsValid)
            return;

        if (_view.InvokeRequired)
        {
            _view.BeginInvoke(() => RemovePendingResult(key));
            return;
        }

        _removedResults.Add(key);
        if (_pending.Remove(key, out PendingResult? pending))
        {
            _view.RemovePresentedResult(key, pending.Tab, pending.Grid);
        }
    }

    private void RemovePending(EditorDocumentId documentId)
    {
        foreach (ResultSetKey key in _pending
            .Where(pair => pair.Value.DocumentId == documentId)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _pending.Remove(key);
        }
    }

    private void AppendRows(SqlExecutionEvent executionEvent)
    {
        if (string.IsNullOrWhiteSpace(executionEvent.ResultSetId))
            return;

        var key = new ResultSetKey(executionEvent.DocumentId, executionEvent.ResultSetId);
        if (!_pending.TryGetValue(key, out PendingResult? pending))
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
        RemovePending(documentId);
    }

    public void Dispose()
    {
        _disposed = true;
        foreach (SqlExecutionViewModel execution in _executions.Values)
        {
            execution.ResultRemoved -= OnResultRemoved;
            execution.ResultAdded -= OnResultAdded;
        }
        _executions.Clear();
        _pending.Clear();
    }

    private void OnResultRemoved(ResultSetKey key)
    {
        if (_disposed)
            return;

        // Result-close originates from both a WinForms click and the clean VM
        // (for example ClearResults at the start of a new run). Always queue
        // the view update to avoid re-entering legacy TabControl handlers that
        // are currently removing the same page.
        _view.BeginInvoke(() =>
        {
            if (_disposed)
                return;

            _removedResults.Add(key);
            TabPage? pendingTab = null;
            CustomDataGridView? pendingGrid = null;
            if (_pending.Remove(key, out PendingResult? pending))
            {
                pendingTab = pending.Tab;
                pendingGrid = pending.Grid;
            }

            _view.RemovePresentedResult(key, pendingTab, pendingGrid);
        });
    }

    private void OnResultAdded(ResultSetKey key, ResultSetDescriptor descriptor)
    {
        if (_disposed || !_executions.ContainsKey(key.DocumentId))
            return;
        if (_removedResults.Contains(key))
            return;

        void EnsureResultProjection()
        {
            if (_disposed || !_executions.ContainsKey(key.DocumentId))
                return;
            if (!_pending.TryGetValue(key, out PendingResult? pending))
            {
                pending = new PendingResult(key, descriptor);
                _pending[key] = pending;
            }
            pending.EnsureGrid(_view);
        }

        if (_view.InvokeRequired)
            _view.BeginInvoke(EnsureResultProjection);
        else
            EnsureResultProjection();
    }

    private sealed class PendingResult(ResultSetKey key, ResultSetDescriptor descriptor)
    {
        public EditorDocumentId DocumentId { get; } = key.DocumentId;
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
