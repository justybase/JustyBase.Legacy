using AppBase.Common.Interfaces;

namespace AppBase.Common;

/// <summary>
/// Abstracts the tag data attached to result tab pages.
/// Enables swapping TabPage+SplitContainer for DockSuite DockContent later.
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
    /// In DockSuite mode, this may be null since the ResultsDockContent
    /// manages its own tab pages without a DraggableTabControl.</summary>
    TabControl? ParentControl { get; set; }
}
