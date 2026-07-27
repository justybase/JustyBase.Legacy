using DatabaseDataGridView.WinForms;
using JustData.Application.Editor;
using JustData.Application.Sql;

namespace JustyBaseLegacy.UI.ImportExport;

/// <summary>Document-scoped lookup used by result export; it never owns a form.</summary>
public interface IDocumentResultGridRegistry
{
    void Register(ResultSetKey key, CustomDataGridView grid);
    bool TryGet(ResultSetKey key, out CustomDataGridView? grid);
    IReadOnlyList<DocumentResultGrid> GetDocument(EditorDocumentId documentId);
    bool TryFind(EditorDocumentId documentId, CustomDataGridView grid, out ResultSetKey? key);
    void RemoveDocument(EditorDocumentId documentId);
    void RemoveResult(ResultSetKey key);
}

public sealed record DocumentResultGrid(ResultSetKey Key, CustomDataGridView Grid)
{
    public string ResultSetId => Key.ResultSetId;
}

public sealed class DocumentResultGridRegistry : IDocumentResultGridRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<ResultSetKey, CustomDataGridView> _grids = [];

    public void Register(ResultSetKey key, CustomDataGridView grid)
    {
        if (!key.IsValid)
            throw new ArgumentException("A document-scoped result-set key is required.", nameof(key));
        ArgumentNullException.ThrowIfNull(grid);
        lock (_sync)
            _grids[key] = grid;
    }

    public bool TryGet(ResultSetKey key, out CustomDataGridView? grid)
    {
        lock (_sync)
        {
            bool found = _grids.TryGetValue(key, out CustomDataGridView? value);
            grid = value;
            return found;
        }
    }

    public IReadOnlyList<DocumentResultGrid> GetDocument(EditorDocumentId documentId)
    {
        lock (_sync)
            return _grids
                .Where(item => item.Key.DocumentId == documentId)
                .Select(item => new DocumentResultGrid(item.Key, item.Value))
                .ToArray();
    }

    public bool TryFind(EditorDocumentId documentId, CustomDataGridView grid, out ResultSetKey? key)
    {
        ArgumentNullException.ThrowIfNull(grid);
        lock (_sync)
        {
            foreach (var item in _grids)
            {
                if (item.Key.DocumentId == documentId && ReferenceEquals(item.Value, grid))
                {
                    key = item.Key;
                    return true;
                }
            }
        }
        key = null;
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

    public void RemoveResult(ResultSetKey key)
    {
        lock (_sync)
            _grids.Remove(key);
    }
}
