// BaseWindow chrome, DPI, and theme partial.
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.JsonContext;
using AppBase.Common.Models;
using AppBase.Common.WindowManagement;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Services;
using AppBase.Services.Helpers;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaDriver;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
using JustyBaseLegacy.UI.Models;
using SpreadSheetTasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        public void RePaintMainWindowX()
        {
            ApplyApplicationColorMode();
            Coloring.ThemeApplier.RePaintMainWindowX(this, _colorTheme, _applicationSettingsContext);
            ApplyMenuStripTheme();
            UpdateDwmMargins();
            SyncTitleBarFrame();
            ScheduleTitleBarRefresh();
        }

        public void RePaintMainWindowX2()
        {
            bool dark = _applicationSettingsContext.Config.UseSpecialColoring;
            FctbRePaint();
            RefreshAllDiagnosticsGrids();
            GridThemingHelper.RecreateThemedDataGridHandlesRecursive(this);
            GridThemingHelper.ApplyScrollbarThemeRecursive(this, dark);
            RefreshAutocompletePopups(dark);
            RefreshWindowChrome();
            InvalidateTabs();
            Invalidate();
        }

        private void RefreshWindowChrome()
        {
            ApplyApplicationColorMode();
            ApplyMenuStripTheme();
            UpdateDwmMargins();
            _windowManagementService.FrameChanged(this);
            SyncTitleBarFrame();
            ScheduleTitleBarRefresh();
            Invalidate(true);
        }

        private void RefreshAutocompletePopups(bool dark)
        {
            _themePresenter.RefreshAutocompletePopups(
                EditorTabPages.Select(_tabManager.GetEditor).Where(editor => editor is not null)!,
                _colorTheme,
                dark);
        }
        private bool _bDrawCaption = false;
        private bool _bPainting = false;
        private bool _bExtendIntoFrame = false;
        private int _iFrameHeight = WindowConstants.FRAME_WIDTH;
        private int _iFrameWidth = WindowConstants.FRAME_WIDTH;
        private int _iFrameOffset = 100;
        private int _iStoreHeight = 0;
        private MARGINS _tMargins = new MARGINS();
        private TitleBarCaptionButtonsControl? _titleBarCaptionButtons;


        private int FrameWidth
        {
            get { return _iFrameWidth; }
        }

        private int FrameHeight
        {
            get { return _iFrameHeight; }
        }
        private void ExtendMargins(int left, int top, int right, int bottom, bool drawcaption, bool intoframe)
        {
            // any negative value causes whole window client to extend
            if (left < 0 || top < 0 || right < 0 || bottom < 0)
            {
                _tMargins.cyTopHeight = -1;
            }
            // only caption can be extended
            else if (intoframe)
            {
                _tMargins.cxLeftWidth = 0;
                _tMargins.cyTopHeight = top;
                _tMargins.cxRightWidth = 0;
                _tMargins.cyBottomHeight = 0;
            }
            // normal extender
            else
            {
                _tMargins.cxLeftWidth = left;
                _tMargins.cyTopHeight = top;
                _tMargins.cxRightWidth = right;
                _tMargins.cyBottomHeight = bottom;
            }
            _bExtendIntoFrame = intoframe;
            _bDrawCaption = drawcaption;
        }

        private void UpdateDwmMargins()
        {
            ExtendMargins(0, DpiScale.Scale(35, DeviceDpi), 0, 0, false, true);
            if (IsHandleCreated)
            {
                WindowNativeMethods.DwmExtendFrameIntoClientArea(Handle, ref _tMargins);
                ApplyTitleBarTheme();
            }
        }

        private void RefreshTitleBarChrome()
        {
            ApplyApplicationColorMode();
            ApplyMenuStripTheme();
            UpdateDwmMargins();
            if (IsHandleCreated)
            {
                _windowManagementService.FrameChanged(this);
                SyncTitleBarFrame();
            }
        }

        private void SyncTitleBarFrame()
        {
            if (IsHandleCreated)
            {
                RefreshNonClientFrame(Handle);
            }
        }

        private void ApplyTitleBarTheme(IntPtr? hwnd = null)
        {
            IntPtr h = hwnd ?? Handle;
            if (h == IntPtr.Zero)
            {
                return;
            }

            (Color back, Color fore, _, _) = GetTitleBarColors();
            bool dark = _applicationSettingsContext.Config.UseSpecialColoring;
            int useImmersiveDarkMode = dark ? 1 : 0;
            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20,
                ref useImmersiveDarkMode,
                sizeof(int));
            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref useImmersiveDarkMode,
                sizeof(int));

            int backdropType = WindowConstants.DWMSBT_NONE;
            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropType,
                sizeof(int));

            int captionColor = ColorTranslator.ToWin32(back);
            int textColor = ColorTranslator.ToWin32(fore);
            int borderColor = captionColor;

            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_BORDER_COLOR,
                ref borderColor,
                sizeof(int));
            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_CAPTION_COLOR,
                ref captionColor,
                sizeof(int));
            WindowNativeMethods.DwmSetWindowAttribute(
                h,
                WindowConstants.DWMWA_TEXT_COLOR,
                ref textColor,
                sizeof(int));

            WindowNativeMethods.SetWindowPos(
                h,
                IntPtr.Zero,
                0, 0, 0, 0,
                WindowConstants.SWP_NOMOVE | WindowConstants.SWP_NOSIZE | WindowConstants.SWP_NOZORDER | WindowConstants.SWP_FRAMECHANGED);

            RefreshNonClientFrame(h);
        }

        private void ScheduleTitleBarRefresh()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)(() =>
                {
                    if (IsHandleCreated && !IsDisposed)
                    {
                        RefreshNonClientFrame(Handle);
                    }
                }));
            }
            catch (InvalidOperationException exception)
            {
                Trace.WriteLine($"Applying window chrome failed: {exception.GetType().Name}");
            }
        }

        private static void RefreshNonClientFrame(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            WindowNativeMethods.RedrawWindow(
                hwnd,
                IntPtr.Zero,
                IntPtr.Zero,
                WindowConstants.RDW_INVALIDATE | WindowConstants.RDW_FRAME | WindowConstants.RDW_UPDATENOW | WindowConstants.RDW_NOCHILDREN);
            WindowNativeMethods.SendMessage(hwnd, WindowConstants.WM_NCPAINT, 1, 0);
        }

        private int GetCaptionButtonReserve()
        {
            if (_titleBarCaptionButtons != null && _titleBarCaptionButtons.Width > 0)
            {
                return _titleBarCaptionButtons.Width;
            }

            return TitleBarCaptionButtonsControl.GetPreferredWidth(DeviceDpi);
        }

        private int GetTitleBarBandHeight()
        {
            int marginTop = _tMargins.cyTopHeight > 0 ? _tMargins.cyTopHeight : DpiScale.Scale(35, DeviceDpi);
            return marginTop + DpiScale.Scale(6, DeviceDpi);
        }

        private (Color Back, Color Fore, Color HoverBack, Color HoverFore) GetTitleBarColors()
        {
            if (_applicationSettingsContext.Config.UseSpecialColoring)
            {
                var cfg = _applicationSettingsContext.Config;
                Color back = Color.FromArgb(cfg.StripBack[0], cfg.StripBack[1], cfg.StripBack[2]);
                Color fore = Color.FromArgb(cfg.StripFore[0], cfg.StripFore[1], cfg.StripFore[2]);
                Color hoverBack = Color.FromArgb(
                    cfg.MenuItemSelectedGradientBegin[0],
                    cfg.MenuItemSelectedGradientBegin[1],
                    cfg.MenuItemSelectedGradientBegin[2]);
                return (back, fore, hoverBack, fore);
            }

            return (Color.White, Color.Black, Color.FromArgb(200, 200, 200), Color.Black);
        }

        private void UpdateMinimumSize()
        {
            int left = menuStrip1?.Location.X ?? DpiScale.Scale(12, DeviceDpi);
            int menuWidth = menuStrip1?.Width ?? DpiScale.Scale(446, DeviceDpi);
            MinimumSize = new Size(
                left + menuWidth + GetCaptionButtonReserve() + DpiScale.Scale(16, DeviceDpi),
                DpiScale.Scale(400, DeviceDpi));
        }

        private const float MenuStripLogicalFontSize = 9.5f;

        private void ApplyMenuStripTheme()
        {
            ApplyApplicationColorMode();
            _colorTheme.InitColors();
            (Color back, Color fore, Color hoverBack, Color hoverFore) = GetTitleBarColors();

            BackColor = back;
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Renderer = new TitleBarMenuStripRenderer(back, fore, hoverBack, hoverFore);
            menuStrip1.BackColor = Color.Transparent;
            menuStrip1.ForeColor = fore;

            foreach (var item in menuStrip1.Items.OfType<ToolStripMenuItem>())
            {
                item.BackColor = Color.Transparent;
                item.ForeColor = fore;

                if (item.DisplayStyle == ToolStripItemDisplayStyle.Image)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(item.Text) && item.Tag is string savedText)
                {
                    item.Text = savedText;
                }
                else if (!string.IsNullOrEmpty(item.Text))
                {
                    item.Tag = item.Text;
                }

                item.Image = null;
                item.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }

            menuStrip1.Invalidate(true);
            _titleBarCaptionButtons?.ApplyTheme(back, fore, hoverBack);
        }

        private int MeasureMenuStripContentWidth()
        {
            int width = menuStrip1.Padding.Horizontal;
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                width += item.GetPreferredSize(Size.Empty).Width + item.Margin.Horizontal;
            }

            return width + DpiScale.Scale(4, DeviceDpi);
        }

        private void LayoutTitleBarMenuStrip(int dpi, int designMenuPadLeft, int designMenuPadTop)
        {
            int left = DpiScale.Scale(designMenuPadLeft, dpi);
            int top = DpiScale.Scale(designMenuPadTop, dpi);
            menuStrip1.Location = new Point(left, top);

            int contentWidth = MeasureMenuStripContentWidth();
            int captionReserve = GetCaptionButtonReserve();
            int rightMargin = DpiScale.Scale(8, dpi);
            int maxRight = Math.Max(left + DpiScale.Scale(40, dpi), ClientSize.Width - captionReserve - rightMargin);
            int maxWidth = maxRight - left;
            menuStrip1.Width = Math.Min(contentWidth, maxWidth);
        }

        private void LayoutTitleBarCaptionButtons(int dpi)
        {
            if (_titleBarCaptionButtons == null)
            {
                return;
            }

            int width = TitleBarCaptionButtonsControl.GetPreferredWidth(dpi);
            int height = TitleBarCaptionButtonsControl.GetPreferredHeight(dpi);
            int top = DpiScale.Scale(2, dpi);
            _titleBarCaptionButtons.Bounds = new Rectangle(
                Math.Max(0, ClientSize.Width - width),
                top,
                width,
                height);
        }

        private void ScaleMenuStrip(int dpi)
        {
            Font menuFont = new Font(Font.FontFamily, MenuStripLogicalFontSize, FontStyle.Regular, GraphicsUnit.Point);
            int menuHeight = (int)Math.Ceiling(menuFont.GetHeight()) + DpiScale.Scale(4, dpi);
            menuStrip1.AutoSize = false;
            menuStrip1.Height = menuHeight;
            ToolStripDpiHelper.ApplyMenuStrip(menuStrip1, menuFont, dpi);
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                item.AutoSize = true;
            }
        }

        private void ScaleContextMenus(int dpi)
        {
            Font menuFont = new Font(Font.FontFamily, MenuStripLogicalFontSize, FontStyle.Regular, GraphicsUnit.Point);
            ContextMenuStrip[] menus =
            [
                databaseContextMenuStrip, tabContextMenuStrip, cmResults, cmMain,
                cmGridContextMenuStrip1, cmGridContextMenuStripRowView,
                cmAllTables, cmSynonyms, cmAllProcsNetezza, cmColumns, cmConstraints,
                cmIndexes, cmPartitions, cmTriggers, cmAllViews,
                contextMenuStripNetezzaSequences, contextMenuStripNetezzaUsersOrGroups,
                cmsDB2Server, cmsSynonyms, _emptyContextMenuStrip
            ];

            foreach (ContextMenuStrip menu in menus)
            {
                if (menu != null)
                {
                    ToolStripDpiHelper.ApplyContextMenu(menu, menuFont, dpi);
                }
            }
        }

        private void LayoutStatusPanel(int dpi)
        {
            if (panel1 == null)
            {
                return;
            }

            Font barFont = statusTextBox?.Font ?? Font;
            int barHeight = (int)Math.Ceiling(barFont.GetHeight()) + DpiScale.Scale(10, dpi);
            int controlHeight = barHeight - DpiScale.Scale(4, dpi);
            int controlTop = Math.Max(0, (barHeight - controlHeight) / 2);

            panel1.Height = barHeight;
            foreach (Control control in panel1.Controls)
            {
                control.Height = controlHeight;
                control.Top = controlTop;
            }
        }

        private void ScaleShellControls(int dpi)
        {
            LayoutStatusPanel(dpi);
            ApplySqlTabMetrics(dpi);
            _leftTabs.ItemSize = TabIconLayout.LeftTabItemSize(_leftTabs, Font, dpi);
            _leftTabs.Invalidate();
            DatabaseTreeImageListHelper.EnsurePopulated(imageList1, dpi);
            _mvvmDatabaseExplorerControl?.ApplyDpiMetrics();
            foreach (FilesControl files in tabPageFiles.Controls.OfType<FilesControl>())
            {
                files.ApplyDpiMetrics();
            }

            _variablesControl?.ApplyDpiMetrics();
            _mvvmObjectExplorerControl?.ApplyDpiMetrics();
            _gitControl?.ApplyDpiMetrics();
            _filesControl?.ApplyDpiMetrics();
        }

        private void ApplySqlTabMetrics(int dpi)
        {
            int tabHeight = TabIconLayout.TabHeight(Font, dpi);
            Point padding = TabIconLayout.TabPadding(dpi);

            if (_tabControlMain != null)
            {
                _tabControlMain.Padding = padding;
                _tabControlMain.ItemSize = new Size(0, tabHeight);
            }

            if (_tabControlMain == null)
            {
                return;
            }

            int resultsTabHeight = TabIconLayout.ResultsTabHeight(Font, dpi);
            Point resultsPadding = TabIconLayout.ResultsTabPadding(dpi);

            foreach (TabPage mainTab in EditorTabPages)
            {
                foreach (SplitContainer splitContainer in mainTab.Controls.OfType<SplitContainer>())
                {
                    foreach (TabControl resultTabs in splitContainer.Panel2.Controls.OfType<TabControl>())
                    {
                        resultTabs.Padding = resultsPadding;
                        resultTabs.ItemSize = new Size(0, resultsTabHeight);
                    }
                }
            }
        }

        private void ApplyShellLayout()
        {
            if (splitContainer1 == null || panel1 == null || menuStrip1 == null)
            {
                return;
            }

            int dpi = DeviceDpi;
            int margin = DpiScale.Scale(3, dpi);

            ScaleMenuStrip(dpi);
            ScaleContextMenus(dpi);
            ApplyMenuStripTheme();

            const int designMenuPadLeft = 12;
            const int designMenuPadTop = 10;
            LayoutTitleBarCaptionButtons(dpi);
            LayoutTitleBarMenuStrip(dpi, designMenuPadLeft, designMenuPadTop);
            _iFrameOffset = GetCaptionButtonReserve();

            ScaleShellControls(dpi);

            int titleBarHeight = Math.Max(
                DpiScale.Scale(32, dpi),
                menuStrip1.Bottom + DpiScale.Scale(4, dpi));

            int statusGap = DpiScale.Scale(1, dpi);
            // Keep the bottom resize band out of the status panel. A docked
            // child control receives the mouse hit-test first, which makes
            // the form's HTBOTTOM border unreachable across the full width.
            int resizeBand = Math.Max(FrameHeight, DpiScale.Scale(8, dpi));
            panel1.Dock = DockStyle.None;
            panel1.Bounds = new Rectangle(
                0,
                Math.Max(0, ClientSize.Height - panel1.Height - resizeBand),
                ClientSize.Width,
                panel1.Height);

            const int toggleBarHeight = 0;
            Control shellHost = splitContainer1;
            shellHost.Location = new Point(margin, titleBarHeight);
            shellHost.Size = new Size(
                Math.Max(0, ClientSize.Width - margin * 2),
                Math.Max(0, ClientSize.Height - titleBarHeight - panel1.Height - toggleBarHeight - statusGap - resizeBand));
            menuStrip1.BringToFront();
            _titleBarCaptionButtons?.BringToFront();
            UpdateMinimumSize();
            UpdateDwmMargins();
        }

        private void LayoutMainSplitter()
        {
            if (splitContainer1 == null || splitContainer1.Width <= 0)
            {
                return;
            }

            // Narrow schema panel, wide SQL area (reference layout at 96 DPI: 101px of 867px).
            const int designLeftWidth = 101;
            const int designTotalWidth = 867;

            int distance = (designLeftWidth * splitContainer1.Width) / designTotalWidth;
            int minDistance = DpiScale.Scale(designLeftWidth, DeviceDpi);
            splitContainer1.SplitterDistance = Math.Max(distance, minDistance);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplyShellLayout();
            LayoutMainSplitter();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState != FormWindowState.Minimized)
            {
                ApplyShellLayout();
            }
        }
        private void GetFrameSize()
        {
            _iFrameOffset = GetCaptionButtonReserve();
            switch (this.FormBorderStyle)
            {
                case FormBorderStyle.Sizable:
                    {
                        _iFrameHeight = WindowConstants.FRAME_WIDTH;
                        _iFrameWidth = WindowConstants.FRAME_WIDTH;
                        break;
                    }
                case FormBorderStyle.Fixed3D:
                    {
                        _iFrameHeight = 4;
                        _iFrameWidth = 4;
                        break;
                    }
                case FormBorderStyle.FixedDialog:
                    {
                        _iFrameHeight = 2;
                        _iFrameWidth = 2;
                        break;
                    }
                case FormBorderStyle.FixedSingle:
                    {
                        _iFrameHeight = 2;
                        _iFrameWidth = 2;
                        break;
                    }
                case FormBorderStyle.FixedToolWindow:
                    {
                        _iFrameOffset = 20;
                        _iFrameHeight = 2;
                        _iFrameWidth = 2;
                        break;
                    }
                case FormBorderStyle.SizableToolWindow:
                    {
                        _iFrameOffset = 20;
                        _iFrameHeight = 4;
                        _iFrameWidth = 4;
                        break;
                    }
                default:
                    {
                        _iFrameHeight = WindowConstants.FRAME_WIDTH;
                        _iFrameWidth = WindowConstants.FRAME_WIDTH;
                        break;
                    }
            }
        }
        private static Point GetScreenPointFromLParam(IntPtr lParam) =>
            SnapLayoutHelper.GetScreenPointFromLParam(lParam);

        protected void CustomProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WindowConstants.WM_SIZE:
                    {
                        base.WndProc(ref m);
                        SyncTitleBarFrame();
                        ScheduleTitleBarRefresh();
                        break;
                    }
                case WindowConstants.WM_ERASEBKGND:
                    {
                        base.WndProc(ref m);
                        break;
                    }
                case WindowConstants.WM_PAINT:
                    {
                        if (!_bPainting)
                        {
                            _bPainting = true;
                            base.WndProc(ref m);
                            SyncTitleBarFrame();
                            _bPainting = false;
                        }
                        else
                        {
                            base.WndProc(ref m);
                        }

                        break;
                    }
                case WindowConstants.WM_CREATE:
                    {
                        GetFrameSize();
                        ExtendMargins(0, DpiScale.Scale(35, DeviceDpi), 0, 0, false, true);
                        WindowNativeMethods.DwmExtendFrameIntoClientArea(m.HWnd, ref _tMargins);
                        ApplyTitleBarTheme(m.HWnd);
                        _windowManagementService.FrameChanged(this);
                        m.Result = WindowConstants.MSG_HANDLED;
                        base.WndProc(ref m);
                        break;
                    }
                case WindowConstants.WM_NCCALCSIZE:
                    if (m.WParam != IntPtr.Zero && m.Result == IntPtr.Zero)
                    {
                        if (_bExtendIntoFrame)
                        {
                            NCCALCSIZE_PARAMS nc = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                            nc.rect0.Right -= 6;
                            nc.rect1 = nc.rect0;
                            Marshal.StructureToPtr(nc, m.LParam, false);
                            m.Result = (IntPtr)WindowConstants.WVR_VALIDRECTS;
                        }
                    }
                    else base.WndProc(ref m);
                    break;
                case WindowConstants.WM_SYSCOMMAND:
                    {
                        UInt32 param;
                        if (IntPtr.Size == 4)
                            param = (UInt32)(m.WParam.ToInt32());
                        else
                            param = (UInt32)(m.WParam.ToInt64());
                        if ((param & 0xFFF0) == WindowConstants.SC_RESTORE)
                        {
                            this.Height = _iStoreHeight;
                        }
                        else if (this.WindowState == FormWindowState.Normal)
                        {
                            _iStoreHeight = this.Height;
                        }

                        base.WndProc(ref m);

                        if ((param & 0xFFF0) == WindowConstants.SC_MAXIMIZE
                            || (param & 0xFFF0) == WindowConstants.SC_RESTORE)
                        {
                            SyncTitleBarFrame();
                            ScheduleTitleBarRefresh();
                        }

                        break;
                    }
                case WindowConstants.WM_NCHITTEST:
                    {
                        Point screen = GetScreenPointFromLParam(m.LParam);
                        if (_titleBarCaptionButtons?.TryHitTestScreenPoint(screen, out HIT_CONSTANTS captionHit) == true)
                        {
                            _titleBarCaptionButtons.UpdateHoverFromHit(captionHit);
                            m.Result = (IntPtr)captionHit;
                            break;
                        }

                        _titleBarCaptionButtons?.ClearMaximizeHover();

                        // Resolve our resize borders before asking DWM. The
                        // frame is extended into the client area, so DWM can
                        // otherwise classify the bottom resize band as
                        // client content and prevent vertical resizing.
                        HIT_CONSTANTS appHit = _windowManagementService.HitTest(
                            this, FrameWidth, FrameHeight, _iFrameOffset, ref _tMargins);
                        if (appHit != HIT_CONSTANTS.HTCLIENT)
                        {
                            m.Result = (IntPtr)appHit;
                            break;
                        }

                        IntPtr res = IntPtr.Zero;
                        if (WindowNativeMethods.DwmDefWindowProc(m.HWnd, (uint)m.Msg, m.WParam, m.LParam, ref res))
                        {
                            m.Result = res;
                        }
                        else
                        {
                            m.Result = (IntPtr)appHit;
                        }

                        break;
                    }
                case WindowConstants.WM_NCLBUTTONDOWN:
                    {
                        var hit = (HIT_CONSTANTS)m.WParam.ToInt32();
                        if (hit == HIT_CONSTANTS.HTMAXBUTTON)
                        {
                            _titleBarCaptionButtons?.SetMaximizePressed(true);
                            // Release mouse capture so WM_NCLBUTTONUP can arrive.
                            WindowNativeMethods.ReleaseCapture();
                            m.Result = IntPtr.Zero;
                            break;
                        }

                        base.WndProc(ref m);
                        break;
                    }
                case WindowConstants.WM_NCLBUTTONUP:
                    {
                        var hit = (HIT_CONSTANTS)m.WParam.ToInt32();
                        if (hit == HIT_CONSTANTS.HTMAXBUTTON)
                        {
                            _titleBarCaptionButtons?.SetMaximizePressed(false);
                            _titleBarCaptionButtons?.PerformClick(HIT_CONSTANTS.HTMAXBUTTON);
                            m.Result = IntPtr.Zero;
                            break;
                        }

                        base.WndProc(ref m);
                        break;
                    }
                case WindowConstants.WM_NCMOUSEMOVE:
                    {
                        var hit = (HIT_CONSTANTS)m.WParam.ToInt32();
                        if (hit == HIT_CONSTANTS.HTMAXBUTTON)
                        {
                            _titleBarCaptionButtons?.UpdateHoverFromHit(hit);
                        }
                        else
                        {
                            _titleBarCaptionButtons?.ClearMaximizeHover();
                        }

                        m.Result = IntPtr.Zero;
                        break;
                    }
                case WindowConstants.WM_NCMOUSELEAVE:
                    {
                        IntPtr dwmResult = IntPtr.Zero;
                        WindowNativeMethods.DwmDefWindowProc(m.HWnd, (uint)m.Msg, m.WParam, m.LParam, ref dwmResult);
                        base.WndProc(ref m);
                        _titleBarCaptionButtons?.ClearHover();
                        m.Result = IntPtr.Zero;
                        break;
                    }
                case WindowConstants.WM_NCPAINT:
                    {
                        base.WndProc(ref m);
                        _titleBarCaptionButtons?.Invalidate();
                        break;
                    }
                case WindowConstants.WM_DWMCOMPOSITIONCHANGED:
                case WindowConstants.WM_ACTIVATE:
                    {
                        WindowNativeMethods.DwmExtendFrameIntoClientArea(this.Handle, ref _tMargins);
                        ApplyTitleBarTheme();
                        SyncTitleBarFrame();
                        m.Result = WindowConstants.MSG_HANDLED;
                        base.WndProc(ref m);
                        break;
                    }
                default:
                    {
                        base.WndProc(ref m);
                        break;
                    }
            }
        }
        public void FctbRePaint()
        {
            if (_applicationSettingsContext.Config.UseSpecialColoring || true)
            {
                foreach (TabPage tab in EditorTabPages)
                {
                    try
                    {
                        var fastColoredTextBox = _tabManager.GetEditor(tab);
                        if (fastColoredTextBox is not null)
                        {
                            fastColoredTextBox.ClearStylesBuffer();
                            fastColoredTextBox.Range.ClearStyle((Style[])fastColoredTextBox.Styles.Clone());
                            fastColoredTextBox.Range.ClearFoldingMarkers();
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Applying toolbar theme failed: {exception.GetType().Name}");
                    }
                }

                _colorTheme.SetStylesForFastColoring();
                foreach (TabPage tab in EditorTabPages)
                {
                    try
                    {
                        var fastColoredTextBox = _tabManager.GetEditor(tab);
                        if (fastColoredTextBox is not null)
                        {
                            MiscellaneousHelper.UpdateAdditionStyles(fastColoredTextBox.Range, _colorTheme.CurrentFctbColors, _applicationSettingsContext.Config.BracketFolding);
                            GetTextCommentRanges(fastColoredTextBox);
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Applying toolbar theme failed: {exception.GetType().Name}");
                    }
                }
            }
        }

        public void InvalidateTabs()
        {
            // _tabControlMain is hidden in DockSuite mode — invalidate the form instead.
            this.Invalidate();
            _leftTabs.Invalidate();
        }
    }
}
