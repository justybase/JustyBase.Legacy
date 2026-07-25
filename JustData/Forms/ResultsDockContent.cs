using AppBase.Common;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

/// <summary>
/// A DockContent that hosts SQL execution results (Results, Log, Diagnostic
/// tabs) in a standard <see cref="TabControl"/> docked below the document
/// area — similar to the Visual Studio Output / Error List pane.
///
/// </summary>
internal sealed class ResultsDockContent : DockContent
{
    private TabControl _activeTabControl;

    /// <summary>The currently displayed TabControl (swapped per active document).</summary>
    public TabControl TabControl => _activeTabControl;

    public ResultsDockContent()
    {
        Text = "Results";
        TabText = "Results";
        CloseButton = true;
        CloseButtonVisible = true;
        HideOnClose = true;

        // DockSuite needs Document here to create a pane nested below the SQL
        // document pane. It remains a dedicated Results content, never an SQL
        // editor document.
        DockAreas = DockAreas.DockLeft | DockAreas.DockRight
                  | DockAreas.DockBottom | DockAreas.DockTop
                  | DockAreas.Document | DockAreas.Float;

        _activeTabControl = CreateResultsTabControl();
        Controls.Add(_activeTabControl);
    }

    /// <summary>
    /// Swaps the displayed TabControl to <paramref name="newTabControl"/>.
    /// Hides the old one, shows the new one — preserving dock fill.
    /// </summary>
    public void SwapTabControl(TabControl newTabControl)
    {
        if (newTabControl is null || newTabControl == _activeTabControl)
            return;

        Controls.Remove(_activeTabControl);
        _activeTabControl = newTabControl;
        _activeTabControl.Dock = DockStyle.Fill;
        Controls.Add(_activeTabControl);
        _activeTabControl.BringToFront();
    }

    /// <summary>Replaces the active result strip after its owner document closes.</summary>
    public void ResetTabControl()
    {
        var replacement = CreateResultsTabControl();
        Controls.Remove(_activeTabControl);
        _activeTabControl = replacement;
        Controls.Add(_activeTabControl);
        _activeTabControl.BringToFront();
    }

    private static TabControl CreateResultsTabControl() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        Padding = new Point(12, 4)
    };

    /// <summary>
    /// Adds a new result tab page with the given title.
    /// </summary>
    public TabPage AddResultTab(string title)
    {
        var page = new TabPage(title)
        {
            Text = title,
            UseVisualStyleBackColor = true
        };
        _activeTabControl.TabPages.Add(page);
        _activeTabControl.SelectedTab = page;
        return page;
    }

    /// <summary>
    /// Persist string — results panel is persisted so layout saves/restores
    /// its dock state and position.
    /// </summary>
    protected override string GetPersistString()
    {
        return "tool:Results";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if ((keyData & Keys.Control) != 0 && key is Keys.N or Keys.T)
        {
            (FindForm() as BaseWindow)?.OpenNewSqlDocument();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
