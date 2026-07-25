using System.Runtime.InteropServices;

namespace JustDataAdditionalForms;

public class CustomProgressBar : ProgressBar
{

}

public static partial class ModifyProgressBarColor
{
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
    public static void SetState(this ProgressBar pBar, int state)
    {
        SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
    }
}
