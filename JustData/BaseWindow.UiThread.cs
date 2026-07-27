namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    /// <summary>
    /// Executes a small UI update on the form thread when the shell is still
    /// alive. This is a general window helper, not part of SQL execution.
    /// </summary>
    private void InvokeOnMainWindow(Action action)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            if (IsHandleCreated)
                Invoke(action);
            return;
        }

        action();
    }
}
