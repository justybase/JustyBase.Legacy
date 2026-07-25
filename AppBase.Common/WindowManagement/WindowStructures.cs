using System.Runtime.InteropServices;

namespace AppBase.Common.WindowManagement;

/// <summary>
/// Contains Windows API structures used for window management operations.
/// </summary>

[StructLayout(LayoutKind.Sequential)]
public struct COPYDATASTRUCT
{
    public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
    public int cbData;       // The count of bytes in the message.
    public IntPtr lpData;    // The address of the message.
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public RECT(int X, int Y, int Width, int Height)
    {
        this.Left = X;
        this.Top = Y;
        this.Right = Width;
        this.Bottom = Height;
    }
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct PAINTSTRUCT
{
    public IntPtr hdc;
    public int fErase;
    public RECT rcPaint;
    public int fRestore;
    public int fIncUpdate;
    public int Reserved1;
    public int Reserved2;
    public int Reserved3;
    public int Reserved4;
    public int Reserved5;
    public int Reserved6;
    public int Reserved7;
    public int Reserved8;
}

[StructLayout(LayoutKind.Sequential)]
public struct MARGINS
{
    public int cxLeftWidth;
    public int cxRightWidth;
    public int cyTopHeight;
    public int cyBottomHeight;

    public MARGINS(int Left, int Right, int Top, int Bottom)
    {
        this.cxLeftWidth = Left;
        this.cxRightWidth = Right;
        this.cyTopHeight = Top;
        this.cyBottomHeight = Bottom;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct NCCALCSIZE_PARAMS
{
    public RECT rect0, rect1, rect2;
    public IntPtr lppos;
}

[StructLayout(LayoutKind.Sequential)]
public struct FLASHWINFO
{
    public uint cbSize;
    public IntPtr hwnd;
    public uint dwFlags;
    public uint uCount;
    public uint dwTimeout;
}
