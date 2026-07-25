using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public class TabControlDrawingHandler
{
    private readonly IColorTheme _colorTheme;
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private readonly Font _baseFont;

    private readonly Image _normalXimage;
    private readonly Image _hoverXimage;
    private readonly Image _normalPinImage;
    private readonly Image _normalPinImageSelected;
    private readonly Image _activePinImage;
    private readonly Image _activePinImageSelected;

    private readonly StringFormat _stringFormatLeftTabs = new StringFormat
    {
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.DirectionVertical | StringFormatFlags.NoWrap,
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private static StringFormat _tabTitleFormat;

    private static StringFormat TabTitleFormat => _tabTitleFormat ??= new StringFormat
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private static Rectangle TabTitleRect(Rectangle tabRect, int dpi, bool reservePinIcon)
    {
        int verticalOffset = TabDrawOffset(dpi);
        int horizontalOffset = DpiScale.Scale(7, dpi);
        int rightLimit = reservePinIcon
            ? TabIconLayout.PinIconRect(tabRect, dpi).X
            : TabIconLayout.CloseIconRect(tabRect, dpi).X;

        return new Rectangle(
            tabRect.X + horizontalOffset,
            tabRect.Y + verticalOffset,
            Math.Max(0, rightLimit - tabRect.X - horizontalOffset - DpiScale.Scale(4, dpi)),
            Math.Max(0, tabRect.Height - verticalOffset * 2));
    }

    private static int TabDrawOffset(int dpi) => DpiScale.Scale(2, dpi);

    private static GraphicsPath CreateRoundedTabPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void DrawModernTabSurface(
        Graphics graphics,
        Rectangle tabRect,
        int dpi,
        bool selected,
        Brush surfaceBrush,
        bool vertical = false)
    {
        bool darkTheme = _colorTheme.IsDark(_colorTheme.MainBack);
        Color borderColor = selected
            ? Color.FromArgb(86, 156, 214)
            : darkTheme ? Color.FromArgb(60, 66, 78) : Color.FromArgb(211, 218, 227);
        using var borderPen = new Pen(borderColor, Math.Max(1f, DpiScale.Factor(dpi)));

        graphics.FillRectangle(surfaceBrush, tabRect);
        if (vertical)
        {
            graphics.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
            if (selected)
            {
                graphics.DrawLine(borderPen, tabRect.Right - 1, tabRect.Top + DpiScale.Scale(2, dpi), tabRect.Right - 1, tabRect.Bottom - DpiScale.Scale(2, dpi));
            }
        }
        else
        {
            graphics.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
            graphics.DrawLine(borderPen, tabRect.Right - 1, tabRect.Top, tabRect.Right - 1, tabRect.Bottom);
        }
    }

    private static void DrawModernCloseButton(Graphics graphics, Rectangle iconBounds, int dpi, bool darkTheme, bool hovered)
    {
        Color buttonBack = hovered
            ? Color.FromArgb(198, 76, 82)
            : darkTheme ? Color.FromArgb(78, 88, 103) : Color.FromArgb(232, 236, 242);
        Color buttonBorder = hovered
            ? Color.FromArgb(224, 113, 117)
            : darkTheme ? Color.FromArgb(123, 135, 153) : Color.FromArgb(198, 206, 218);
        Color signColor = hovered
            ? Color.White
            : darkTheme ? Color.FromArgb(225, 231, 239) : Color.FromArgb(82, 91, 104);

        Rectangle buttonBounds = iconBounds;
        int radius = DpiScale.Scale(5, dpi);
        using var buttonPath = CreateRoundedTabPath(buttonBounds, radius);
        using var buttonBrush = new SolidBrush(buttonBack);
        using var buttonPen = new Pen(buttonBorder, Math.Max(1f, DpiScale.Factor(dpi)));
        graphics.FillPath(buttonBrush, buttonPath);
        graphics.DrawPath(buttonPen, buttonPath);

        int centerX = buttonBounds.Left + buttonBounds.Width / 2;
        int centerY = buttonBounds.Top + buttonBounds.Height / 2;
        int arm = DpiScale.Scale(4, dpi);
        using var signPen = new Pen(signColor, Math.Max(1.25f, DpiScale.Factor(dpi)));
        signPen.StartCap = LineCap.Round;
        signPen.EndCap = LineCap.Round;
        graphics.DrawLine(signPen, centerX - arm, centerY - arm, centerX + arm, centerY + arm);
        graphics.DrawLine(signPen, centerX + arm, centerY - arm, centerX - arm, centerY + arm);
    }

    private static void DrawModernPinButton(Graphics graphics, Rectangle iconBounds, int dpi, bool darkTheme, bool pinned, bool hovered)
    {
        Color buttonBack = pinned || hovered
            ? darkTheme ? Color.FromArgb(55, 94, 132) : Color.FromArgb(224, 238, 250)
            : darkTheme ? Color.FromArgb(78, 88, 103) : Color.FromArgb(232, 236, 242);
        Color buttonBorder = pinned || hovered
            ? Color.FromArgb(86, 156, 214)
            : darkTheme ? Color.FromArgb(123, 135, 153) : Color.FromArgb(198, 206, 218);
        Color pinColor = pinned || hovered
            ? Color.FromArgb(86, 156, 214)
            : darkTheme ? Color.FromArgb(225, 231, 239) : Color.FromArgb(82, 91, 104);

        Rectangle buttonBounds = iconBounds;
        using var buttonPath = CreateRoundedTabPath(buttonBounds, DpiScale.Scale(5, dpi));
        using var buttonBrush = new SolidBrush(buttonBack);
        using var buttonPen = new Pen(buttonBorder, Math.Max(1f, DpiScale.Factor(dpi)));
        graphics.FillPath(buttonBrush, buttonPath);
        graphics.DrawPath(buttonPen, buttonPath);

        int centerX = buttonBounds.Left + buttonBounds.Width / 2;
        int centerY = buttonBounds.Top + buttonBounds.Height / 2;
        int headSize = DpiScale.Scale(5, dpi);
        int bodyHeight = DpiScale.Scale(5, dpi);
        int stemHeight = DpiScale.Scale(3, dpi);
        using var pinBrush = new SolidBrush(pinColor);
        using var pinPen = new Pen(pinColor, Math.Max(1f, DpiScale.Factor(dpi)));
        graphics.FillEllipse(
            pinBrush,
            centerX - headSize / 2,
            centerY - DpiScale.Scale(6, dpi),
            headSize,
            headSize);
        graphics.DrawLine(pinPen,
            centerX,
            centerY - DpiScale.Scale(3, dpi),
            centerX,
            centerY + stemHeight);
        graphics.DrawLine(pinPen,
            centerX - DpiScale.Scale(5, dpi),
            centerY + bodyHeight / 2,
            centerX + DpiScale.Scale(5, dpi),
            centerY + bodyHeight / 2);
        graphics.DrawLine(pinPen,
            centerX,
            centerY + stemHeight,
            centerX,
            centerY + DpiScale.Scale(6, dpi));
    }

    private Font GetTabFont(TabControl tabControl) => tabControl?.Font ?? _baseFont;

    public TabControlDrawingHandler(IColorTheme colorTheme, IApplicationSettingsContext applicationSettingsContext, Font baseFont
        , Image close, Image closeJasne, Image gray_pin, Image gray_pin_selected, Image Black_pin, Image Black_pin_selected)
    {
        _colorTheme = colorTheme ?? throw new ArgumentNullException(nameof(colorTheme));
        _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
        _baseFont = baseFont ?? throw new ArgumentNullException(nameof(_baseFont));

        _normalXimage = close;
        _hoverXimage = closeJasne;
        _normalPinImage = gray_pin;
        _normalPinImageSelected = gray_pin_selected;
        _activePinImage = Black_pin;
        _activePinImageSelected = Black_pin_selected;
    }

    public void TabControlMain_DrawItem(object sender, DrawItemEventArgs e)
    {
        var tabControl = sender as TabControl;
        int dpi = tabControl.DeviceDpi;
        TabPagePicture actualTab = tabControl.TabPages[e.Index] as TabPagePicture;
        bool closeHovered = actualTab.CloseImage == _hoverXimage;
        bool darkTheme = _colorTheme.IsDark(_colorTheme.MainBack);

        if (actualTab == tabControl.SelectedTab)
        {
            actualTab.FinishedInBackground = false;
        }
        bool finishedInBackground = actualTab.FinishedInBackground;
        Rectangle tabRect = tabControl.GetTabRect(e.Index);

        BufferedGraphicsContext currentContext;
        BufferedGraphics myBuffer;
        currentContext = BufferedGraphicsManager.Current;
        myBuffer = currentContext.Allocate(e.Graphics, tabRect);

        DrawModernTabSurface(
            myBuffer.Graphics,
            tabRect,
            dpi,
            e.Index == tabControl.SelectedIndex,
            e.Index == tabControl.SelectedIndex ? _colorTheme.SelectedTabBrush : _colorTheme.NonSelectedTabBrush);

        if (_applicationSettingsContext.Config.UseSpecialColoring)
        {
            var gg = e.Graphics;
            gg.SmoothingMode = SmoothingMode.AntiAlias;
            Region rgn = new Region(tabControl.ClientRectangle);
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var rec1 = tabControl.GetTabRect(i);
                rgn.Exclude(rec1);
            }
            gg.FillRegion(_colorTheme.NonSelectedTabBrush, rgn);
        }

        Rectangle textRect = TabTitleRect(tabRect, dpi, reservePinIcon: false);

        Brush titleBrush = finishedInBackground ? _colorTheme.TitleBrushBackground : _colorTheme.TitleBrush;
        Font tabFont = GetTabFont(tabControl);
        if (finishedInBackground)
        {
            using var titleFont = new Font(tabFont, FontStyle.Bold);
            myBuffer.Graphics.DrawString(tabControl.TabPages[e.Index].Text, titleFont, titleBrush, textRect, TabTitleFormat);
        }
        else
        {
            myBuffer.Graphics.DrawString(tabControl.TabPages[e.Index].Text, tabFont, titleBrush, textRect, TabTitleFormat);
        }

        DrawModernCloseButton(
            myBuffer.Graphics,
            TabIconLayout.CloseIconRect(tabRect, dpi),
            dpi,
            darkTheme,
            closeHovered);
        myBuffer.Render();
        myBuffer.Dispose();
    }

    public void TabControlResults_DrawItem(object sender, DrawItemEventArgs e)
    {
        TabControl tcCurrent = sender as TabControl;
        if (tcCurrent is null) return;
        int dpi = tcCurrent.DeviceDpi;
        TabPagePicture tabPage = tcCurrent.TabPages[e.Index] as TabPagePicture;
        Image closeImg = tabPage?.CloseImage;
        Image pinImg = tabPage?.PinImage;
        bool isRunning = tabPage?.IsRunning == true;
        bool isSuccess = tabPage?.IsSuccess == true;
        string tabText = tcCurrent.TabPages[e.Index].Text ?? string.Empty;
        bool isLog = tabText.StartsWith("Log", StringComparison.OrdinalIgnoreCase);
        bool isPermanentDiagnostics = (tcCurrent.TabPages[e.Index].Tag as TabPageResultsTag)?.IsPermanentDiagnostics == true;
        bool isPinned = (tcCurrent.TabPages[e.Index].Tag as TabPageResultsTag)?.Docked == true;
        bool closeHovered = closeImg == _hoverXimage;
        bool pinHovered = pinImg == _activePinImageSelected || pinImg == _normalPinImageSelected;
        bool darkTheme = _colorTheme.IsDark(_colorTheme.MainBack);

        Rectangle tabRect = tcCurrent.GetTabRect(e.Index);

        BufferedGraphicsContext currentContext;
        BufferedGraphics myBuffer;
        currentContext = BufferedGraphicsManager.Current;
        myBuffer = currentContext.Allocate(e.Graphics, tabRect);

        Brush tabSurfaceBrush = e.Index == tcCurrent.SelectedIndex
            ? _colorTheme.SelectedTabBrush
            : _colorTheme.NonSelectedTabBrush;

        if (!_applicationSettingsContext.Config.UseSpecialColoring && isLog)
        {
            if (isRunning)
            {
                tabSurfaceBrush = MyColors.Log1Brush;
            }
            else if (!isSuccess)
            {
                tabSurfaceBrush = MyColors.Log2Brush;
            }
            else
            {
                tabSurfaceBrush = MyColors.Log3Brush;
            }
        }
        else if (_applicationSettingsContext.Config.UseSpecialColoring && isLog)
        {
            Color logTint = isRunning
                ? Color.FromArgb(50, 50, 30)
                    : !isSuccess
                        ? Color.FromArgb(55, 25, 25)
                        : Color.FromArgb(25, 45, 25);
            tabSurfaceBrush = new SolidBrush(logTint);
        }

        DrawModernTabSurface(myBuffer.Graphics, tabRect, dpi, e.Index == tcCurrent.SelectedIndex, tabSurfaceBrush);
        if (tabSurfaceBrush is SolidBrush logBrush && _applicationSettingsContext.Config.UseSpecialColoring && isLog)
        {
            logBrush.Dispose();
        }

        if (_applicationSettingsContext.Config.UseSpecialColoring)
        {
            var gg = e.Graphics;
            gg.SmoothingMode = SmoothingMode.AntiAlias;
            Region rgn = new Region(tcCurrent.ClientRectangle);
            for (int i = 0; i < tcCurrent.TabCount; i++)
            {
                var rec1 = tcCurrent.GetTabRect(i);
                rgn.Exclude(rec1);
            }
            gg.FillRegion(_colorTheme.NonSelectedTabBrush, rgn);
        }

        Rectangle textRect = TabTitleRect(tabRect, dpi, reservePinIcon: !isPermanentDiagnostics);

        string title = tabText;
        Font tabFont = GetTabFont(tcCurrent);
        tcCurrent.TabPages[e.Index].ToolTipText = title;
        if (isRunning && isLog)
        {
            using var underlinedFont = new Font(tabFont, FontStyle.Underline);
            myBuffer.Graphics.DrawString(title, underlinedFont, _colorTheme.TitleBrush, textRect, TabTitleFormat);
        }
        else
        {
            myBuffer.Graphics.DrawString(title, tabFont, _colorTheme.TitleBrush, textRect, TabTitleFormat);
        }

        if (!isPermanentDiagnostics)
        {
            DrawModernCloseButton(
                myBuffer.Graphics,
                TabIconLayout.CloseIconRect(tabRect, dpi),
                dpi,
                darkTheme,
                closeHovered);
            DrawModernPinButton(
                myBuffer.Graphics,
                TabIconLayout.PinIconRect(tabRect, dpi),
                dpi,
                darkTheme,
                isPinned,
                pinHovered);
        }

        myBuffer.Render();
        myBuffer.Dispose();
    }

    public void LeftTabsDrawItem(object sender, DrawItemEventArgs e)
    {
        var leftTabs = sender as TabControl;
        int dpi = leftTabs.DeviceDpi;
        Font tabFont = GetTabFont(leftTabs);

        var gg = e.Graphics;
        gg.SmoothingMode = SmoothingMode.AntiAlias;
        Region rgn = new Region(leftTabs.ClientRectangle);
        for (int i = 0; i < leftTabs.TabCount; i++)
        {
            var rec1 = leftTabs.GetTabRect(i);
            rgn.Exclude(rec1);
        }
        gg.FillRegion(_colorTheme.NonSelectedTabBrush, rgn);

        var r = leftTabs.GetTabRect(e.Index);
        BufferedGraphicsContext currentContext;
        BufferedGraphics myBuffer;
        currentContext = BufferedGraphicsManager.Current;
        myBuffer = currentContext.Allocate(e.Graphics, r);

        Brush tabBrush = e.Index == leftTabs.SelectedIndex
            ? _colorTheme.SelectedTabBrush
            : _colorTheme.NonSelectedTabBrush;
        DrawModernTabSurface(myBuffer.Graphics, r, dpi, e.Index == leftTabs.SelectedIndex, tabBrush, vertical: true);
        if (leftTabs.SelectedIndex == e.Index)
        {
            using var underlinedFont = new Font(tabFont, FontStyle.Underline);
            myBuffer.Graphics.DrawString(
                leftTabs.TabPages[e.Index].Text,
                underlinedFont,
                _colorTheme.TitleBrush,
                r,
                _stringFormatLeftTabs);
        }
        else
        {
            myBuffer.Graphics.DrawString(
                leftTabs.TabPages[e.Index].Text,
                tabFont,
                _colorTheme.TitleBrush,
                r,
                _stringFormatLeftTabs);
        }
        myBuffer.Render();
        myBuffer.Dispose();
    }

    public void TabControlMain_MouseLeave(object sender, EventArgs e)
    {
        var tabControl = sender as TabControl;
        int dpi = tabControl.DeviceDpi;
        for (var i = 0; i < tabControl.TabPages.Count; i++)
        {
            Rectangle hitRect = TabIconLayout.HitRect(TabIconLayout.CloseIconRect(tabControl.GetTabRect(i), dpi), dpi);

            if ((tabControl.TabPages[i] as TabPagePicture).CloseImage != _normalXimage)
            {
                (tabControl.TabPages[i] as TabPagePicture).CloseImage = _normalXimage;
                tabControl.Invalidate(hitRect);
            }
        }
    }

    public void TabControlResults_MouseMove(object sender, MouseEventArgs e)
    {
        TabControl tcCurrent = sender as TabControl;
        if (tcCurrent is null) return;
        int dpi = tcCurrent.DeviceDpi;
        for (var i = 0; i < tcCurrent.TabPages.Count; i++)
        {
            Point p = e.Location;
            Rectangle tabRect = tcCurrent.GetTabRect(i);
            bool isPermanentDiagnostics = (tcCurrent.TabPages[i].Tag as TabPageResultsTag)?.IsPermanentDiagnostics == true;
            Rectangle closeHit = TabIconLayout.HitRect(TabIconLayout.CloseIconRect(tabRect, dpi), dpi);
            Rectangle pinHit = TabIconLayout.HitRect(TabIconLayout.PinIconRect(tabRect, dpi), dpi);

            if (tcCurrent.TabPages[i] is not TabPagePicture pic) continue;

            Image imageToDraw = !isPermanentDiagnostics && closeHit.Contains(p) ? _hoverXimage : _normalXimage;

            if (pic.CloseImage != imageToDraw)
            {
                pic.CloseImage = imageToDraw;
                tcCurrent.Invalidate(closeHit);
            }

            if (pic.Tag is TabPageResultsTag tag)
            {
                Image imageToDraw2 = pinHit.Contains(p)
                    ? tag.Docked ? _activePinImageSelected : _normalPinImageSelected
                    : tag.Docked ? _activePinImage : _normalPinImage;

                if (pic.PinImage != imageToDraw2)
                {
                    pic.PinImage = imageToDraw2;
                    tcCurrent.Invalidate(pinHit);
                }
            }
        }
    }

    public void Tc_MouseLeave(object sender, EventArgs e)
    {
        TabControl tcCurrent = sender as TabControl;
        if (tcCurrent is null) return;
        int dpi = tcCurrent.DeviceDpi;

        for (var i = 0; i < tcCurrent.TabPages.Count; i++)
        {
            Rectangle tabRect = tcCurrent.GetTabRect(i);
            Rectangle closeHit = TabIconLayout.HitRect(TabIconLayout.CloseIconRect(tabRect, dpi), dpi);
            Rectangle pinHit = TabIconLayout.HitRect(TabIconLayout.PinIconRect(tabRect, dpi), dpi);

            if (tcCurrent.TabPages[i] is not TabPagePicture pic) continue;

            if (pic.CloseImage != _normalXimage)
            {
                pic.CloseImage = _normalXimage;
                tcCurrent.Invalidate(closeHit);
            }

            if (pic.Tag is TabPageResultsTag tag)
            {
                Image imageToDraw2 = tag.Docked ? _activePinImage : _normalPinImage;

                if (pic.PinImage != imageToDraw2)
                {
                    pic.PinImage = imageToDraw2;
                    tcCurrent.Invalidate(pinHit);
                }
            }
        }
    }

    public void TabControlMain_MouseMove(object sender, MouseEventArgs e)
    {
        var tabControl = sender as TabControl;
        int dpi = tabControl.DeviceDpi;
        for (var i = 0; i < tabControl.TabPages.Count; i++)
        {
            Point p = e.Location;
            Rectangle hitRect = TabIconLayout.HitRect(TabIconLayout.CloseIconRect(tabControl.GetTabRect(i), dpi), dpi);

            Image imageToDraw = hitRect.Contains(p) ? _hoverXimage : _normalXimage;

            if ((tabControl.TabPages[i] as TabPagePicture).CloseImage != imageToDraw)
            {
                (tabControl.TabPages[i] as TabPagePicture).CloseImage = imageToDraw;
                tabControl.Invalidate(hitRect);
            }
        }
    }
}
