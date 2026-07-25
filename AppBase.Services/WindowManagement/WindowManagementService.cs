using AppBase.Common;
using AppBase.Common.WindowManagement;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AppBase.Services;

/// <summary>
/// Implementation of window management services providing inter-process communication,
/// window manipulation, and custom frame handling functionality.
/// </summary>
public class WindowManagementService : IWindowManagementService
{

    public void SendDataMessage(Process targetProcess, string msg)
    {
        //Copy the string message to a global memory area in unicode format
        IntPtr _stringMessageBuffer = Marshal.StringToHGlobalUni(msg);

        //Prepare copy data structure
        COPYDATASTRUCT _copyData = new COPYDATASTRUCT();
        _copyData.dwData = IntPtr.Zero;
        _copyData.lpData = _stringMessageBuffer;
        _copyData.cbData = msg.Length * 2;//Number of bytes required for marshalling this string as a series of unicode characters
        IntPtr _copyDataBuff = IntPtrAlloc(_copyData);

        //Send message to the other process
        WindowNativeMethods.SendMessageA(targetProcess.MainWindowHandle, WindowConstants.WM_COPYDATA, IntPtr.Zero, _copyDataBuff);

        Marshal.FreeHGlobal(_copyDataBuff);
        Marshal.FreeHGlobal(_stringMessageBuffer);
    }

    // Allocate a pointer to an arbitrary structure on the global heap.
    private static IntPtr IntPtrAlloc<T>(T param)
    {
        IntPtr retval = Marshal.AllocHGlobal(Marshal.SizeOf(param));
        Marshal.StructureToPtr(param, retval, false);
        return retval;
    }

    public Process SendMessageToAnotherInstances(string[] args)
    {
        Process _currentProc = Process.GetCurrentProcess();
        Process[] _allProcs = Process.GetProcessesByName(_currentProc.ProcessName);

        for (int i = 0; i < _allProcs.Length; i++)
        {
            if (_allProcs[i].Id != _currentProc.Id)
                SendDataMessage(_allProcs[i], args[0]);
        }

        return null;
    }

    /// <summary>
    /// Pure hit-testing logic — no P/Invoke, no Form dependency.
    /// Takes cursor position relative to the window's top-left corner.
    /// </summary>
    public static HIT_CONSTANTS HitTest(
        int cursorRelativeX, int cursorRelativeY,
        int windowWidth, int windowHeight,
        int frameWidth, int frameHeight,
        int frameOffset, int captionTopHeight)
    {
        if (IsInRect(cursorRelativeX, cursorRelativeY, 0, 0, frameWidth, frameHeight))
            return HIT_CONSTANTS.HTTOPLEFT;

        if (IsInRect(cursorRelativeX, cursorRelativeY, windowWidth - frameWidth, 0, windowWidth, frameHeight))
            return HIT_CONSTANTS.HTTOPRIGHT;

        if (IsInRect(cursorRelativeX, cursorRelativeY, frameWidth, 0, windowWidth - (frameWidth * 2) - frameOffset, frameHeight))
            return HIT_CONSTANTS.HTTOP;

        if (IsInRect(cursorRelativeX, cursorRelativeY, frameWidth, frameHeight, windowWidth - ((frameWidth * 2) + frameOffset), captionTopHeight))
            return HIT_CONSTANTS.HTCAPTION;

        if (IsInRect(cursorRelativeX, cursorRelativeY, 0, frameHeight, frameWidth, windowHeight - frameHeight))
            return HIT_CONSTANTS.HTLEFT;

        if (IsInRect(cursorRelativeX, cursorRelativeY, 0, windowHeight - frameHeight, frameWidth, windowHeight))
            return HIT_CONSTANTS.HTBOTTOMLEFT;

        if (IsInRect(cursorRelativeX, cursorRelativeY, frameWidth, windowHeight - frameHeight, windowWidth - frameWidth, windowHeight))
            return HIT_CONSTANTS.HTBOTTOM;

        if (IsInRect(cursorRelativeX, cursorRelativeY, windowWidth - frameWidth, windowHeight - frameHeight, windowWidth, windowHeight))
            return HIT_CONSTANTS.HTBOTTOMRIGHT;

        if (IsInRect(cursorRelativeX, cursorRelativeY, windowWidth - frameWidth, frameHeight, windowWidth, windowHeight - frameHeight))
            return HIT_CONSTANTS.HTRIGHT;

        return HIT_CONSTANTS.HTCLIENT;
    }

    private static bool IsInRect(int x, int y, int left, int top, int right, int bottom)
        => x >= left && x < right && y >= top && y < bottom;

    public HIT_CONSTANTS HitTest(Form form, int FrameWidth, int FrameHeight, int iFrameOffset, ref MARGINS _tMargins)
    {
        RECT windowRect = new RECT();
        Point cursorPoint = new Point();
        WindowNativeMethods.GetCursorPos(ref cursorPoint);
        WindowNativeMethods.GetWindowRect(form.Handle, ref windowRect);
        cursorPoint.X -= windowRect.Left;
        cursorPoint.Y -= windowRect.Top;
        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;

        return HitTest(
            cursorPoint.X, cursorPoint.Y,
            width, height,
            FrameWidth, FrameHeight,
            iFrameOffset, _tMargins.cyTopHeight);
    }

    public void FrameChanged(Form form)
    {
        RECT rcClient = new RECT();
        WindowNativeMethods.GetWindowRect(form.Handle, ref rcClient);
        // force a calc size message
        WindowNativeMethods.SetWindowPos(form.Handle,
                     IntPtr.Zero,
                     rcClient.Left, rcClient.Top,
                     rcClient.Right - rcClient.Left, rcClient.Bottom - rcClient.Top,
                     WindowConstants.SWP_FRAMECHANGED);
    }

    public bool FlashWindowEx(Form form)
    {
        IntPtr hWnd = form.Handle;
        FLASHWINFO fInfo = new FLASHWINFO();

        fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
        fInfo.hwnd = hWnd;
        fInfo.dwFlags = WindowConstants.FLASHW_ALL | WindowConstants.FLASHW_TIMERNOFG;
        fInfo.uCount = uint.MaxValue;
        fInfo.dwTimeout = 0;
        return WindowNativeMethods.FlashWindowEx(ref fInfo);
    }

    #region Static Utility Methods

    /// <summary>
    /// Paints custom window frame elements (DWM glass path — Windows 11).
    /// </summary>
    public static void PaintThis(IntPtr hdc, RECT rc, bool _bExtendIntoFrame, bool _bPaintWindow, bool _bDrawCaption,
        int captionHeight, int FrameWidth, int captionButtonReserve,
        Form form, ref MARGINS _tMargins, ref RECT _tClientRect)
    {
        RECT clientRect = new RECT();
        WindowNativeMethods.GetClientRect(form.Handle, ref clientRect);
        if (_bExtendIntoFrame)
        {
            clientRect.Left = _tClientRect.Left - _tMargins.cxLeftWidth;
            clientRect.Top = _tMargins.cyTopHeight;
            clientRect.Right -= _tMargins.cxRightWidth;
            clientRect.Bottom -= _tMargins.cyBottomHeight;
        }
        else if (!_bPaintWindow)
        {
            clientRect.Left = _tMargins.cxLeftWidth;
            clientRect.Top = _tMargins.cyTopHeight;
            clientRect.Right -= _tMargins.cxRightWidth;
            clientRect.Bottom -= _tMargins.cyBottomHeight;
        }

        static RECT ExcludeCaptionButtons(RECT area, bool extendIntoFrame, int reserve)
        {
            if (!extendIntoFrame || reserve <= 0)
            {
                return area;
            }

            RECT trimmed = area;
            trimmed.Right = Math.Max(trimmed.Left, trimmed.Right - reserve);
            return trimmed;
        }

        if (_bExtendIntoFrame && captionButtonReserve > 0)
        {
            RECT clipClient = new RECT();
            WindowNativeMethods.GetClientRect(form.Handle, ref clipClient);
            int bandTop = _tMargins.cyTopHeight > 0 ? _tMargins.cyTopHeight : clipClient.Bottom;
            WindowNativeMethods.ExcludeClipRect(
                hdc,
                Math.Max(0, clipClient.Right - captionButtonReserve),
                0,
                clipClient.Right,
                bandTop);
        }

        if (!_bPaintWindow)
        {
            int frameColor = ColorTranslator.ToWin32(form.BackColor);
            RECT frameRect = ExcludeCaptionButtons(rc, _bExtendIntoFrame, captionButtonReserve);
            IntPtr hb;
            using (ClippingRegion cp = new ClippingRegion(hdc, clientRect, rc))
            {
                hb = WindowNativeMethods.CreateSolidBrush(frameColor);
                WindowNativeMethods.FillRect(hdc, ref frameRect, hb);
                WindowNativeMethods.DeleteObject(hb);
            }

            if (_bExtendIntoFrame && captionButtonReserve > 0)
            {
                RECT clipClient = new RECT();
                WindowNativeMethods.GetClientRect(form.Handle, ref clipClient);
                int bandTop = _tMargins.cyTopHeight > 0 ? _tMargins.cyTopHeight : clipClient.Bottom;
                WindowNativeMethods.ExcludeClipRect(
                    hdc,
                    Math.Max(0, clipClient.Right - captionButtonReserve),
                    0,
                    clipClient.Right,
                    bandTop);
            }

            hb = WindowNativeMethods.CreateSolidBrush(frameColor);
            WindowNativeMethods.FillRect(hdc, ref clientRect, hb);
            WindowNativeMethods.DeleteObject(hb);
        }
        else
        {
            RECT frameRect = ExcludeCaptionButtons(rc, _bExtendIntoFrame, captionButtonReserve);
            IntPtr hb = WindowNativeMethods.CreateSolidBrush(ColorTranslator.ToWin32(form.BackColor));
            WindowNativeMethods.FillRect(hdc, ref frameRect, hb);
            WindowNativeMethods.DeleteObject(hb);
        }
        if (_bExtendIntoFrame && _bDrawCaption)
        {
            Rectangle captionBounds = new Rectangle(4, 4, rc.Right, captionHeight);
            using (Graphics g = Graphics.FromHdc(hdc))
            {
                using (Font fc = new Font("Segoe UI", 12, FontStyle.Regular))
                {
                    SizeF sz = g.MeasureString(form.Text, fc);
                    int offset = (rc.Right - (int)sz.Width) / 2;
                    if (offset < 2 * FrameWidth)
                        offset = 2 * FrameWidth;
                    captionBounds.X = offset;
                    captionBounds.Y = 4;
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Near;
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            path.AddString(form.Text, fc.FontFamily, (int)fc.Style, fc.Size, captionBounds, sf);
                            using var captionBrush = new SolidBrush(form.ForeColor);
                            g.FillPath(captionBrush, path);
                        }
                    }
                }
            }
        }
    }

    #endregion
}

