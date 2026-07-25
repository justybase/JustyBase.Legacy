using AppBase.Common;
using AppBase.Common.Enums;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS.Helpers;
using JustyBaseLegacy.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls
{
    public partial class ObjectExplorerControl : UserControl
    {
        private DataGridView _dgvObjectExplorer;
        private DataGridViewImageColumn _imageColumn;
        private readonly IEditorHost _hostWindow;
        private readonly IUiHelperService _uiHelperService;
        private readonly IColorTheme _colorTheme;
        private readonly IAutocompleteClass _autocompleteClass;
        private readonly ImageList _imageList;

        public ObjectExplorerControl()
        {
            InitializeComponent();
        }

        public ObjectExplorerControl(IEditorHost hostWindows, IUiHelperService uiHelperService, IColorTheme colorTheme, IAutocompleteClass autocompleteClass,
            ImageList imageList)
        {
            _hostWindow = hostWindows;
            _uiHelperService = uiHelperService;
            _colorTheme = colorTheme;
            _autocompleteClass = autocompleteClass;
            _imageList = imageList;
            InitializeComponent();
            InitializeObjectExplorer();
            this.CellMouseClick += ObjectExplorerControl_CellMouseClick;
            this.CellValueNeeded += ObjectExplorerControl_CellValueNeeded;
        }

        public DataGridView DataGridView => _dgvObjectExplorer;

        private void InitializeObjectExplorer()
        {
            var clImage = new DataGridViewImageColumn();
            var clName = new DataGridViewTextBoxColumn();
            _imageColumn = clImage;

            _dgvObjectExplorer = new ThemedDataGridView();
            ((System.ComponentModel.ISupportInitialize)(_dgvObjectExplorer)).BeginInit();

            // Configure DataGridView
            _dgvObjectExplorer.AllowUserToAddRows = false;
            _dgvObjectExplorer.AllowUserToDeleteRows = false;
            _dgvObjectExplorer.AllowUserToResizeColumns = false;
            _dgvObjectExplorer.AllowUserToResizeRows = false;
            _dgvObjectExplorer.BackgroundColor = SystemColors.ControlLightLight;
            _dgvObjectExplorer.BorderStyle = BorderStyle.Fixed3D;
            _dgvObjectExplorer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _dgvObjectExplorer.ColumnHeadersVisible = false;
            _dgvObjectExplorer.Columns.AddRange(new DataGridViewColumn[] { clImage, clName });
            _dgvObjectExplorer.Cursor = Cursors.Hand;
            _dgvObjectExplorer.Dock = DockStyle.Fill;
            _dgvObjectExplorer.MultiSelect = false;
            _dgvObjectExplorer.Name = "dgvObjectExplorer";
            _dgvObjectExplorer.ReadOnly = true;
            _dgvObjectExplorer.RowHeadersVisible = false;
            _dgvObjectExplorer.ScrollBars = ScrollBars.Vertical;
            _dgvObjectExplorer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvObjectExplorer.TabIndex = 6;
            _dgvObjectExplorer.VirtualMode = true;

            // Configure columns
            clImage.HeaderText = "";
            clImage.Name = "clImage";
            clImage.ReadOnly = true;
            clImage.ImageLayout = DataGridViewImageCellLayout.Zoom;
            clImage.Width = 20;

            clName.HeaderText = "";
            clName.Name = "clName";
            clName.ReadOnly = true;
            clName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            ((System.ComponentModel.ISupportInitialize)(_dgvObjectExplorer)).EndInit();

            // Add to control
            this.Controls.Add(_dgvObjectExplorer);

            // Apply styling
            if (_uiHelperService != null)
            {
                _uiHelperService.DoubleBufDateGridView(_dgvObjectExplorer);
            }

            if (_colorTheme != null)
            {
                _colorTheme.ColorForm(this);
            }

            ApplyDpiMetrics();
        }

        public void ApplyDpiMetrics()
        {
            if (_dgvObjectExplorer == null)
            {
                return;
            }

            int dpi = DeviceDpi;
            GridDpiMetrics.Apply(_dgvObjectExplorer, dpi, paddingLogical: 8, updateExistingRows: true);

            if (_imageColumn != null)
            {
                int iconColumnWidth = DpiScale.Scale(30, dpi);
                _imageColumn.Width = iconColumnWidth;
                _imageColumn.MinimumWidth = iconColumnWidth;
                _imageColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _imageColumn.DefaultCellStyle.Padding = new Padding(DpiScale.Scale(4, dpi));
            }

            _dgvObjectExplorer.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDpiMetrics();
        }

        // Event handler delegates that can be set from outside
        public event DataGridViewCellMouseEventHandler CellMouseClick
        {
            add { _dgvObjectExplorer.CellMouseClick += value; }
            remove { _dgvObjectExplorer.CellMouseClick -= value; }
        }

        public event DataGridViewCellValueEventHandler CellValueNeeded
        {
            add { _dgvObjectExplorer.CellValueNeeded += value; }
            remove { _dgvObjectExplorer.CellValueNeeded -= value; }
        }

        // Properties to expose DataGridView functionality
        public int RowCount
        {
            get { return _dgvObjectExplorer?.RowCount ?? 0; }
            set { if (_dgvObjectExplorer != null) _dgvObjectExplorer.RowCount = value; }
        }

        public DataGridViewSelectedRowCollection SelectedRows => _dgvObjectExplorer?.SelectedRows;

        public void ClearSelection()
        {
            _dgvObjectExplorer?.ClearSelection();
        }

        public void InvalidateRow(int rowIndex)
        {
            _dgvObjectExplorer?.InvalidateRow(rowIndex);
        }

        public new void Refresh()
        {
            _dgvObjectExplorer?.Refresh();
        }

        private void ObjectExplorerControl_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_hostWindow.CurrentTB != null)
            {
                var item = ExplorerList[e.RowIndex];
                _hostWindow.CurrentTB.GoEnd();
                _hostWindow.CurrentTB.SelectionStart = item.Position;
                _hostWindow.CurrentTB.SelectionLength = item.Title.TrimStart().Length;
                _hostWindow.CurrentTB.DoSelectionVisible();
                _hostWindow.CurrentTB.Focus();
            }
        }
        private void ObjectExplorerControl_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (ExplorerList.Count == 0 || e.RowIndex == -1)
                {
                    return;
                }
                ExplorerItem item = ExplorerList[e.RowIndex];
                if (e.ColumnIndex == 1)
                    e.Value = item.Title;
                else
                    switch (item.type)
                    {
                        case ExplorerItemType.Insert:
                            e.Value = JustData.Properties.Resources.table_row_insert;
                            return;
                        case ExplorerItemType.Delete:
                            e.Value = JustData.Properties.Resources.table_row_delete;
                            return;
                        case ExplorerItemType.Drop:
                            e.Value = JustData.Properties.Resources.table_delete;
                            return;
                        case ExplorerItemType.TemporatyTable:
                            e.Value = JustData.Properties.Resources.table_add;
                            return;
                        case ExplorerItemType.With:
                            e.Value = JustData.Properties.Resources.table;
                            return;
                        case ExplorerItemType.Select:
                            e.Value = JustData.Properties.Resources.Radiation;
                            return;
                        case ExplorerItemType.From:
                            e.Value = JustData.Properties.Resources.Movie;
                            return;
                        case ExplorerItemType.WhereGroupByLimit:
                            e.Value = JustData.Properties.Resources.Film;
                            return;
                        case ExplorerItemType.CreateView:
                            e.Value = _imageList.Images[9];
                            return;
                        case ExplorerItemType.CreateProcedure:
                            e.Value = _imageList.Images[5];
                            return;
                    }
            }
            catch {; }
        }

        private static readonly Regex _rxTable2 = RegexTable2();
        private static readonly Regex _rxCreateView = RegexCreateView();
        private static readonly Regex _rxCreateProcedure = RegexCreateProcedure();
        private static readonly Regex _rxWith2 = RegexWith2();
        private static readonly Regex _rxDelete = RegexDelete();
        private static readonly Regex _rxDrop = RegexDrop();
        private static readonly Regex _rxInsert = RegexInsert();

        [GeneratedRegex("\\b(create\\s+temp\\s+table|create\\s+table)\\s+(?<tableAlias>(\\w|\\.)+?)\\b\\s*as\\b\\s*\\({0,1}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexTable2();


        [GeneratedRegex("(,|with\\s)\\s*(?<tableAlias>\\w+)\\s+as\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexWith2();


        [GeneratedRegex("CREATE\\s+(OR\\s+REPLACE\\s+)?VIEW\\s+(?<name>\\w+)\\s+AS", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexCreateView();

        [GeneratedRegex("CREATE\\s+(OR\\s+REPLACE\\s+)?PROCEDURE\\s+(?<name>\\w+)\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexCreateProcedure();

        [GeneratedRegex("\\bdelete\\s+from\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexDelete();
        [GeneratedRegex("\\bdrop\\s+table\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexDrop();

        [GeneratedRegex("\\binsert\\s+into\\s+(?<table_name>(\\w|\\.)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        private static partial Regex RegexInsert();

        public void ReBuildObjectExplorer(string text)
        {
            try
            {
                List<ExplorerItem> list = new List<ExplorerItem>();
                Dictionary<int, int> dic = new Dictionary<int, int>();

                foreach (Match r in _rxTable2.Matches(text).Cast<Match>())
                {
                        string s = r.Groups["tableAlias"].Value;
                        int position = r.Groups["tableAlias"].Index;
                    dic[position] = 1;
                    if (SimpleComment(ref text, position))
                    {
                            var item = new ExplorerItem() { Title = s, Position = position, type = ExplorerItemType.TemporatyTable };
                        list.Add(item);
                    }
                }

                foreach (Match r in _rxDelete.Matches(text).Cast<Match>())
                {
                    string s = r.Groups["table_name"].Value;
                    int poz = r.Groups["table_name"].Index;
                    if (SimpleComment(ref text, poz))
                    {
                        var item = new ExplorerItem() { Title = s, Position = poz, type = ExplorerItemType.Delete };
                        list.Add(item);
                    }
                }
                foreach (Match r in _rxDrop.Matches(text).Cast<Match>())
                {
                    string s = r.Groups["table_name"].Value;
                    int poz = r.Groups["table_name"].Index;
                    if (SimpleComment(ref text, poz))
                    {
                        var item = new ExplorerItem() { Title = s, Position = poz, type = ExplorerItemType.Drop };
                        list.Add(item);
                    }
                }
                foreach (Match r in _rxInsert.Matches(text).Cast<Match>())
                {
                    string s = r.Groups["table_name"].Value;
                    int poz = r.Groups["table_name"].Index;
                    if (SimpleComment(ref text, poz))
                    {
                        var item = new ExplorerItem() { Title = s, Position = poz, type = ExplorerItemType.Insert };
                        list.Add(item);
                    }
                }
                foreach (Match r in _rxWith2.Matches(text).Cast<Match>())
                {
                    int position = r.Groups["tableAlias"].Index;
                    if (!dic.ContainsKey(position))
                    {
                        string s = r.Groups["tableAlias"].Value;
                        int balance = text.LeftParenthesesBalance(r.Index + 1);
                        if (SimpleComment(ref text, position))
                        {
                            int nn = s.Length + 2 * balance;
                            if (nn < 0)
                            {
                                nn = 0;
                            }
                            var item = new ExplorerItem() { Title = s.PadLeft(nn, ' '), Position = position, type = ExplorerItemType.With };
                            list.Add(item);
                        }
                    }
                }
                foreach (Match r in _rxCreateView.Matches(text).Cast<Match>())
                {
                    string s = r.Groups["name"].Value;
                    int poz = r.Groups["name"].Index;
                    if (SimpleComment(ref text, poz))
                    {
                        var item = new ExplorerItem() { Title = s, Position = poz, type = ExplorerItemType.CreateView };
                        list.Add(item);
                    }
                }
                foreach (Match r in _rxCreateProcedure.Matches(text).Cast<Match>())
                {
                    string s = r.Groups["name"].Value;
                    int poz = r.Groups["name"].Index;
                    if (SimpleComment(ref text, poz))
                    {
                        var item = new ExplorerItem() { Title = s, Position = poz, type = ExplorerItemType.CreateProcedure };
                        list.Add(item);
                    }
                }


                int m = _autocompleteClass.LastSelect(ref text, false);
                if (m != -1)
                {
                    var itemX = new ExplorerItem() { Title = "Select", Position = m + 1, type = ExplorerItemType.Select };
                    list.Add(itemX);
                    int m1 = _autocompleteClass.FirstFrom(text.Substring(m));
                    if (m1 != -1)
                    {
                        itemX = new ExplorerItem() { Title = "From", Position = m + m1 + 1, type = ExplorerItemType.From };
                        list.Add(itemX);
                        int m2 = _autocompleteClass.FirstWhereGroupLimit(text.Substring(m + m1 + 1));
                        if (m2 != -1)
                        {
                            switch (text[m + m1 + m2 + 2])
                            {
                                case 'W':
                                case 'w':
                                    itemX = new ExplorerItem() { Title = "Where", Position = m + m1 + m2 + 2, type = ExplorerItemType.WhereGroupByLimit };
                                    break;
                                case 'G':
                                case 'g':
                                    itemX = new ExplorerItem() { Title = "Group By", Position = m + m1 + m2 + 2, type = ExplorerItemType.WhereGroupByLimit };
                                    break;
                                case 'L':
                                case 'l':
                                    itemX = new ExplorerItem() { Title = "Limit", Position = m + m1 + m2 + 2, type = ExplorerItemType.WhereGroupByLimit };
                                    break;
                                default:
                                    break;
                            }

                            list.Add(itemX);
                        }
                    }
                }


                list.Sort(new ExplorerItemComparer());
                BeginInvoke(
                    new Action(() =>
                    {
                        ExplorerList = list;
                        RowCount = ExplorerList.Count;
                        Invalidate();
                    })
                );
            }
            catch {; }
        }
        public List<ExplorerItem> ExplorerList { get; set; } = new List<ExplorerItem>();
        private static bool SimpleComment(ref string text, int pos)
        {
            while (pos >= 1 && text[pos] != '\n')
            {
                if (text[pos] == '-' && text[pos - 1] == '-' && (pos == 1 || pos >= 2 && text[pos - 2] == '\n'))
                {
                    return false;
                }
                pos--;
            }
            return true;
        }


    }
}
