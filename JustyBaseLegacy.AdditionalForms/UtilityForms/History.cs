using AppBase.Common;
using FastColoredTextBoxNS;
using JustyBaseLegacy.UI.Helpers;
using JustData.Application.History;
using JustData.ViewModels.History;
using System.Data;
using System.Drawing;

namespace JustyBaseLegacy.UI
{
    public partial class History : Form
    {
        private readonly bool _hist;
        private readonly DataTable _dthist;
        private readonly DataView _dthistView;

        private readonly Action<string, string, string> _addTabAction;
        private bool _splitDistanceInitialized;

        private readonly HistoryViewModel? _viewModel;
        private readonly string _historyDatFile;
        private bool _useViewModel;

        public History(Action<Form> DoColorize,
            Action<DataGridView> DoubleBuff,
            Action<string, string, string> addTabAction,
            string historyDatFile, bool useSpecialColoring
            , bool hist = true, string searchSuggestion = "")
        {
            InitializeComponent();

            DoColorize(this);

            _addTabAction = addTabAction;
            _historyDatFile = historyDatFile;
            this._hist = hist;
            _useViewModel = false;

            _dthist = new DataTable();
            _dthist.TableName = "tabelka";
            _dthist.Columns.Add("Date", typeof(DateTime));
            _dthist.Columns.Add("SQL", typeof(string));
            _dthist.Columns.Add("DB", typeof(string));
            _dthist.Columns.Add("Connection", typeof(string));

            lock (historyDatFile)
            {
                if (File.Exists(historyDatFile))
                {
                    using (BinaryReader br = new BinaryReader(new FileStream(historyDatFile, FileMode.Open, FileAccess.Read), encoding: System.Text.Encoding.UTF8))
                    {
                        while (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            var d1 = DateTime.FromBinary(br.ReadInt64());
                            var sql = br.ReadString();
                            var database = br.ReadString();
                            var connectionName = br.ReadString();
                            _dthist.Rows.Add(new object[] { d1, sql, database, connectionName });
                        }
                    }
                }
            }

            _dthistView = new DataView(_dthist);

            SetupGrid(useSpecialColoring);
            DoubleBuff(this.historyDataGridView);

            if (searchSuggestion != "")
            {
                textBox1.Text = searchSuggestion;
            }

            Load += (_, _) => ApplyDpiLayout();
        }

        public History(HistoryViewModel viewModel,
            Action<Form> DoColorize,
            Action<DataGridView> DoubleBuff,
            Action<string, string, string> addTabAction,
            string historyDatFile, bool useSpecialColoring)
        {
            InitializeComponent();

            DoColorize(this);

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _addTabAction = addTabAction;
            _historyDatFile = historyDatFile;
            _useViewModel = true;

            _dthist = new DataTable();
            _dthist.TableName = "tabelka";
            _dthist.Columns.Add("Date", typeof(DateTime));
            _dthist.Columns.Add("SQL", typeof(string));
            _dthist.Columns.Add("DB", typeof(string));
            _dthist.Columns.Add("Connection", typeof(string));
            _dthistView = new DataView(_dthist);

            SetupGrid(useSpecialColoring);
            DoubleBuff(this.historyDataGridView);

            Load += async (_, _) =>
            {
                ApplyDpiLayout();
                await _viewModel.LoadAsync(_historyDatFile);
                RefreshGridFromViewModel();
                SelectFirstRow();
            };

        }

        private void SetupGrid(bool useSpecialColoring)
        {
            historyDataGridView.Columns.Clear();
            historyDataGridView.Columns.Add("Date", "Date");
            historyDataGridView.Columns.Add("SQL", "SQL");
            historyDataGridView.Columns.Add("DB", "DB");
            historyDataGridView.Columns.Add("Connection", "Connection");

            historyDataGridView.VirtualMode = true;
            historyDataGridView.RowCount = _useViewModel ? 0 : _dthistView.Count;
            historyDataGridView.CellValueNeeded += historyDataGridView_CellValueNeeded;
            historyDataGridView.ColumnHeaderMouseClick += historyDataGridView_ColumnHeaderMouseClick;

            _dthistView.Sort = "Date DESC";
            historyDataGridView.Columns["Date"].HeaderCell.SortGlyphDirection = SortOrder.Descending;

            if (useSpecialColoring)
            {
                this.historyDataGridView.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                this.historyDataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
                this.historyDataGridView.DefaultCellStyle.ForeColor = Color.FromArgb(241, 241, 241);
                this.historyDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(241, 241, 241);
                this.historyDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            }

            this.historyDataGridView.EnableHeadersVisualStyles = false;
            this.historyDataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.historyDataGridView.KeyDown += HistoryDataGridView_KeyDown;
            this.historyDataGridView.DoubleClick += HistoryDataGridView_DoubleClick;
            this.historyDataGridView.CellToolTipTextNeeded += HistoryDataGridView_CellToolTipTextNeeded;
            this.historyDataGridView.SelectionChanged += HistoryDataGridView_SelectionChanged;

            splitContainer1.Panel2.BackColor = Color.FromArgb(248, 249, 250);

            textBox1.KeyDown += TextBox_KeyDown;
            fastColoredTextBox1.Enabled = false;
            fastColoredTextBox1.Visible = !_hist;
        }

        public void PrepareForDocumentHost()
        {
            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            Dock = DockStyle.Fill;
        }

        private void RefreshGridFromViewModel()
        {
            if (_viewModel is null) return;
            historyDataGridView.RowCount = _viewModel.FilteredEntries.Count;
            historyDataGridView.Invalidate();
        }

        private void SelectFirstRow()
        {
            if (historyDataGridView.RowCount <= 0) return;
            try
            {
                if (historyDataGridView.Rows.Count > 0)
                {
                    historyDataGridView.CurrentCell = historyDataGridView.Rows[0].Cells[0];
                    historyDataGridView.Rows[0].Selected = true;
                }
            }
            catch
            {
            }
        }

        private IReadOnlyList<HistoryEntry> GetCurrentEntries()
        {
            if (_useViewModel && _viewModel is not null)
                return _viewModel.FilteredEntries;
            return [];
        }

        private HistoryEntry? GetEntryAt(int rowIndex)
        {
            if (_useViewModel && _viewModel is not null)
            {
                var entries = _viewModel.FilteredEntries;
                return rowIndex >= 0 && rowIndex < entries.Count ? entries[rowIndex] : null;
            }
            return null;
        }

        private void ApplyDpiLayout()
        {
            int dpi = DeviceDpi;
            int margin = DpiScale.Scale(20, dpi);
            int controlHeight = DpiScale.Scale(30, dpi);
            int searchFieldWidth = DpiScale.Scale(350, dpi);
            int buttonWidth = DpiScale.Scale(100, dpi);

            panelHeader.Padding = new Padding(margin);
            panelHeader.Height = DpiScale.Scale(100, dpi);

            labelSearch.Location = new Point(0, DpiScale.Scale(4, dpi));
            textBox1.SetBounds(0, DpiScale.Scale(28, dpi), searchFieldWidth, controlHeight);
            button1.SetBounds(textBox1.Right + DpiScale.Scale(20, dpi), textBox1.Top, buttonWidth, controlHeight);
            panelSearch.Width = button1.Right + DpiScale.Scale(8, dpi);
            panelSearch.Height = button1.Bottom + DpiScale.Scale(4, dpi);

            int belowHeader = panelHeader.Bottom + DpiScale.Scale(10, dpi);
            labelResults.Location = new Point(margin, belowHeader);

            int splitTop = labelResults.Bottom + DpiScale.Scale(8, dpi);
            if (fastColoredTextBox1.Visible)
            {
                fastColoredTextBox1.SetBounds(
                    margin + DpiScale.Scale(3, dpi),
                    splitTop,
                    Math.Max(0, ClientSize.Width - margin * 2),
                    DpiScale.Scale(60, dpi));
                splitTop = fastColoredTextBox1.Bottom + DpiScale.Scale(8, dpi);
            }

            splitContainer1.Location = new Point(margin, splitTop);
            splitContainer1.Size = new Size(
                Math.Max(0, ClientSize.Width - margin * 2),
                Math.Max(0, ClientSize.Height - splitTop - margin));
            splitContainer1.SplitterWidth = DpiScale.Scale(8, dpi);

            if (!_splitDistanceInitialized && splitContainer1.Height > splitContainer1.SplitterWidth * 2)
            {
                splitContainer1.SplitterDistance = Math.Max(DpiScale.Scale(220, dpi), splitContainer1.Height / 2);
                _splitDistanceInitialized = true;
            }

            int previewHeader = DpiScale.Scale(26, dpi);
            labelSqlPreview.Location = new Point(DpiScale.Scale(10, dpi), DpiScale.Scale(4, dpi));
            fastColoredTextBox2.Location = new Point(0, previewHeader);

            ApplyGridDpiMetrics(dpi);
            FctbDpiHelper.ApplyCharMetrics(fastColoredTextBox1, 8);
            FctbDpiHelper.ApplyCharMetrics(fastColoredTextBox2, 10);
        }

        private void ApplyGridDpiMetrics(int dpi)
        {
            int rowHeight = (int)Math.Ceiling(historyDataGridView.Font.GetHeight()) + DpiScale.Scale(8, dpi);
            historyDataGridView.RowTemplate.Height = rowHeight;
            historyDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            historyDataGridView.ColumnHeadersHeight = rowHeight + DpiScale.Scale(4, dpi);
            historyDataGridView.DefaultCellStyle.Padding = new Padding(DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi));

            foreach (DataGridViewRow row in historyDataGridView.Rows)
            {
                row.Height = rowHeight;
            }

            if (historyDataGridView.Columns.Count >= 4)
            {
                historyDataGridView.Columns[0].Width = DpiScale.Scale(180, dpi);
                historyDataGridView.Columns[2].Width = DpiScale.Scale(140, dpi);
                historyDataGridView.Columns[3].Width = DpiScale.Scale(160, dpi);
                historyDataGridView.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                historyDataGridView.Columns[1].MinimumWidth = DpiScale.Scale(200, dpi);
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplyDpiLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated)
            {
                ApplyDpiLayout();
            }
        }

        private void historyDataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_useViewModel && _viewModel is not null)
            {
                var entries = _viewModel.FilteredEntries;
                if (e.RowIndex >= entries.Count) return;
                var entry = entries[e.RowIndex];
                e.Value = e.ColumnIndex switch
                {
                    0 => entry.Date,
                    1 => entry.Sql,
                    2 => entry.Database,
                    3 => entry.ConnectionName,
                    _ => null
                };
                return;
            }

            if (e.RowIndex >= _dthistView.Count) return;
            e.Value = _dthistView[e.RowIndex][e.ColumnIndex];
        }

        private void historyDataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_useViewModel)
                return;

            string newSortColumn = historyDataGridView.Columns[e.ColumnIndex].Name;
            SortOrder newSortOrder = SortOrder.Ascending;

            if (_dthistView.Sort.Contains(newSortColumn))
            {
                newSortOrder = _dthistView.Sort.EndsWith("ASC") ? SortOrder.Descending : SortOrder.Ascending;
            }

            foreach (DataGridViewColumn column in historyDataGridView.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            _dthistView.Sort = $"{newSortColumn} {(newSortOrder == SortOrder.Ascending ? "ASC" : "DESC")}";
            historyDataGridView.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = newSortOrder;

            historyDataGridView.Invalidate();
        }

        public int RowNum { get; set; }

        readonly DataTable databaseDataTable = new DataTable();

        private void HistoryDataGridView_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
                return;

            try
            {
                string? w = null;
                if (_useViewModel && _viewModel is not null)
                {
                    var entries = _viewModel.FilteredEntries;
                    if (e.RowIndex < entries.Count)
                    {
                        w = e.ColumnIndex switch
                        {
                            0 => entries[e.RowIndex].Date.ToString(),
                            1 => entries[e.RowIndex].Sql,
                            2 => entries[e.RowIndex].Database,
                            3 => entries[e.RowIndex].ConnectionName,
                            _ => null
                        };
                    }
                }
                else if (e.RowIndex < _dthistView.Count)
                {
                    w = _dthistView[e.RowIndex][e.ColumnIndex]?.ToString();
                }

                if (w is not null && w.Length > 1000)
                {
                    e.ToolTipText = $"{w.Substring(0, 1000)}{Environment.NewLine}...";
                }
                else
                {
                    e.ToolTipText = w;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"History error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoSearch()
        {
            if (_useViewModel && _viewModel is not null)
            {
                _viewModel.Filter(textBox1.Text);
                historyDataGridView.RowCount = _viewModel.FilteredEntries.Count;
                historyDataGridView.Invalidate();
                return;
            }

            if (_historySearchTimer == null)
            {
                _historySearchTimer = new System.Windows.Forms.Timer();
                _historySearchTimer.Interval = 20;
                _historySearchTimer.Tick += new EventHandler(this.StrikeTimer);
            }
            _historySearchTimer.Stop();
            _historySearchTimer.Tag = textBox1.Text;
            _historySearchTimer.Start();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoSearch();
            }
        }

        private void HistoryDataGridView_DoubleClick(object sender, EventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv?.SelectedCells.Count != 1) return;

            var cell = dgv.SelectedCells[0];
            int rowIndex = cell.RowIndex;

            if (_useViewModel && _viewModel is not null)
            {
                var entries = _viewModel.FilteredEntries;
                if (rowIndex >= 0 && rowIndex < entries.Count)
                {
                    var entry = entries[rowIndex];
                    _addTabAction(null, "hist_", entry.Sql);
                }
                return;
            }

            int colIndex = cell.ColumnIndex;
            if (rowIndex >= 0 && colIndex >= 0 && rowIndex < _dthistView.Count)
            {
                var value = _dthistView[rowIndex][colIndex]?.ToString();
                _addTabAction(null, "hist_", value);
            }
        }

        System.Windows.Forms.Timer? _historySearchTimer = null;

        private void StrikeTimer(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;

            if (timer == null)
                return;

            timer.Stop();
            try
            {
                _dthistView.RowFilter = $"SQL like '%{timer.Tag}%'";
                historyDataGridView.RowCount = _dthistView.Count;
                historyDataGridView.Invalidate();
            }
            catch
            {
            }
        }

        private void TextBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (textBox1.Text == "search...")
            {
                textBox1.Text = "";
            }
        }

        private void FastColoredTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (ModifierKeys == Keys.Control && e.KeyCode == Keys.Return)
            {
                DoSearch();
            }
        }

        private void HistoryDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                string sqlContent = "";
                int rowIndex = -1;

                if (historyDataGridView.SelectedRows.Count > 0)
                {
                    rowIndex = historyDataGridView.SelectedRows[0].Index;
                }
                else if (historyDataGridView.SelectedCells.Count > 0)
                {
                    rowIndex = historyDataGridView.SelectedCells[0].RowIndex;
                }

                if (_useViewModel && _viewModel is not null)
                {
                    var entries = _viewModel.FilteredEntries;
                    if (rowIndex >= 0 && rowIndex < entries.Count)
                    {
                        sqlContent = entries[rowIndex].Sql;
                    }
                }
                else if (rowIndex >= 0 && rowIndex < _dthistView.Count)
                {
                    sqlContent = _dthistView[rowIndex]["SQL"]?.ToString();
                }

                fastColoredTextBox2.Text = sqlContent;
            }
            catch (Exception)
            {
                fastColoredTextBox2.Text = "";
            }
        }

        private void HistoryDataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (historyDataGridView.SelectedCells.Count != 1)
                    return;

                var cell = historyDataGridView.SelectedCells[0];
                var rowIndex = cell.RowIndex;
                var colIndex = cell.ColumnIndex;

                string? text = null;
                if (_useViewModel && _viewModel is not null)
                {
                    var entries = _viewModel.FilteredEntries;
                    if (rowIndex >= 0 && rowIndex < entries.Count)
                    {
                        var entry = entries[rowIndex];
                        text = colIndex switch
                        {
                            0 => entry.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                            1 => entry.Sql,
                            2 => entry.Database,
                            3 => entry.ConnectionName,
                            _ => null
                        };
                    }
                }
                else if (rowIndex >= 0 && rowIndex < _dthistView.Count && colIndex >= 0)
                {
                    text = _dthistView[rowIndex][colIndex]?.ToString();
                }

                if (text is not null)
                {
                    Clipboard.SetText(text);
                    e.Handled = true;
                }
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            DoSearch();
        }
    }
}
