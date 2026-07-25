using System.ComponentModel;
using System.Runtime.InteropServices;

public partial class CueTextBox : TextBox
{
    [Localizable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Cue
    {
        get { return mCue; }
        set { mCue = value; updateCue(); }
    }

    private void updateCue()
    {
        if (this.IsHandleCreated && mCue != null)
        {
            SendMessage(this.Handle, 0x1501, (IntPtr)1, mCue);
        }
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        updateCue();
    }
    private string mCue = string.Empty;

    // PInvoke
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
}
