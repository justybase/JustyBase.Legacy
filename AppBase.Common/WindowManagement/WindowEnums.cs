namespace AppBase.Common.WindowManagement;

/// <summary>
/// Contains enums used for window management operations.
/// </summary>

public enum HIT_CONSTANTS : int
{
    HTERROR = -2,
    HTTRANSPARENT = -1,
    HTNOWHERE = 0,
    HTCLIENT = 1,
    HTCAPTION = 2,
    HTSYSMENU = 3,
    HTGROWBOX = 4,
    HTMENU = 5,
    HTHSCROLL = 6,
    HTVSCROLL = 7,
    HTMINBUTTON = 8,
    HTMAXBUTTON = 9,
    HTLEFT = 10,
    HTRIGHT = 11,
    HTTOP = 12,
    HTTOPLEFT = 13,
    HTTOPRIGHT = 14,
    HTBOTTOM = 15,
    HTBOTTOMLEFT = 16,
    HTBOTTOMRIGHT = 17,
    HTBORDER = 18,
    HTOBJECT = 19,
    HTCLOSE = 20,
    HTHELP = 21
}

internal enum CombineRgnStyles : int
{
    RGN_AND = 1,
    RGN_OR = 2,
    RGN_XOR = 3,
    RGN_DIFF = 4,
    RGN_COPY = 5,
    RGN_MIN = RGN_AND,
    RGN_MAX = RGN_COPY
}
