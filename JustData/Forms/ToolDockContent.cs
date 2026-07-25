using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

/// <summary>
/// A DockContent that wraps an arbitrary <see cref="Control"/> (e.g.
/// DatabaseExplorerControl, FilesControl, VariablesControl) as a
/// dockable/auto-hidable tool window — similar to the Visual Studio
/// Solution Explorer, Toolbox, etc.
/// </summary>
internal sealed class ToolDockContent : DockContent
{
    /// <summary>The wrapped content control.</summary>
    public Control Content { get; }

    /// <summary>Persist-string prefix used during layout serialization.</summary>
    private const string PersistPrefix = "tool:";

    public ToolDockContent(Control content, string title)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Text = title;
        TabText = title;

        // Tools can be docked left, right, bottom, top, or floated.
        DockAreas = DockAreas.DockLeft | DockAreas.DockRight
                  | DockAreas.DockBottom | DockAreas.DockTop
                  | DockAreas.Float;

        // Closing only hides the tool; the content stays alive.
        HideOnClose = true;
        CloseButton = true;
        CloseButtonVisible = true;

        // Ensure the control isn't orphaned from a previous parent.
        if (content.Parent is not null)
            content.Parent.Controls.Remove(content);
        content.Dock = DockStyle.Fill;
        if (content.MinimumSize != Size.Empty)
            MinimumSize = content.MinimumSize;
        content.SizeChanged += (_, _) =>
        {
            if (!content.IsDisposed && content.MinimumSize != Size.Empty)
                MinimumSize = content.MinimumSize;
        };
        Controls.Add(content);
    }

    /// <summary>
    /// Persist string used by DockPanelSuite layout serialization.
    /// Tools are serialized as <c>"tool:Title"</c>.
    /// </summary>
    protected override string GetPersistString()
    {
        return PersistPrefix + Text;
    }
}
