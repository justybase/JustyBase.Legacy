using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Command keys are processed before KeyDown. Keep the SQL-document
        // and document-tab shortcuts independent of the removed terminal UI.
        Keys key = keyData & Keys.KeyCode;
        if ((keyData & Keys.Control) != 0 && key is Keys.N or Keys.T)
        {
            OpenNewSqlDocument();
            return true;
        }

        if ((keyData & Keys.Control) != 0 && key == Keys.Tab && EditorTabPages.Count > 1)
        {
            int currentIndex = ActiveEditorTabPage is TabPage activeTab
                ? EditorTabPages.ToList().IndexOf(activeTab)
                : -1;
            int nextIndex = (Math.Max(0, currentIndex) + 1) % EditorTabPages.Count;
            _tabManager.SelectTab(EditorTabPages[nextIndex]);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
