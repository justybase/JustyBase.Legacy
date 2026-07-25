using AppBase.Common.Interfaces;
using FastColoredTextBoxNS;

namespace AppBase.Common;

/// <summary>
/// Manages the lifecycle of editor tabs and their associated controls.
/// Decouples tab-map bookkeeping from BaseWindow so the layout
/// (TabPage+SplitContainer vs. DockSuite DockContent) can be swapped later.
/// </summary>
public interface ITabManager
{
    // ── Initialization ────────────────────────────────────────

    /// <summary>Initializes the tab manager with the main tab control (created in Designer).</summary>
    void Initialize(TabControl mainTabControl);

    // ── Registration ──────────────────────────────────────────

    /// <summary>Registers a new editor tab with its panel and splitter.</summary>
    void RegisterEditorTab(TabPage tabPage, IEditorPanel editorPanel, SplitContainer splitter);

    /// <summary>Unregisters a tab and cleans up internal maps.</summary>
    void UnregisterTab(TabPage tabPage);

    // ── Lookup ────────────────────────────────────────────────

    /// <summary>Currently active FastColoredTextBox (from the selected tab).</summary>
    FastColoredTextBox? CurrentEditor { get; }

    /// <summary>Currently active editor panel (SQLUpperPanel).</summary>
    IEditorPanel? CurrentEditorPanel { get; }

    /// <summary>Currently active SplitContainer (editor/results split).</summary>
    SplitContainer? CurrentSplitContainer { get; }

    /// <summary>Gets the SplitContainer associated with a specific tab.</summary>
    SplitContainer? GetSplitContainerForTab(TabPage tabPage);

    /// <summary>Gets the editor panel associated with a specific tab.</summary>
    IEditorPanel? GetEditorPanel(TabPage tabPage);

    /// <summary>Gets the FastColoredTextBox for a specific tab (convenience).</summary>
    FastColoredTextBox? GetEditor(TabPage tabPage);

    /// <summary>Activates the editor tab identified by the given TabPage.</summary>
    void SelectTab(TabPage tabPage);
}
