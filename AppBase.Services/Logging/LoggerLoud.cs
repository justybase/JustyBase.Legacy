using AppBase.Common;

namespace AppBase.Services;

public class LoggerLoud : ILogger
{
    private Form? _form;

    public DialogResult MessageBox_Show(IWin32Window owner, string text, string caption, MessageBoxButtons messageBoxButtons, MessageBoxIcon boxIcon)
    {
        DialogResult r = DialogResult.OK;
        if (_form != null && _form.InvokeRequired)
        {
            _form.Invoke(() => r = MessageBox.Show(owner, text, caption, messageBoxButtons, boxIcon));
        }
        else
        {
            r = MessageBox.Show(owner, text, caption, messageBoxButtons, boxIcon);
        }
        return r;
    }

    public DialogResult MessageBox_Show(IWin32Window owner, string text)
    {
        DialogResult r = DialogResult.OK;
        if (_form != null && _form.InvokeRequired)
        {
            _form.Invoke(() => r = MessageBox.Show(text));
        }
        else if ((owner as Form)?.IsDisposed == false)
        {
            r = MessageBox.Show(owner, text);
        }
        return r;
    }


    public void Log(string message)
    {
        FileDiagnosticLog.Write(DiagnosticLogLevel.Info, message);
        if (_form == null)
            MessageBox.Show(message, "Log Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
        {
            if (_form != null && _form.InvokeRequired)
            {
                _form.Invoke(() => MessageBox.Show(_form, message, "Log Message", MessageBoxButtons.OK, MessageBoxIcon.Information));
            }
            else
            {
                MessageBox.Show(_form, message, "Log Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

    }
    public bool LogYesNo(string message)
    {
        FileDiagnosticLog.Write(DiagnosticLogLevel.Info, $"Prompt (Yes/No): {message}");
        DialogResult resut = DialogResult.Cancel;
        if (_form == null)
            resut = MessageBox.Show(message, "Log Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        else
        {
            if (_form != null && _form.InvokeRequired)
            {
                _form.Invoke(() => resut = MessageBox.Show(_form, message, "Log Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question));
            }
            else
            {
                resut = MessageBox.Show(_form, message, "Log Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
        }
        bool accepted = resut == DialogResult.Yes;
        FileDiagnosticLog.Write(DiagnosticLogLevel.Info, $"Prompt result: {(accepted ? "Yes" : "No")}");
        return accepted;
    }

    public void LogError(string message, Exception ex)
    {
        FileDiagnosticLog.WriteError(message, ex);
        string safeMessage = SensitiveDataRedactor.Redact(message);
        string safeException = SensitiveDataRedactor.RedactException(ex);
        if (_form == null || !_form.InvokeRequired)
            MessageBox.Show($"{safeMessage}\n{safeException}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else
            _form.Invoke(() =>
            MessageBox.Show(_form, $"{safeMessage}\n{safeException}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
    }

    public void SetWindow(Form? window)
    {
        _form = window;
    }

    public bool OnSchemaProblemMessage(string connectionName)
    {
        var btYes = new TaskDialogCommandLinkButton("&Yes", "Restart Just Data now")
        {
            Tag = 1
        };

        var btNo = new TaskDialogCommandLinkButton("&No", "I will restart Just Data later")
        {
            Tag = 2
        };

        TaskDialogPage td = new TaskDialogPage()
        {
            Caption = $"{connectionName} - problem",
            Heading = "Schema refreshing error",
            Text = "Restart ?",
            Buttons =
            {
                btYes,
                btNo
            },
            Icon = TaskDialogIcon.ShieldWarningYellowBar
        };
        td.Expander = new TaskDialogExpander("More info")
        {
            Position = TaskDialogExpanderPosition.AfterFootnote,
            Text = "If you see this message every time please contact your database administrator"
        };
        TaskDialogButton? res = null;
        // LoggerLoud is also used by startup/background services before a main
        // form has been assigned.  TaskDialog supports an owner-less dialog;
        // do not dereference the optional form in that path.
        if (_form is { IsDisposed: false } owner && owner.InvokeRequired)
        {
            owner.Invoke(() =>
            {
                res = ShowSchemaProblemDialog(owner, td);
            });
        }
        else
        {
            res = ShowSchemaProblemDialog(_form is { IsDisposed: false } ? _form : null, td);
        }

        return string.Equals(res?.Text, "&Yes", StringComparison.Ordinal);
    }

    /// <summary>Kept virtual so UI-free tests can verify owner-less behavior.</summary>
    protected virtual TaskDialogButton ShowSchemaProblemDialog(IWin32Window? owner, TaskDialogPage page) =>
        owner is null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
}
