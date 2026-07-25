namespace AppBase.Common.WindowManagement;

/// <summary>
/// Contains Windows API constants used for window management operations.
/// </summary>
public static class WindowConstants
{
    // Window Messages
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;
    public const int WM_COPYDATA = 0x004A;
    public const int WM_SETREDRAW = 11;
    public const int WM_SYSCOMMAND = 0x112;
    public const int WM_CREATE = 0x0001;
    public const int WM_NCCALCSIZE = 0x83;
    public const int WM_NCHITTEST = 0x84;
    public const int WM_SIZE = 0x5;
    public const int WM_PAINT = 0xF;
    public const int WM_ERASEBKGND = 0x14;
    public const int WM_TIMER = 0x113;
    public const int WM_ACTIVATE = 0x6;
    public const int WM_NCMOUSEMOVE = 0xA0;
    public const int WM_NCMOUSEHOVER = 0x02A0;
    public const int WM_NCMOUSELEAVE = 0x02A2;
    public const int WM_NCLBUTTONUP = 0xA2;
    public const int WM_NCLBUTTONDBLCLK = 0xA3;
    public const int WM_NCRBUTTONDOWN = 0xA4;
    public const int WM_NCRBUTTONUP = 0xA5;
    public const int WM_NCRBUTTONDBLCLK = 0xA6;
    public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
    public const int WM_GETTITLEBARINFOEX = 0x033F;

    // System Commands
    public const int SC_RESTORE = 0xF120;
    public const int SC_MAXIMIZE = 0xF030;

    // Window Position Flags
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_NOREDRAW = 0x0008;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_FRAMECHANGED = 0x0020;
    public const int SWP_SHOWWINDOW = 0x0040;
    public const int SWP_HIDEWINDOW = 0x0080;
    public const int SWP_NOCOPYBITS = 0x0100;
    public const int SWP_NOOWNERZORDER = 0x0200;
    public const int SWP_NOSENDCHANGING = 0x0400;

    // Redraw Flags
    public const int RDW_INVALIDATE = 0x0001;
    public const int RDW_INTERNALPAINT = 0x0002;
    public const int RDW_ERASE = 0x0004;
    public const int RDW_VALIDATE = 0x0008;
    public const int RDW_NOINTERNALPAINT = 0x0010;
    public const int RDW_NOERASE = 0x0020;
    public const int RDW_NOCHILDREN = 0x0040;
    public const int RDW_ALLCHILDREN = 0x0080;
    public const int RDW_UPDATENOW = 0x0100;
    public const int RDW_ERASENOW = 0x0200;
    public const int RDW_FRAME = 0x0400;
    public const int RDW_NOFRAME = 0x0800;

    // Frame and Caption Dimensions
    public const int FRAME_WIDTH = 8;
    public const int CAPTION_HEIGHT = 30;
    public const int FRAME_SMWIDTH = 4;
    public const int CAPTION_SMHEIGHT = 24;

    // Virtual Key Codes
    public const int VK_LBUTTON = 0x1;
    public const int VK_RBUTTON = 0x2;
    public const int KEY_PRESSED = 0x1000;

    // Miscellaneous
    public const int SM_SWAPBUTTON = 23;
    public const int BLACK_BRUSH = 4;
    public const uint SW_RESTORE = 0x09;

    // Window Validation Rectangle Flags
    public const int WVR_ALIGNTOP = 0x0010;
    public const int WVR_ALIGNLEFT = 0x0020;
    public const int WVR_ALIGNBOTTOM = 0x0040;
    public const int WVR_ALIGNRIGHT = 0x0080;
    public const int WVR_HREDRAW = 0x0100;
    public const int WVR_VREDRAW = 0x0200;
    public const int WVR_REDRAW = (WVR_HREDRAW | WVR_VREDRAW);
    public const int WVR_VALIDRECTS = 0x400;

    // Flash Window Constants
    public const uint FLASHW_ALL = 3;
    public const uint FLASHW_TIMERNOFG = 12;

    // DWM window attributes (Windows 11 caption chrome)
    public const int DWMWA_BORDER_COLOR = 34;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20 = 19;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_CAPTION_COLOR = 35;
    public const int DWMWA_TEXT_COLOR = 36;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    public const int DWMSBT_NONE = 1;
    public const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);
    public const int WM_NCPAINT = 0x85;
    public static readonly IntPtr MSG_HANDLED = new IntPtr(0);
}
