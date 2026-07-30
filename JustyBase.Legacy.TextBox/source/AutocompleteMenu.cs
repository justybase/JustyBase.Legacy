using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Popup menu for autocomplete
    /// </summary>
    [Browsable(false)]
    public class AutocompleteMenu : ToolStripDropDown, IDisposable
    {
        readonly AutocompleteListView listView;
        public ToolStripControlHost host;
        public Range Fragment { get; internal set; }

        /// <summary>
        /// Regex pattern for serach fragment around caret
        /// </summary>
        public string SearchPattern { get; set; }
        /// <summary>
        /// Minimum fragment length for popup
        /// </summary>
        public int MinFragmentLength { get; set; }
        /// <summary>
        /// User selects item
        /// </summary>
        public event EventHandler<SelectingEventArgs> Selecting;
        /// <summary>
        /// It fires after item inserting
        /// </summary>
        public event EventHandler<SelectedEventArgs> Selected;
        /// <summary>
        /// Occurs when popup menu is opening
        /// </summary>
        public new event EventHandler<CancelEventArgs> Opening;
        /// <summary>
        /// Allow TAB for select menu item
        /// </summary>
        public bool AllowTabKey { get { return listView.AllowTabKey; } set { listView.AllowTabKey = value; } }
        /// <summary>
        /// Interval of menu appear (ms)
        /// </summary>
        public int AppearInterval { get { return listView.AppearInterval; } set { listView.AppearInterval = value; } }
        /// <summary>
        /// Sets the max tooltip window size
        /// </summary>
        public Size MaxTooltipSize { get { return listView.MaxToolTipSize; } set { listView.MaxToolTipSize = value; } }
        /// <summary>
        /// Tooltip will perm show and duration will be ignored
        /// </summary>
        public bool AlwaysShowTooltip { get { return listView.AlwaysShowTooltip; } set { listView.AlwaysShowTooltip = value; } }

        /// <summary>
        /// Back color of selected item
        /// </summary>
        [DefaultValue(typeof(Color), "Orange")]
        public Color SelectedColor
        {
            get { return listView.SelectedColor; }
            set { listView.SelectedColor = value; }
        }

        /// <summary>
        /// Border color of hovered item
        /// </summary>
        [DefaultValue(typeof(Color), "Red")]
        public Color HoveredColor
        {
            get { return listView.HoveredColor; }
            set { listView.HoveredColor = value; }
        }

        public AutocompleteMenu(FastColoredTextBox tb)
        {
            // create a new popup and add the list view to it 
            AutoClose = false;
            AutoSize = false;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            BackColor = Color.White;
            DropShadowEnabled = true;
            listView = new AutocompleteListView(tb);
            host = new ToolStripControlHost(listView);
            host.Margin = new Padding(1);
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.AutoToolTip = false;
            CalcSize();
            base.Items.Add(host);
            listView.Parent = this;
            SearchPattern = @"[\w\.]";
            MinFragmentLength = 2;

        }

        public new Font Font
        {
            get { return listView.Font; }
            set { listView.Font = value; }
        }

        public Control ListViewHost => listView;

        public void ApplyAppearance(Color backColor, Color foreColor, Color selectedColor)
        {
            BackColor = backColor;
            ForeColor = foreColor;
            SelectedColor = selectedColor;
            listView.BackColor = backColor;
            listView.ForeColor = foreColor;
            if (host != null)
            {
                host.BackColor = backColor;
            }
            listView.Invalidate(true);
            Invalidate(true);
        }

        new internal void OnOpening(CancelEventArgs args)
        {
            if (Opening != null)
                Opening(this, args);
        }

        public new void Close()
        {
            listView.toolTip.Hide(listView);
            base.Close();
        }

        internal void CalcSize()
        {
            host.Size = listView.Size;
            Size = new System.Drawing.Size(listView.Size.Width + 2, listView.Size.Height + 2);
        }

        public virtual void OnSelecting()
        {
            listView.OnSelecting();
        }

        public void SelectNext(int shift)
        {
            listView.SelectNext(shift);
        }

        internal void OnSelecting(SelectingEventArgs args)
        {
            if (Selecting != null)
                Selecting(this, args);
        }

        public void OnSelected(SelectedEventArgs args)
        {
            if (Selected != null)
                Selected(this, args);
        }

        /// <summary>
        /// Set by <see cref="AutocompleteListView.DoAutocomplete"/> — large-script engine runs only when true.
        /// </summary>
        public bool LastAutocompleteForced { get; internal set; }

        public new AutocompleteListView Items
        {
            get { return listView; }
        }

        /// <summary>
        /// Shows popup menu immediately
        /// </summary>
        /// <param name="forced">If True - MinFragmentLength will be ignored</param>
        public void Show(bool forced)
        {
            Items.DoAutocomplete(forced);
        }

        /// <summary>
        /// Minimal size of menu
        /// </summary>
        public new Size MinimumSize
        {
            get { return Items.MinimumSize; }
            set { Items.MinimumSize = value; }
        }

        /// <summary>
        /// Image list of menu
        /// </summary>
        public new ImageList ImageList
        {
            get { return Items.ImageList; }
            set { Items.ImageList = value; }
        }

        /// <summary>
        /// Tooltip duration (ms)
        /// </summary>
        public int ToolTipDuration
        {
            get { return Items.ToolTipDuration; }
            set { Items.ToolTipDuration = value; }
        }

        /// <summary>
        /// Tooltip
        /// </summary>
        public ToolTip ToolTip
        {
            get { return Items.toolTip; }
            set { Items.toolTip = value; }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (listView != null && !listView.IsDisposed)
                listView.Dispose();
        }
    }

    [System.ComponentModel.ToolboxItem(false)]
    public class AutocompleteListView : UserControl, IDisposable
    {
        public event EventHandler FocussedItemIndexChanged;

        internal List<AutocompleteItem> visibleItems;
        IEnumerable<AutocompleteItem> sourceItems = new List<AutocompleteItem>();
        int focussedItemIndex = 0;
        int hoveredItemIndex = -1;

        private int ItemHeight
        {
            get
            {
                int textHeight = Font.Height + DpiScale(6);
                int imageHeight = ImageList?.ImageSize.Height + DpiScale(6) ?? 0;
                return Math.Max(DpiScale(20), Math.Max(textHeight, imageHeight));
            }
        }

        private int DpiScale(int logicalPixels)
        {
            return Math.Max(1, (int)Math.Round(logicalPixels * DeviceDpi / 96f));
        }

        AutocompleteMenu Menu { get { return Parent as AutocompleteMenu; } }
        int oldItemCount = 0;
        readonly FastColoredTextBox tb;
        internal ToolTip toolTip = new ToolTip();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        internal bool AllowTabKey { get; set; }
        public ImageList ImageList { get; set; }
        internal int AppearInterval { get { return timer.Interval; } set { timer.Interval = value; } }
        internal int ToolTipDuration { get; set; }
        internal Size MaxToolTipSize { get; set; }
        internal bool AlwaysShowTooltip
        {
            get { return toolTip.ShowAlways; }
            set { toolTip.ShowAlways = value; }
        }

        public Color SelectedColor { get; set; }
        public Color HoveredColor { get; set; }
        public int FocussedItemIndex
        {
            get { return focussedItemIndex; }
            set
            {
                if (focussedItemIndex != value)
                {
                    focussedItemIndex = value;
                    if (FocussedItemIndexChanged != null)
                        FocussedItemIndexChanged(this, EventArgs.Empty);
                }
            }
        }

        public AutocompleteItem FocussedItem
        {
            get
            {
                if (FocussedItemIndex >= 0 && focussedItemIndex < visibleItems.Count)
                    return visibleItems[focussedItemIndex];
                return null;
            }
            set
            {
                FocussedItemIndex = visibleItems.IndexOf(value);
            }
        }

        internal AutocompleteListView(FastColoredTextBox tb)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            base.Font = new Font(FontFamily.GenericSansSerif, 9);
            AutoScroll = true;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            BorderStyle = BorderStyle.None;
            visibleItems = new List<AutocompleteItem>();
            VerticalScroll.SmallChange = ItemHeight;
            MaximumSize = new Size(Size.Width, DpiScale(180));
            toolTip.ShowAlways = false;
            AppearInterval = 500;
            timer.Tick += new EventHandler(timer_Tick);
            SelectedColor = Color.FromArgb(86, 156, 214);
            HoveredColor = Color.FromArgb(205, 214, 226);
            ToolTipDuration = 3000;
            toolTip.Popup += ToolTip_Popup;

            this.tb = tb;

            tb.KeyDown += new KeyEventHandler(tb_KeyDown);
            tb.SelectionChanged += new EventHandler(tb_SelectionChanged);
            tb.KeyPressed += new KeyPressEventHandler(tb_KeyPressed);

            Form form = tb.FindForm();
            if (form != null)
            {
                form.LocationChanged += delegate { SafetyClose(); };
                form.ResizeBegin += delegate { SafetyClose(); };
                form.FormClosing += delegate { SafetyClose(); };
                form.LostFocus += delegate { SafetyClose(); };
            }

            tb.LostFocus += (o, e) =>
            {
                if (Menu != null && !Menu.IsDisposed)
                if (!Menu.Focused) 
                    SafetyClose();
            };

            tb.Scroll += delegate { SafetyClose(); };

            this.VisibleChanged += (o, e) =>
            {
                if (this.Visible)
                    DoSelectedVisible();
            };
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            VerticalScroll.SmallChange = ItemHeight;
            Invalidate();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            VerticalScroll.SmallChange = ItemHeight;
            Invalidate();
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            if (MaxToolTipSize.Height > 0 && MaxToolTipSize.Width > 0)
                e.ToolTipSize = MaxToolTipSize;
        }

        protected override void Dispose(bool disposing)
        {
            if (toolTip != null)
            {
                toolTip.Popup -= ToolTip_Popup;
                toolTip.Dispose();
            }
            if (tb != null)
            {
                tb.KeyDown -= tb_KeyDown;
                tb.KeyPressed -= tb_KeyPressed;
                tb.SelectionChanged -= tb_SelectionChanged;
            }

            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= timer_Tick;
                timer.Dispose();
            }

            base.Dispose(disposing);
        }

        void SafetyClose()
        {
            if (Menu != null && !Menu.IsDisposed)
                Menu.Close();
        }

        void tb_KeyPressed(object sender, KeyPressEventArgs e)
        {
            bool backspaceORdel = e.KeyChar == '\b' || e.KeyChar == 0xff;

            /*
            if (backspaceORdel)
                prevSelection = tb.Selection.Start;*/

            if (Menu.Visible && !backspaceORdel)
                DoAutocomplete(false);
            else
                ResetTimer(timer);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            DoAutocomplete(false);
        }

        void ResetTimer(System.Windows.Forms.Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        internal void DoAutocomplete()
        {
            DoAutocomplete(false);
        }

        internal void DoAutocomplete(bool forced)
        {
            Menu.LastAutocompleteForced = forced;
            if (!Menu.Enabled)
            {
                Menu.Close();
                return;
            }

            visibleItems.Clear();
            FocussedItemIndex = 0;
            VerticalScroll.Value = 0;
            //some magic for update scrolls
            AutoScrollMinSize -= new Size(1, 0);
            AutoScrollMinSize += new Size(1, 0);
            //get fragment around caret
            Range fragment = tb.Selection.GetFragment(Menu.SearchPattern);
            string text = fragment.Text;
            //calc screen point for popup menu
            Point point = tb.PlaceToPoint(fragment.End);
            point.Offset(2, tb.CharHeight);
            //
            if (forced || (text.Length >= Menu.MinFragmentLength 
                && tb.Selection.IsEmpty /*pops up only if selected range is empty*/
                && (tb.Selection.Start > fragment.Start || text.Length == 0/*pops up only if caret is after first letter*/)))
            {
                Menu.Fragment = fragment;
                bool foundSelected = false;
                //build popup menu
                foreach (var item in sourceItems)
                {
                    item.Parent = Menu;
                    CompareResult res = item.Compare(text);
                    if(res != CompareResult.Hidden)
                       visibleItems.Add(item);
                    if (res == CompareResult.VisibleAndSelected && !foundSelected)
                    {
                        foundSelected = true;
                        FocussedItemIndex = visibleItems.Count - 1;
                    }
                }

                if (foundSelected)
                {
                    AdjustScroll();
                    DoSelectedVisible();
                }
            }

            //show popup menu
            if (Count > 0)
            {
                if (!Menu.Visible)
                {
                    CancelEventArgs args = new CancelEventArgs();
                    Menu.OnOpening(args);
                    if (!args.Cancel)
                        Menu.Show(tb, point);
                }
                DoSelectedVisible();
                Invalidate();
            }
            else
                Menu.Close();
        }

        void tb_SelectionChanged(object sender, EventArgs e)
        {
            /*
            FastColoredTextBox tb = sender as FastColoredTextBox;
            
            if (Math.Abs(prevSelection.iChar - tb.Selection.Start.iChar) > 1 ||
                        prevSelection.iLine != tb.Selection.Start.iLine)
                Menu.Close();
            prevSelection = tb.Selection.Start;*/
            if (Menu.Visible)
            {
                bool needClose = false;

                if (!tb.Selection.IsEmpty)
                    needClose = true;
                else
                    if (!Menu.Fragment.Contains(tb.Selection.Start))
                    {
                        if (tb.Selection.Start.iLine == Menu.Fragment.End.iLine && tb.Selection.Start.iChar == Menu.Fragment.End.iChar + 1)
                        {
                            //user press key at end of fragment
                            char c = tb.Selection.CharBeforeStart;
                            if (!Regex.IsMatch(c.ToString(), Menu.SearchPattern))//check char
                                needClose = true;
                        }
                        else
                            needClose = true;
                    }

                if (needClose)
                    Menu.Close();
            }
            
        }

        void tb_KeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as FastColoredTextBox;

            if (Menu.Visible)
                if (ProcessKey(e.KeyCode, e.Modifiers))
                    e.Handled = true;

            if (!Menu.Visible)
            {
                if (tb.HotkeysMapping.ContainsKey(e.KeyData) && tb.HotkeysMapping[e.KeyData] == FCTBAction.AutocompleteMenu)
                {
                    DoAutocomplete(true);
                    e.Handled = true;
                }
                else
                {
                    if (e.KeyCode == Keys.Escape && timer.Enabled)
                        timer.Stop();
                }
            }
        }

        void AdjustScroll()
        {
            if (oldItemCount == visibleItems.Count)
                return;

            int needHeight = ItemHeight * visibleItems.Count + 1;
            Height = Math.Min(needHeight, MaximumSize.Height);
            Menu.CalcSize();

            AutoScrollMinSize = new Size(0, needHeight);
            oldItemCount = visibleItems.Count;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            AdjustScroll();

            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var backgroundBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            }

            var itemHeight = ItemHeight;
            int startI = VerticalScroll.Value / itemHeight - 1;
            int finishI = (VerticalScroll.Value + ClientSize.Height) / itemHeight + 1;
            startI = Math.Max(startI, 0);
            finishI = Math.Min(finishI, visibleItems.Count);
            int y = 0;
            int itemInset = DpiScale(6);
            int textPadding = DpiScale(10);
            int viewportWidth = ClientSize.Width - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
            viewportWidth = Math.Max(0, viewportWidth);

            // Resolve the display text once. Method-based completion items can
            // have a qualified replacement text while exposing only the last
            // identifier in ToString(). If that display value is empty, the
            // popup must still show the item's actual text instead of leaving a
            // blank label column.
            var displayTexts = new string[visibleItems.Count];
            for (int i = 0; i < visibleItems.Count; i++)
                displayTexts[i] = GetDisplayText(visibleItems[i]);

            int detailColumnWidth = GetMaxTextWidth(item => item.DetailText);
            if (detailColumnWidth > 0)
            {
                detailColumnWidth = Math.Min(
                    DpiScale(170),
                    detailColumnWidth + DpiScale(8));
            }

            int labelColumnWidth = GetMaxTextWidth(displayTexts) + DpiScale(8);
            bool hasDescriptions = visibleItems.Any(item => !string.IsNullOrWhiteSpace(item.DescriptionText));

            using (var borderPen = new Pen(GetBorderColor(), Math.Max(1f, DeviceDpi / 96f)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, viewportWidth - 1), Math.Max(0, ClientSize.Height - 1));
            }

            for (int i = startI; i < finishI; i++)
            {
                y = i * itemHeight - VerticalScroll.Value;

                var item = visibleItems[i];
                Rectangle itemBounds = new Rectangle(
                    itemInset,
                    y + DpiScale(2),
                    Math.Max(0, viewportWidth - itemInset * 2),
                    Math.Max(0, itemHeight - DpiScale(4)));

                if (item.BackColor != Color.Transparent)
                {
                    using var brush = new SolidBrush(item.BackColor);
                    e.Graphics.FillRectangle(brush, itemBounds);
                }
                else
                {
                    using var brush = new SolidBrush(BackColor);
                    e.Graphics.FillRectangle(brush, itemBounds);
                }

                bool selected = i == FocussedItemIndex;
                bool hovered = i == hoveredItemIndex;
                if (selected || hovered)
                {
                    Color selectionBack = selected ? GetSelectedBackColor() : GetHoveredBackColor();
                    Color selectionBorder = selected ? GetSelectedBorderColor() : GetHoveredBorderColor();
                    using var selectionPath = CreateRoundedRectanglePath(itemBounds, DpiScale(4));
                    using var selectionBrush = new SolidBrush(selectionBack);
                    using var selectionPen = new Pen(selectionBorder, Math.Max(1f, DeviceDpi / 96f));
                    e.Graphics.FillPath(selectionBrush, selectionPath);
                    e.Graphics.DrawPath(selectionPen, selectionPath);

                    using var accentBrush = new SolidBrush(selected ? SelectedColor : selectionBorder);
                    e.Graphics.FillRectangle(
                        accentBrush,
                        itemBounds.Left + DpiScale(3),
                        itemBounds.Top + DpiScale(4),
                        DpiScale(2),
                        Math.Max(DpiScale(8), itemBounds.Height - DpiScale(8)));
                }

                int textLeft = itemBounds.Left + textPadding;
                if (ImageList != null && item.ImageIndex >= 0 && item.ImageIndex < ImageList.Images.Count)
                {
                    int imageTop = itemBounds.Top + Math.Max(0, (itemBounds.Height - ImageList.ImageSize.Height) / 2);
                    e.Graphics.DrawImage(ImageList.Images[item.ImageIndex], itemBounds.Left + textPadding, imageTop);
                    textLeft += ImageList.ImageSize.Width + DpiScale(6);
                }

                var layout = CalculateColumnLayout(
                    textLeft,
                    itemBounds.Right,
                    labelColumnWidth,
                    detailColumnWidth,
                    hasDescriptions);

                Rectangle labelBounds = new Rectangle(
                    textLeft,
                    itemBounds.Top,
                    layout.LabelWidth,
                    itemBounds.Height);
                Color textColor = item.ForeColor != Color.Transparent ? item.ForeColor : ForeColor;
                if (labelBounds.Width > 0 && displayTexts[i].Length > 0)
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        displayTexts[i],
                        Font,
                        labelBounds,
                        textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }

                if (!string.IsNullOrEmpty(item.DescriptionText) && layout.DescriptionWidth > 0)
                {
                    Rectangle descriptionBounds = new Rectangle(
                        layout.DescriptionLeft,
                        itemBounds.Top,
                        layout.DescriptionWidth,
                        itemBounds.Height);
                    TextRenderer.DrawText(
                        e.Graphics,
                        item.DescriptionText,
                        Font,
                        descriptionBounds,
                        ControlPaint.LightLight(textColor),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }

                if (!string.IsNullOrEmpty(item.DetailText) && detailColumnWidth > 0)
                {
                    Rectangle detailBounds = new Rectangle(
                        layout.DetailLeft,
                        itemBounds.Top,
                        layout.DetailWidth,
                        itemBounds.Height);
                    TextRenderer.DrawText(
                        e.Graphics,
                        item.DetailText,
                        Font,
                        detailBounds,
                        ControlPaint.LightLight(textColor),
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }
        }

        private ColumnLayout CalculateColumnLayout(
            int textLeft,
            int itemRight,
            int requestedLabelWidth,
            int requestedDetailWidth,
            bool hasDescriptions)
        {
            int rightPadding = DpiScale(6);
            int columnGap = DpiScale(6);
            int availableWidth = Math.Max(0, itemRight - rightPadding - textLeft);
            int minimumLabelWidth = DpiScale(64);

            int detailWidth = Math.Min(requestedDetailWidth, DpiScale(170));
            if (detailWidth > 0)
            {
                // The label is more important than the description, but both
                // the label and the type must have a usable rectangle. If the
                // popup is narrow, shrink the type column before removing it.
                int maximumDetailWidth = Math.Max(0, availableWidth - minimumLabelWidth - columnGap);
                detailWidth = Math.Min(detailWidth, maximumDetailWidth);
            }

            int labelWidth;
            if (detailWidth > 0)
            {
                int maximumLabelWidth = Math.Max(0, availableWidth - detailWidth - columnGap);
                int preferredLabelWidth = requestedLabelWidth;

                if (hasDescriptions)
                {
                    // Leave some room for descriptions when possible. The
                    // description remains optional and is never allowed to
                    // consume the label's minimum width.
                    preferredLabelWidth = Math.Min(
                        preferredLabelWidth,
                        Math.Max(minimumLabelWidth, availableWidth / 2));
                }

                labelWidth = Math.Min(
                    Math.Max(minimumLabelWidth, preferredLabelWidth),
                    maximumLabelWidth);
            }
            else
            {
                labelWidth = availableWidth;
            }

            labelWidth = Math.Max(0, labelWidth);
            int detailLeft = itemRight - rightPadding - detailWidth;
            int descriptionLeft = textLeft + labelWidth + columnGap;
            int descriptionRight = detailWidth > 0
                ? detailLeft - columnGap
                : itemRight - rightPadding;

            return new ColumnLayout(
                labelWidth,
                detailLeft,
                detailWidth,
                descriptionLeft,
                Math.Max(0, descriptionRight - descriptionLeft));
        }

        private static string GetDisplayText(AutocompleteItem item)
        {
            if (item is null)
                return string.Empty;

            string displayText = item.ToString();
            if (!string.IsNullOrWhiteSpace(displayText))
                return displayText;

            return item.Text ?? string.Empty;
        }

        private Color GetSelectedBackColor() => BlendColor(BackColor, SelectedColor, IsDarkTheme() ? .55f : .22f);

        private Color GetHoveredBackColor() => BlendColor(BackColor, SelectedColor, IsDarkTheme() ? .30f : .10f);

        private Color GetSelectedBorderColor() => IsDarkTheme()
            ? ControlPaint.Light(SelectedColor, .20f)
            : ControlPaint.Dark(SelectedColor, .05f);

        private Color GetHoveredBorderColor() => IsDarkTheme()
            ? ControlPaint.Light(HoveredColor, .25f)
            : HoveredColor;

        private Color GetBorderColor() => IsDarkTheme()
            ? Color.FromArgb(86, 101, 121)
            : Color.FromArgb(194, 204, 216);

        private int GetMaxTextWidth(Func<AutocompleteItem, string> selector)
        {
            int width = 0;
            foreach (var item in visibleItems)
            {
                string text = selector(item);
                if (!string.IsNullOrEmpty(text))
                    width = Math.Max(width, TextRenderer.MeasureText(text, Font).Width);
            }

            return width;
        }

        private static int GetMaxTextWidth(IReadOnlyList<string> texts, Font font)
        {
            int width = 0;
            foreach (string text in texts)
            {
                if (!string.IsNullOrEmpty(text))
                    width = Math.Max(width, TextRenderer.MeasureText(text, font).Width);
            }

            return width;
        }

        private int GetMaxTextWidth(IReadOnlyList<string> texts)
            => GetMaxTextWidth(texts, Font);

        private readonly record struct ColumnLayout(
            int LabelWidth,
            int DetailLeft,
            int DetailWidth,
            int DescriptionLeft,
            int DescriptionWidth);

        private bool IsDarkTheme() => BackColor.GetBrightness() < .5f;

        private static Color BlendColor(Color baseColor, Color overlayColor, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                (int)(baseColor.R + (overlayColor.R - baseColor.R) * amount),
                (int)(baseColor.G + (overlayColor.G - baseColor.G) * amount),
                (int)(baseColor.B + (overlayColor.B - baseColor.B) * amount));
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = PointToItemIndex(e.Location);
            int viewportWidth = ClientSize.Width - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
            if (e.X < 0 || e.X >= viewportWidth || index < 0 || index >= visibleItems.Count)
            {
                index = -1;
            }

            if (hoveredItemIndex != index)
            {
                hoveredItemIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredItemIndex != -1)
            {
                hoveredItemIndex = -1;
                Invalidate();
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                FocussedItemIndex = PointToItemIndex(e.Location);
                DoSelectedVisible();
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            FocussedItemIndex = PointToItemIndex(e.Location);
            Invalidate();
            OnSelecting();
        }

        internal virtual void OnSelecting()
        {
            if (FocussedItemIndex < 0 || FocussedItemIndex >= visibleItems.Count)
                return;
            tb.TextSource.Manager.BeginAutoUndoCommands();
            try
            {
                AutocompleteItem item = FocussedItem;
                SelectingEventArgs args = new SelectingEventArgs()
                {
                    Item = item,
                    SelectedIndex = FocussedItemIndex
                };

                Menu.OnSelecting(args);

                if (args.Cancel)
                {
                    FocussedItemIndex = args.SelectedIndex;
                    Invalidate();
                    return;
                }

                if (!args.Handled)
                {
                    var fragment = Menu.Fragment;
                    DoAutocomplete(item, fragment);
                }

                Menu.Close();
                //
                SelectedEventArgs args2 = new SelectedEventArgs()
                {
                    Item = item,
                    Tb = Menu.Fragment.tb
                };
                item.OnSelected(Menu, args2);
                Menu.OnSelected(args2);
            }
            finally
            {
                tb.TextSource.Manager.EndAutoUndoCommands();
            }
        }

        private void DoAutocomplete(AutocompleteItem item, Range fragment)
        {
            string newText = item.GetTextForReplace();

            //replace text of fragment
            var tb = fragment.tb;

            tb.BeginAutoUndo();
            tb.TextSource.Manager.ExecuteCommand(new SelectCommand(tb.TextSource));
            if (tb.Selection.ColumnSelectionMode)
            {
                var start = tb.Selection.Start;
                var end = tb.Selection.End;
                start.iChar = fragment.Start.iChar;
                end.iChar = fragment.End.iChar;
                tb.Selection.Start = start;
                tb.Selection.End = end;
            }
            else
            {
                tb.Selection.Start = fragment.Start;
                tb.Selection.End = fragment.End;
            }
            tb.InsertText(newText);
            tb.TextSource.Manager.ExecuteCommand(new SelectCommand(tb.TextSource));
            tb.EndAutoUndo();
            tb.Focus();
        }

        int PointToItemIndex(Point p)
        {
            return (p.Y + VerticalScroll.Value) / ItemHeight;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ProcessKey(keyData, Keys.None);
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool ProcessKey(Keys keyData, Keys keyModifiers)
        {
            if (keyModifiers == Keys.None)
            switch (keyData)
            {
                case Keys.Down:
                    SelectNext(+1);
                    return true;
                case Keys.PageDown:
                    SelectNext(+10);
                    return true;
                case Keys.Up:
                    SelectNext(-1);
                    return true;
                case Keys.PageUp:
                    SelectNext(-10);
                    return true;
                case Keys.Enter:
                    OnSelecting();
                    return true;
                case Keys.Tab:
                    if (!AllowTabKey)
                        break;
                    OnSelecting();
                    return true;
                case Keys.Escape:
                    Menu.Close();
                    return true;
            }

            return false;
        }

        public void SelectNext(int shift)
        {
            FocussedItemIndex = Math.Max(0, Math.Min(FocussedItemIndex + shift, visibleItems.Count - 1));
            DoSelectedVisible();
            //
            Invalidate();
        }

        private void DoSelectedVisible()
        {
            if (FocussedItem != null)
                SetToolTip(FocussedItem);

            var y = FocussedItemIndex * ItemHeight - VerticalScroll.Value;
            if (y < 0)
                VerticalScroll.Value = FocussedItemIndex * ItemHeight;
            if (y > ClientSize.Height - ItemHeight)
                VerticalScroll.Value = Math.Min(VerticalScroll.Maximum, FocussedItemIndex * ItemHeight - ClientSize.Height + ItemHeight);
            //some magic for update scrolls
            AutoScrollMinSize -= new Size(1, 0);
            AutoScrollMinSize += new Size(1, 0);
        }

        private void SetToolTip(AutocompleteItem autocompleteItem)
        {
            var title = autocompleteItem.ToolTipTitle;
            var text = autocompleteItem.ToolTipText;

            if (string.IsNullOrEmpty(title))
            {
                toolTip.ToolTipTitle = null;
                toolTip.SetToolTip(this, null);
                return;
            }

            if (this.Parent != null)
            {
                IWin32Window window = this.Parent ?? this;
                Point location;

                if ((this.PointToScreen(this.Location).X + MaxToolTipSize.Width + 105) < Screen.FromControl(this.Parent).WorkingArea.Right)
                    location = new Point(Right + 5, 0);
                else
                    location = new Point(Left - 105 - MaximumSize.Width, 0);

                if (string.IsNullOrEmpty(text))
                {
                    toolTip.ToolTipTitle = null;
                    toolTip.Show(title, window, location.X, location.Y, ToolTipDuration);
                }
                else
                {
                    toolTip.ToolTipTitle = title;
                    toolTip.Show(text, window, location.X, location.Y, ToolTipDuration);
                }
            }
        }

        public int Count
        {
            get { return visibleItems.Count; }
        }

        public void SetAutocompleteItems(ICollection<string> items)
        {
            List<AutocompleteItem> list = new List<AutocompleteItem>(items.Count);
            foreach (var item in items)
                list.Add(new AutocompleteItem(item));
            SetAutocompleteItems(list);
        }

        public void SetAutocompleteItems(IEnumerable<AutocompleteItem> items)
        {
            sourceItems = items;
        }
    }

    public class SelectingEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public bool Cancel {get;set;}
        public int SelectedIndex{get;set;}
        public bool Handled { get; set; }
    }

    public class SelectedEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public FastColoredTextBox Tb { get; set; }
    }
}
