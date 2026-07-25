using System.Runtime.InteropServices;

namespace AppBase.Common.WindowManagement;

public static partial class WindowNativeMethods
{
    #region User32.dll Methods

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    public static partial int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [LibraryImport("user32", EntryPoint = "SendMessageA")]
    public static partial int SendMessageA(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCapture();

    [LibraryImport("user32.dll")]
    public static partial int ShowWindow(IntPtr hWnd, uint Msg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int x, int y, int cx, int cy, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(ref Point lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PtInRect(ref RECT lprc, Point pt);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(IntPtr hWnd, ref RECT r);

    [LibraryImport("user32.dll")]
    public static partial int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

    [LibraryImport("user32.dll")]
    public static partial IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InflateRect(ref RECT lprc, int dx, int dy);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OffsetRect(ref RECT lprc, int dx, int dy);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlashWindowEx(ref FLASHWINFO pwfi);

    #endregion

    #region Gdi32.dll Methods

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateSolidBrush(int crColor);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr GetStockObject(int fnObject);

    [LibraryImport("gdi32.dll")]
    public static partial int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

    [LibraryImport("gdi32.dll")]
    public static partial int GetClipRgn(IntPtr hdc, IntPtr hrgn);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateEllipticRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [LibraryImport("gdi32.dll")]
    internal static partial int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, CombineRgnStyles fnCombineMode);

    [LibraryImport("gdi32.dll")]
    public static partial int ExcludeClipRect(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumChildWindows(IntPtr hWndParent, [MarshalAs(UnmanagedType.FunctionPtr)] EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #endregion

    #region UxTheme.dll Methods

    [LibraryImport("uxtheme.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

    #endregion

    #region Dwmapi.dll Methods

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmExtendFrameIntoClientArea(IntPtr hdc, ref MARGINS marInset);

    [LibraryImport("dwmapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DwmDefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, ref IntPtr result);

    #endregion
}
