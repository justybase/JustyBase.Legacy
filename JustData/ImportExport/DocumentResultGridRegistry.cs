using DatabaseDataGridView.WinForms;
using JustData.Application.Editor;

namespace JustyBaseLegacy.UI.ImportExport;

/// <summary>Document-scoped lookup used by result export; it never owns a form.</summary>
public interface IDocumentResultGridRegistry
{
    void Register(EditorDocumentId documentId, string resultSetId, CustomDataGridView grid);
    bool TryGet(EditorDocumentId documentId, string resultSetId, out CustomDataGridView? grid);
    IReadOnlyList<DocumentResultGrid> GetDocument(EditorDocumentId documentId);
    bool TryFind(EditorDocumentId documentId, CustomDataGridView grid, out string? resultSetId);
    void RemoveDocument(EditorDocumentId documentId);
    void RemoveResult(string resultSetId);
}

public sealed record DocumentResultGrid(string ResultSetId, CustomDataGridView Grid);

public sealed class DocumentResultGridRegistry : IDocumentResultGridRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<(EditorDocumentId DocumentId, string ResultSetId), CustomDataGridView> _grids = [];

    public void Register(EditorDocumentId documentId, string resultSetId, CustomDataGridView grid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultSetId);
        ArgumentNullException.ThrowIfNull(grid);
        lock (_sync)
            _grids[(documentId, resultSetId)] = grid;
    }

    public bool TryGet(EditorDocumentId documentId, string resultSetId, out CustomDataGridView? grid)
    {
        lock (_sync)
        {
            bool found = _grids.TryGetValue((documentId, resultSetId), out CustomDataGridView? value);
            grid = value;
            return found;
        }
    }

    public IReadOnlyList<DocumentResultGrid> GetDocument(EditorDocumentId documentId)
    {
        lock (_sync)
            return _grids
                .Where(item => item.Key.DocumentId == documentId)
                .Select(item => new DocumentResultGrid(item.Key.ResultSetId, item.Value))
                .ToArray();
    }

    public bool TryFind(EditorDocumentId documentId, CustomDataGridView grid, out string? resultSetId)
    {
        ArgumentNullException.ThrowIfNull(grid);
        lock (_sync)
        {
            foreach (var item in _grids)
            {
                if (item.Key.DocumentId == documentId && ReferenceEquals(item.Value, grid))
                {
                    resultSetId = item.Key.ResultSetId;
                    return true;
                }
            }
        }
        resultSetId = null;
        return false;
    }

    public void RemoveDocument(EditorDocumentId documentId)
    {
        lock (_sync)
        {
            foreach (var key in _grids.Keys.Where(key => key.DocumentId == documentId).ToArray())
                _grids.Remove(key);
        }
    }

    public void RemoveResult(string resultSetId)
    {
        lock (_sync)
        {
            foreach (var key in _grids.Keys.Where(key => key.ResultSetId == resultSetId).ToArray())
                _grids.Remove(key);
        }
    }
}
