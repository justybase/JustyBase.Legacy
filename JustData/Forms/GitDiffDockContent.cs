using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

/// <summary>Document host for the Git side-by-side diff viewer.</summary>
internal sealed class GitDiffDockContent : DockContent
{
    public Controls.GitDiffControl DiffControl { get; }

    public GitDiffDockContent(Controls.GitDiffControl diffControl, string title)
    {
        DiffControl = diffControl ?? throw new ArgumentNullException(nameof(diffControl));
        Text = title;
        TabText = title;
        CloseButton = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.Document | DockAreas.Float;
        HideOnClose = false;

        if (diffControl.Parent is not null)
            diffControl.Parent.Controls.Remove(diffControl);
        diffControl.Dock = DockStyle.Fill;
        Controls.Add(diffControl);
    }

    public void SetTitle(string title)
    {
        Text = title;
        TabText = title;
    }

    protected override string GetPersistString() => "git-diff://";
}
