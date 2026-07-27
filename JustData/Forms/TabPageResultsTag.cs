using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;

namespace JustyBaseLegacy.Services;


public sealed class TabPageResultsTag : IResultTab
{
    public bool Docked { get; set; }
    public bool IsPermanentDiagnostics { get; set; }
    public bool IsLog { get; set; }
    public TabControl? ParentControl { get; set; }
    public EditorDocumentId? DocumentId { get; set; }
    public string ResultSetId { get; set; } = Guid.NewGuid().ToString("N");
    public ResultSetKey? Key => DocumentId is { } documentId && !string.IsNullOrWhiteSpace(ResultSetId)
        ? new ResultSetKey(documentId, ResultSetId)
        : null;
    public bool HasDiagnostics { get; set; }
    public bool HasLog { get; set; }

    // IResultTab remains a transitional UI contract. Command ownership is now
    // held by the document execution VM and its adapter, not by a tab tag.
    bool IResultTab.CommandCanceled
    {
        get => false;
        set { }
    }
}
