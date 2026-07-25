using System.Runtime.InteropServices;

namespace DatabaseDataGridView.WinForms
{
    public partial class SummariesChooseForm : Form
    {
        private bool _isDark;
        private Color _listBackColor;
        private Color _listForeColor;
        private Color _selectionBackColor;
        private Color _selectionForeColor;
        private Color _borderColor;
        private Color _buttonBackColor;
        private Color _buttonHoverColor;
        private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);

        [LibraryImport("uxtheme.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        public SummariesChooseForm(bool isDark, DataGridViewCellStyle st)
        {
            InitializeComponent();
            _isDark = isDark;
            ApplyTheme(st);
        }

        private void ApplyTheme(DataGridViewCellStyle st)
        {
            Color formBack = st.BackColor.IsEmpty ? SystemColors.Window : st.BackColor;
            Color formFore = st.ForeColor.IsEmpty ? SystemColors.WindowText : st.ForeColor;
            _isDark = _isDark || IsDarkColor(formBack);

            BackColor = formBack;
            ForeColor = GetReadableTextColor(formFore, formBack);

            _listBackColor = _isDark ? ControlPaint.Light(formBack, 0.04f) : Color.White;
            _listForeColor = GetReadableTextColor(formFore, _listBackColor);
            _selectionBackColor = AccentColor;
            _selectionForeColor = Color.White;
            _borderColor = _isDark
                ? ControlPaint.Light(formBack, 0.28f)
                : Color.FromArgb(203, 213, 225);
            _buttonBackColor = _isDark
                ? ControlPaint.Light(formBack, 0.12f)
                : Color.White;
            _buttonHoverColor = _isDark
                ? ControlPaint.Light(formBack, 0.22f)
                : Color.FromArgb(241, 245, 249);

            checkedListBox1.BackColor = _listBackColor;
            checkedListBox1.ForeColor = _listForeColor;
            checkedListBox1.BorderStyle = BorderStyle.FixedSingle;
            checkedListBox1.DrawMode = DrawMode.OwnerDrawFixed;
            checkedListBox1.ItemHeight = 30;
            checkedListBox1.IntegralHeight = false;
            checkedListBox1.CheckOnClick = true;
            checkedListBox1.DrawItem += CheckedListBox1_DrawItem;

            ApplyButtonTheme(button1, primary: true);
            ApplyButtonTheme(button2);

            label1.ForeColor = GetReadableTextColor(formFore, formBack);
            label1.BackColor = formBack;

            SetWindowTheme(checkedListBox1.Handle, _isDark ? "DarkMode_Explorer" : "Explorer", null);
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

        private void ApplyButtonTheme(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? AccentColor : _borderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(0, 102, 184) : _buttonHoverColor;
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0, 86, 156) : _buttonHoverColor;
            button.ForeColor = primary ? Color.White : ForeColor;
            button.BackColor = primary ? AccentColor : _buttonBackColor;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }

        private void CheckedListBox1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isChecked = checkedListBox1.GetItemChecked(e.Index);
            Color backgroundColor = selected ? _selectionBackColor : _listBackColor;
            Color textColor = selected ? _selectionForeColor : _listForeColor;

            using var background = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(background, e.Bounds);

            Rectangle checkBounds = new Rectangle(
                e.Bounds.Left + 10,
                e.Bounds.Top + (e.Bounds.Height - 16) / 2,
                16,
                16);
            using var checkBorder = new Pen(selected ? Color.White : AccentColor, 1);
            using var checkFill = new SolidBrush(isChecked ? AccentColor : Color.Transparent);
            e.Graphics.FillRectangle(checkFill, checkBounds);
            e.Graphics.DrawRectangle(checkBorder, checkBounds);

            if (isChecked)
            {
                using var checkMark = new Pen(Color.White, 2)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };
                e.Graphics.DrawLines(checkMark, new[]
                {
                    new Point(checkBounds.Left + 3, checkBounds.Top + 8),
                    new Point(checkBounds.Left + 7, checkBounds.Top + 12),
                    new Point(checkBounds.Left + 13, checkBounds.Top + 4)
                });
            }

            Rectangle textBounds = e.Bounds;
            textBounds.X += 36;
            textBounds.Width = Math.Max(0, textBounds.Width - 44);
            string text = checkedListBox1.Items[e.Index]?.ToString() ?? string.Empty;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.Font ?? checkedListBox1.Font,
                textBounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        public void textMode()
        {
            checkedListBox1.Items.Clear();
            checkedListBox1.Items.Add("COUNT");
            checkedListBox1.Items.Add("COUNT DISTINCT");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public string? Choosed { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                Choosed = checkedListBox1.CheckedItems[0]?.ToString();
            }
            else
            {
                Choosed = null;
            }

            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (i != e.Index)
                {
                    checkedListBox1.SetItemChecked(i, false);
                }
            }
        }

        public void chose(string name)
        {
            int a = checkedListBox1.Items.IndexOf(name);
            if (a >= 0)
            {
                checkedListBox1.SetItemChecked(a, true);
            }
        }
    }
}
