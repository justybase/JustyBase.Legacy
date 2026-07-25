namespace AppBase.Common;

public interface ILogger
{
    DialogResult MessageBox_Show(IWin32Window owner, string text, string caption, MessageBoxButtons messageBoxButtons, MessageBoxIcon boxIcon);
    public DialogResult MessageBox_Show(IWin32Window owner, string text);
    void Log(string message);
    bool LogYesNo(string message);
    void LogError(string message, Exception ex);
    void SetWindow(Form? window);
    bool OnSchemaProblemMessage(string connectionName);
}
