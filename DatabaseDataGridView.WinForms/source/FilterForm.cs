using DatabaseDataGridView.WinForms;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseDataGridView.WinForms
{
    public partial class FilterForm : UserControl
    {
        public FilterForm(int columnIndex, bool dark, DataGridViewCellStyle st)
        {
            InitializeComponent();
            _columnIndex = columnIndex;
            ApplyTheme(st, dark);
        }

        private bool _isDark;
        private Color _listBackColor;
        private Color _listForeColor;
        private Color _selectionBackColor;
        private Color _selectionForeColor;
        private Color _matchBackColor;
        private Color _borderColor;
        private Color _buttonBackColor;
        private Color _buttonHoverColor;
        private Color _mutedTextColor;
        private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);

        private void ApplyTheme(DataGridViewCellStyle st, bool dark)
        {
            Color gridBack = st.BackColor.IsEmpty ? SystemColors.Window : st.BackColor;
            Color gridFore = st.ForeColor.IsEmpty ? SystemColors.WindowText : st.ForeColor;
            _isDark = dark || IsDarkColor(gridBack);

            BackColor = gridBack;
            ForeColor = GetReadableTextColor(gridFore, gridBack);

            _listBackColor = _isDark ? ControlPaint.Light(gridBack, 0.04f) : Color.White;
            _listForeColor = GetReadableTextColor(gridFore, _listBackColor);
            _selectionBackColor = AccentColor;
            _selectionForeColor = Color.White;
            _matchBackColor = _isDark
                ? Color.FromArgb(72, 58, 24)
                : Color.FromArgb(255, 248, 218);
            _borderColor = _isDark
                ? ControlPaint.Light(gridBack, 0.28f)
                : Color.FromArgb(203, 213, 225);
            _buttonBackColor = _isDark
                ? ControlPaint.Light(gridBack, 0.12f)
                : Color.White;
            _buttonHoverColor = _isDark
                ? ControlPaint.Light(gridBack, 0.22f)
                : Color.FromArgb(241, 245, 249);
            _mutedTextColor = _isDark
                ? Color.FromArgb(190, 198, 208)
                : Color.FromArgb(100, 116, 139);

            tbFind.BackColor = _isDark ? ControlPaint.Light(gridBack, 0.08f) : Color.White;
            tbFind.ForeColor = GetReadableTextColor(gridFore, tbFind.BackColor);
            tbFind.BorderStyle = BorderStyle.FixedSingle;
            tbFind.PlaceholderText = "Find values...";

            listView1.BackColor = _listBackColor;
            listView1.ForeColor = _listForeColor;
            listView1.BorderStyle = BorderStyle.FixedSingle;
            listView1.HideSelection = false;
            listView1.FullRowSelect = true;

            SetButtonTheme(btConfirm, primary: true);
            SetButtonTheme(button1);
            SetButtonTheme(btNull);
            SetButtonTheme(btNotNull);

            lbInfo.ForeColor = _mutedTextColor;
            lbInfo.BackColor = BackColor;
            lbInfo.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static bool IsDarkColor(Color color)
        {
            double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
            return luminance < 96;
        }

        private static Color GetReadableTextColor(Color preferred, Color background)
        {
            double preferredLuminance = 0.2126 * preferred.R + 0.7152 * preferred.G + 0.0722 * preferred.B;
            double backgroundLuminance = 0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B;

            return Math.Abs(preferredLuminance - backgroundLuminance) < 80
                ? IsDarkColor(background) ? Color.White : Color.FromArgb(15, 23, 42)
                : preferred;
        }

        private void SetButtonTheme(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? AccentColor : _borderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(0, 102, 184) : _buttonHoverColor;
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0, 86, 156) : _buttonHoverColor;
            button.ForeColor = primary ? Color.White : ForeColor;
            button.BackColor = primary ? AccentColor : _buttonBackColor;
            button.UseVisualStyleBackColor = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Cursor = Cursors.Hand;
        }

        private int _columnIndex;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ListView ListView
        {
            get { return this.listView1; }
            set { this.listView1 = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<object> ValuesInFilter { get; set; } = [];

        private ListViewVirtualModeManager? _virtualModeManager;
        private DataFilter? _dataFilter;

        private static int ScaleDpi(int logicalPixels, int dpi) =>
            (int)Math.Round(logicalPixels * dpi / 96f);

        public void ApplyDpiMetrics(int width, int dpi)
        {
            int controlWidth = width;
            int margin = ScaleDpi(8, dpi);
            int gap = ScaleDpi(6, dpi);
            int inputHeight = ScaleDpi(29, dpi);
            int buttonHeight = ScaleDpi(28, dpi);
            int buttonWidth = Math.Max(ScaleDpi(80, dpi), (controlWidth - 2 * margin - gap) / 2);
            int firstRowTop = margin + inputHeight + gap;
            int secondRowTop = firstRowTop + buttonHeight + gap;
            int infoTop = secondRowTop + buttonHeight + ScaleDpi(5, dpi);
            int listTop = infoTop + ScaleDpi(22, dpi);

            Width = controlWidth;
            MinimumSize = new Size(ScaleDpi(200, dpi), ScaleDpi(200, dpi));

            tbFind.SetBounds(margin, margin, controlWidth - 2 * margin, inputHeight);
            btConfirm.SetBounds(margin, firstRowTop, buttonWidth, buttonHeight);
            button1.SetBounds(margin + buttonWidth + gap, firstRowTop, buttonWidth, buttonHeight);
            btNull.SetBounds(margin, secondRowTop, buttonWidth, buttonHeight);
            btNotNull.SetBounds(margin + buttonWidth + gap, secondRowTop, buttonWidth, buttonHeight);
            lbInfo.SetBounds(margin, infoTop, controlWidth - 2 * margin, ScaleDpi(18, dpi));
            listView1.SetBounds(margin, listTop, controlWidth - 2 * margin,
                Math.Max(ScaleDpi(100, dpi), Height - listTop - margin));

            if (listView1.Columns.Count > 0)
            {
                listView1.Columns[0].Width = Math.Max(0, listView1.ClientSize.Width - ScaleDpi(4, dpi));
            }
        }

        private void FilterForm_Load(object? sender, EventArgs e)
        {
            ApplyDpiMetrics(ScaleDpi(240, DeviceDpi), DeviceDpi);
            ColumnHeader header = new ColumnHeader();
            header.Text = "";
            header.Name = "col1";
            header.Width = listView1.Width - 2;
            listView1.Columns.Add(header);

            _virtualModeManager = new ListViewVirtualModeManager(listView1, ValuesInFilter);
            _virtualModeManager.Attach();

            _dataFilter = new DataFilter(this.Name, ValuesInFilter, RaiseSearchAsync);

            listView1.OwnerDraw = true;
            listView1.DrawItem += ListView1_DrawItem;
            listView1.DrawSubItem += ListView1_DrawSubItem;
            listView1.DrawColumnHeader += ListView1_DrawColumnHeader;

            this.Invoke(() =>
            {
                lbInfo.Text = $"{ValuesInFilter.Count:N0} items";
            });
        }

        private void ListView1_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            // In Details view the text is painted by DrawSubItem. Drawing the
            // item here as well makes the text disappear on some WinForms themes.
            if (listView1.View == View.Details)
            {
                return;
            }

            if (e.Item is not null)
            {
                DrawListItem(e.Graphics, e.Bounds, e.Item, e.Item.Selected);
            }
        }

        private void ListView1_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item is not null)
            {
                DrawListItem(e.Graphics, e.Bounds, e.Item, e.Item.Selected);
            }
        }

        private void ListView1_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var background = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(background, e.Bounds);
        }

        private void DrawListItem(Graphics graphics, Rectangle bounds, ListViewItem item, bool selected)
        {
            bool isSearchMatch = !string.IsNullOrWhiteSpace(tbFind.Text)
                && item.Text.Contains(tbFind.Text, StringComparison.OrdinalIgnoreCase);
            Color backgroundColor = selected
                ? _selectionBackColor
                : isSearchMatch ? _matchBackColor : _listBackColor;
            Color foregroundColor = selected ? _selectionForeColor : _listForeColor;

            using var background = new SolidBrush(backgroundColor);
            graphics.FillRectangle(background, bounds);

            Rectangle textBounds = bounds;
            textBounds.X += ScaleDpi(8, DeviceDpi);
            textBounds.Width = Math.Max(0, textBounds.Width - ScaleDpi(12, DeviceDpi));
            TextRenderer.DrawText(
                graphics,
                item.Text,
                item.Font ?? listView1.Font,
                textBounds,
                foregroundColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private string? _lastSearchedText;
        private int _lastIndex = 0;

        private void DoSearch()
        {
            ListViewItem? lvi = null;
            if (_lastSearchedText == tbFind.Text)
            {
                if (_lastIndex + 1 == ValuesInFilter.Count)
                {
                    _lastIndex = -1;
                }
                lvi = listView1.FindItemWithText(tbFind.Text, true, _lastIndex + 1);
            }
            else if (listView1.Items.Count > 0)
            {
                lvi = listView1.FindItemWithText(tbFind.Text, true, 0);
            }
            if (lvi == null)
            {
                _lastSearchedText = null;
                _lastIndex = -1;
            }
            else
            {
                _lastSearchedText = tbFind.Text;
                _lastIndex = lvi.Index;
                listView1.EnsureVisible(lvi.Index);
            }
        }
        private void TbFind_TextChanged(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void Timer1_Tick(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            listView1.Invalidate();
            DoSearch();

            _dataFilter?.ApplyFilter(tbFind.Text, false);
        }

        private void BtConfirm_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0 && ValuesInFilter.Count > 0)
            {
                var type = ValuesInFilter[0].GetType();
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.String: CreateAndSearchSet<string>(); break;
                    case TypeCode.Int32: CreateAndSearchSet<int>(); break;
                    case TypeCode.Int64: CreateAndSearchSet<long>(); break;
                    case TypeCode.Decimal: CreateAndSearchSet<decimal>(); break;
                    case TypeCode.Boolean: CreateAndSearchSet<bool>(); break;
                    case TypeCode.DateTime: CreateAndSearchSet<DateTime>(); break;
                }
            }
            this.Hide();
        }

        private void CreateAndSearchSet<T>()
        {
            var set = new HashSet<T>();
            foreach (int index in listView1.SelectedIndices)
            {
                set.Add((T)ValuesInFilter[index]);
            }
            OnSearch?.Invoke(this.Name, set, FilterType.inn, false);
        }

        public event Func<string, object?, FilterType, bool, Task>? OnSearch;

        private Task RaiseSearchAsync(string name, object? value, FilterType filterType, bool over) =>
            OnSearch?.Invoke(name, value, filterType, over) ?? Task.CompletedTask;

        private void ListView1_ItemSelectionChanged(object? sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                _dataFilter?.ApplyFilter(e.Item?.Tag?.ToString() ?? string.Empty, true);
                this.Hide();
            }
        }

        public event Action<int>? OnClear;

        private void Button1_Click(object? sender, EventArgs e)
        {
            OnClear?.Invoke(_columnIndex);
            this.Hide();
        }

        private void BtNull_Click(object? sender, EventArgs e)
        {
            OnSearch?.Invoke(this.Name, null, FilterType.isNull, false);
            this.Hide();
        }

        private void BtNotNull_Click(object? sender, EventArgs e)
        {
            OnSearch?.Invoke(this.Name, null, FilterType.isNotNull, false);
            this.Hide();
        }
    }
}
