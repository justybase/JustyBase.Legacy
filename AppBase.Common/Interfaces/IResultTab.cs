using AppBase.Common.Interfaces;

namespace AppBase.Common;

/// <summary>
/// Abstracts the tag data attached to result tab pages.
/// Keeps the result-tab contract independent from the concrete tab host.
/// </summary>
public interface IResultTab
{
    /// <summary>Whether the result tab is pinned (docked) and should not be auto-closed.</summary>
    bool Docked { get; set; }

    /// <summary>Whether this is a permanent diagnostics tab (never auto-closed).</summary>
    bool IsPermanentDiagnostics { get; set; }

    /// <summary>Whether the associated command has been canceled.</summary>
    bool CommandCanceled { get; set; }

    /// <summary>Reference to the parent tab control (results tab strip).
    /// Results always live in the document's own tab strip; this is null only
    /// for synthetic tabs that have not been attached yet.</summary>
    TabControl? ParentControl { get; set; }
}
