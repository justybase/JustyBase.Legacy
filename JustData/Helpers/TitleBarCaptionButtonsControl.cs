using AppBase.Common;
using AppBase.Common.WindowManagement;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

internal enum CaptionButtonKind
{
    Minimize,
    Maximize,
    Close,
}

/// <summary>
/// Client-area Min / Max / Close for DWM-extended title bars (no system NC buttons).
/// </summary>
internal sealed class TitleBarCaptionButtonsControl : Control
{
    private static readonly Color CloseHoverBack = Color.FromArgb(232, 17, 35);
    private static readonly Color CloseHoverFore = Color.White;

    private readonly Form _owner;
    private CaptionButtonKind? _hovered;
    private bool _maximizePressed;
    private Color _back;
    private Color _fore;
    private Color _hoverBack;

    public TitleBarCaptionButtonsControl(Form owner)
    {
        _owner = owner;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        TabStop = false;
        Cursor = Cursors.Arrow;
        _owner.Resize += (_, _) => Invalidate();
    }

    public bool TryHitTestScreenPoint(Point screenPoint, out HIT_CONSTANTS hit)
    {
        hit = HIT_CONSTANTS.HTCLIENT;
        if (!IsHandleCreated || !Visible || !SnapLayoutHelper.IsWindows11OrGreater)
        {
            return false;
        }

        if (!GetMaximizeButtonScreenRectangle().Contains(screenPoint))
        {
            return false;
        }

        hit = HIT_CONSTANTS.HTMAXBUTTON;
        return true;
    }

    public static int GetPreferredWidth(int dpi) => DpiScale.Scale(46, dpi) * 3;

    public static int GetPreferredHeight(int dpi) => DpiScale.Scale(32, dpi);

    public void ApplyTheme(Color back, Color fore, Color hoverBack)
    {
        _back = back;
        _fore = fore;
        _hoverBack = hoverBack;
        BackColor = back;
        Invalidate();
    }

    public void UpdateHoverFromHit(HIT_CONSTANTS hit)
    {
        SetHovered(hit == HIT_CONSTANTS.HTMAXBUTTON ? CaptionButtonKind.Maximize : null);
    }

    public void ClearMaximizeHover()
    {
        if (_hovered == CaptionButtonKind.Maximize && !_maximizePressed)
        {
            _hovered = null;
            Invalidate();
        }
    }

    public void ClearHover()
    {
        if (_hovered != null || _maximizePressed)
        {
            _hovered = null;
            _maximizePressed = false;
            Invalidate();
        }
    }

    public void SetMaximizePressed(bool pressed)
    {
        if (_maximizePressed == pressed)
        {
            return;
        }

        _maximizePressed = pressed;
        if (pressed)
        {
            _hovered = CaptionButtonKind.Maximize;
        }

        Invalidate();
    }

    public void PerformClick(HIT_CONSTANTS hit)
    {
        switch (hit)
        {
            case HIT_CONSTANTS.HTMINBUTTON:
                _owner.WindowState = FormWindowState.Minimized;
                break;
            case HIT_CONSTANTS.HTMAXBUTTON:
                _owner.WindowState = _owner.WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                Invalidate();
                break;
            case HIT_CONSTANTS.HTCLOSE:
                _owner.Close();
                break;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WindowConstants.WM_NCHITTEST && SnapLayoutHelper.IsWindows11OrGreater)
        {
            Point screen = SnapLayoutHelper.GetScreenPointFromLParam(m.LParam);
            if (GetMaximizeButtonScreenRectangle().Contains(screen))
            {
                SetHovered(CaptionButtonKind.Maximize);
                m.Result = (IntPtr)HIT_CONSTANTS.HTMAXBUTTON;
                return;
            }

            ClearMaximizeHover();
            m.Result = (IntPtr)HIT_CONSTANTS.HTCLIENT;
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var brush = new SolidBrush(_back.A == 0 ? _owner.BackColor : _back);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int btnW = Math.Max(1, Width / 3);
        DrawButton(e.Graphics, new Rectangle(0, 0, btnW, Height), CaptionButtonKind.Minimize);
        DrawButton(e.Graphics, new Rectangle(btnW, 0, btnW, Height), CaptionButtonKind.Maximize);
        DrawButton(e.Graphics, new Rectangle(btnW * 2, 0, Width - btnW * 2, Height), CaptionButtonKind.Close);
    }

    private void DrawButton(Graphics g, Rectangle bounds, CaptionButtonKind kind)
    {
        bool active = _hovered == kind || (kind == CaptionButtonKind.Maximize && _maximizePressed);
        bool isClose = kind == CaptionButtonKind.Close;

        if (active)
        {
            Color fill = isClose ? CloseHoverBack : _hoverBack;
            using var brush = new SolidBrush(fill);
            g.FillRectangle(brush, bounds);
        }

        Color ink = active && isClose ? CloseHoverFore : _fore;

        float penWidth = Math.Max(1f, DpiScale.Scale(1f, DeviceDpi));
        using var pen = new Pen(ink, penWidth);
        int cx = bounds.Left + bounds.Width / 2;
        int cy = bounds.Top + bounds.Height / 2;
        int half = DpiScale.Scale(5, DeviceDpi);

        switch (kind)
        {
            case CaptionButtonKind.Minimize:
                g.DrawLine(pen, cx - half, cy + half / 2, cx + half, cy + half / 2);
                break;
            case CaptionButtonKind.Maximize:
                if (_owner.WindowState == FormWindowState.Maximized)
                {
                    int offset = DpiScale.Scale(4, DeviceDpi);
                    g.DrawRectangle(pen, cx - half + offset, cy - half, half, half);
                    g.DrawRectangle(pen, cx - half, cy - half + offset, half, half);
                }
                else
                {
                    g.DrawRectangle(pen, cx - half, cy - half, half * 2, half * 2);
                }

                break;
            case CaptionButtonKind.Close:
                g.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                g.DrawLine(pen, cx + half, cy - half, cx - half, cy + half);
                break;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        CaptionButtonKind? kind = HitTestButton(e.Location);
        SetHovered(kind);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hovered != null)
        {
            _hovered = null;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        CaptionButtonKind? kind = HitTestButton(e.Location);
        if (kind == null)
        {
            return;
        }

        if (kind == CaptionButtonKind.Maximize && SnapLayoutHelper.IsWindows11OrGreater)
        {
            return;
        }

        HIT_CONSTANTS hit = kind switch
        {
            CaptionButtonKind.Minimize => HIT_CONSTANTS.HTMINBUTTON,
            CaptionButtonKind.Maximize => HIT_CONSTANTS.HTMAXBUTTON,
            CaptionButtonKind.Close => HIT_CONSTANTS.HTCLOSE,
            _ => HIT_CONSTANTS.HTCLIENT,
        };
        PerformClick(hit);
    }

    private Rectangle GetMaximizeButtonClientBounds()
    {
        int btnW = Math.Max(1, Width / 3);
        return new Rectangle(btnW, 0, btnW, Height);
    }

    private Rectangle GetMaximizeButtonScreenRectangle()
    {
        return RectangleToScreen(GetMaximizeButtonClientBounds());
    }

    private CaptionButtonKind? HitTestButton(Point p)
    {
        if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height)
        {
            return null;
        }

        int btnW = Math.Max(1, Width / 3);
        int index = p.X / btnW;
        return index switch
        {
            0 => CaptionButtonKind.Minimize,
            1 => CaptionButtonKind.Maximize,
            _ => CaptionButtonKind.Close,
        };
    }

    private void SetHovered(CaptionButtonKind? kind)
    {
        if (_hovered != kind)
        {
            _hovered = kind;
            Invalidate();
        }
    }
}
