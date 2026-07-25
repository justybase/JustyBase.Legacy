using AppBase.Common.Interfaces;
using JustData.Application.Editor;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

/// <summary>
/// A DockContent that wraps the editor SplitContainer (SQLUpperPanel top,
/// results bottom). Used by DockSuiteTabManager to host SQL editors as
/// docked documents instead of TabPage+TabControl.
/// </summary>
internal sealed class EditorDockContent : DockContent
{
    /// <summary>The SplitContainer (editor top / results bottom).</summary>
    public SplitContainer SplitContainer { get; }

    /// <summary>The SQL editor panel hosted in Panel1 of the splitter.</summary>
    public IEditorPanel EditorPanel { get; }

    /// <summary>The raw FastColoredTextBox for quick access.</summary>
    public FastColoredTextBoxNS.FastColoredTextBox Fctb => EditorPanel.CurrentTb;

    /// <summary>
    /// File-system path of the SQL file being edited, or <c>null</c> for unsaved tabs.
    /// Used by <see cref="GetPersistString"/> for dock-layout persistence.
    /// </summary>
    public string? FilePath { get; set; }

    public EditorDocumentId? DocumentId { get; set; }

    public EditorDockContent(SplitContainer splitter, IEditorPanel editor, string title)
    {
        SplitContainer = splitter ?? throw new ArgumentNullException(nameof(splitter));
        EditorPanel = editor ?? throw new ArgumentNullException(nameof(editor));

        Text = title;
        TabText = title;
        CloseButton = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.Document;
        // Let the document close without disappearing
        HideOnClose = false;

        // Detach from any existing parent and re-parent under this DockContent
        if (splitter.Parent is not null)
            splitter.Parent.Controls.Remove(splitter);
        splitter.Dock = DockStyle.Fill;
        Controls.Add(splitter);
    }

    /// <summary>Updates the title without re-creating the window.</summary>
    public void SetTitle(string title)
    {
        Text = title;
        TabText = title;
    }

    /// <summary>
    /// Persist string used by DockPanelSuite layout serialization.
    /// File-backed tabs return the full file path; unsaved tabs return
    /// <c>"unsaved://"</c> followed by the tab title and are NOT restored.
    /// </summary>
    protected override string GetPersistString()
    {
        if (!string.IsNullOrEmpty(FilePath))
            return FilePath;
        return "unsaved://" + Text;
    }
}
