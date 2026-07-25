using AppBase.Common;
using AppBase.Common.Interfaces;
using FastColoredTextBoxNS;
using JustyBaseLegacy.UI.DbForms;

namespace JustyBaseLegacy.UI;

/// <summary>
/// Owns the maps that associate TabPages with their editor panels and splitters.
/// Replaces the old _tabToEditorMap, _tabToSplitterMap, and OtherUtils CWT.
/// </summary>
internal sealed class TabManagerService : ITabManager
{
    private readonly Dictionary<TabPage, IEditorPanel> _tabToEditor = new();
    private readonly Dictionary<TabPage, SplitContainer> _tabToSplitter = new();
    private TabControl _mainTabControl;

    public void Initialize(TabControl mainTabControl)
    {
        _mainTabControl = mainTabControl ?? throw new ArgumentNullException(nameof(mainTabControl));
    }

    // ── Registration ──────────────────────────────────────────

    public void RegisterEditorTab(TabPage tabPage, IEditorPanel editorPanel, SplitContainer splitter)
    {
        _tabToEditor[tabPage] = editorPanel;
        _tabToSplitter[tabPage] = splitter;
    }

    public void UnregisterTab(TabPage tabPage)
    {
        _tabToEditor.Remove(tabPage);
        _tabToSplitter.Remove(tabPage);
    }

    // ── Lookup ────────────────────────────────────────────────

    public FastColoredTextBox? CurrentEditor
    {
        get
        {
            if (_mainTabControl is null || _mainTabControl.IsDisposed)
                return null;

            var selTab = _mainTabControl.SelectedTab;
            if (selTab?.Name == "NO FAST COLORED" || selTab is null)
                return null;

            if (_tabToEditor.TryGetValue(selTab, out var panel))
                return panel.CurrentTb;

            return null;
        }
    }

    public IEditorPanel? CurrentEditorPanel
    {
        get
        {
            if (_mainTabControl is null || _mainTabControl.IsDisposed)
                return null;

            var selTab = _mainTabControl.SelectedTab;
            if (selTab?.Name == "NO FAST COLORED" || selTab is null)
                return null;

            _tabToEditor.TryGetValue(selTab, out var panel);
            return panel;
        }
    }

    public SplitContainer? CurrentSplitContainer
    {
        get
        {
            if (_mainTabControl is null || _mainTabControl.IsDisposed)
                return null;

            var selTab = _mainTabControl.SelectedTab;
            if (selTab is not null && _tabToSplitter.TryGetValue(selTab, out var splitter))
                return splitter;

            return null;
        }
    }

    public SplitContainer? GetSplitContainerForTab(TabPage tabPage)
    {
        if (tabPage is not null && _tabToSplitter.TryGetValue(tabPage, out var splitter))
            return splitter;
        return null;
    }

    public IEditorPanel? GetEditorPanel(TabPage tabPage)
    {
        if (tabPage is not null && _tabToEditor.TryGetValue(tabPage, out var panel))
            return panel;
        return null;
    }

    /// <summary>
    /// Returns the FastColoredTextBox for a tab (convenience for callers that
    /// only need the editor, not the full IEditorPanel).
    /// Used to replace OtherUtils.GetFastColored(TabPage).
    /// </summary>
    public FastColoredTextBox? GetEditor(TabPage tabPage)
    {
        return GetEditorPanel(tabPage)?.CurrentTb;
    }

    public void SelectTab(TabPage tabPage)
    {
        if (_mainTabControl is not null && !_mainTabControl.IsDisposed
            && tabPage is not null && _mainTabControl.TabPages.Contains(tabPage))
        {
            _mainTabControl.SelectedTab = tabPage;
        }
    }
}
