using FastColoredTextBoxNS;
using DatabaseDataGridView.WinForms.Commands;
using DatabaseDataGridView.WinForms.Models;
using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DatabaseDataGridView.WinForms.Coloring;
using DatabaseDataGridView.WinForms.Interfaces;
using DatabaseDataGridView.WinForms.Extensions;
using JustyBase.Core.Grid;


namespace DatabaseDataGridView.WinForms
{
    public partial class CustomDataGridView : UserControl, ICustomDataGridView
    {
        private readonly IExportMakes _importExportTasks;
        private readonly IUiHelperService _uiHelperService;
        private readonly IColorTheme _colorTheme;
        public CustomDataGridView(IColorTheme colorTheme,
            object importExportTasks, IUiHelperService uiHelperService, 
            FastColoredTextBox fctb, DataTable dt, List<object[]> originalDataList,
            SqlFirstRenderProbeRun? firstRenderProbeRun = null)
        {
            _colorTheme = colorTheme; 
            _importExportTasks = (IExportMakes)importExportTasks;
            _uiHelperService = uiHelperService;
            InitializeComponent();
            Disposed += (_, _) =>
            {
                _statsDebounceTimer.Stop();
                _statsDebounceTimer.Dispose();
            };
            _statsDebounceTimer.Tick += DataGridView1_SelectionStatsTick;
            toolTip1.SetToolTip(btOpenInExcel, "open as excel file");
            toolTip1.SetToolTip(btCopyAsExcel, "copy as excel file");
            toolTip1.SetToolTip(btCopyAsText, "copy table to clipboard");
            toolTip1.SetToolTip(btDownload, "save result to file");
            toolTip1.SetToolTip(btRowView, "Row view");

            foreach (Button button in new[] { btCopyAsExcel, btCopyAsText, btOpenInExcel, btDownload, btRowView })
            {
                // The old resources are low-resolution bitmaps.  Leaving them
                // in the designer is useful for backwards compatibility, but
                // the toolbar paints crisp DPI-independent glyphs at runtime.
                button.BackgroundImage = null;
                button.Text = string.Empty;
                button.Paint += ToolbarButton_Paint;
            }

            dgvDrop.RowHeadersVisible = false;
            dgvDrop.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvDrop.CellPainting += DgvDrop_CellPainting;
            dgvDrop.MouseLeave += DgvDrop_MouseLeave;
            dataGridView1.VirtualMode = true;
            dataGridView1.RowHeightInfoNeeded += DataGridView1_RowHeightInfoNeeded;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.ColumnHeaderSelect;
            FctbX = fctb;
            _dataTable = dt;
            _originalDataList = originalDataList;
            _workingRowsList = originalDataList;

            ColumnsWidths = new int[CurrentDataTable.Columns.Count];
            if (dataGridView1 is null)
            {
                return;
            }
dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.DataError += DataGridView1_DataError;
            dataGridView1.ColumnWidthChanged += DataGridView_ColumnWidthChanged;
            dataGridView1.Scroll += DataGridView1_Scroll;
            dgvSummaries.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvSummaries.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvSummaries.CellPainting += DgvSummaries_CellPainting;

            // Keep the small toolbar controls visually consistent with the
            // custom grid headers.  The designer still owns the resources and
            // click handlers; only the chrome is normalized here so it also
            // follows the current DPI.
            cbAprox.Appearance = Appearance.Button;
            cbAprox.AutoSize = false;
            cbAprox.Text = "≈";
            cbAprox.TextAlign = ContentAlignment.MiddleCenter;
            cbAprox.FlatStyle = FlatStyle.Flat;

            GrifOffsetHeight = ScaleDpi(RowPaddingLogical, DeviceDpi);
            ApplyDpiMetrics();
            _uiHelperService.DoubleBufDateGridView(dataGridView1);
            _uiHelperService.DoubleBufDateGridView(dgvSummaries);
            _uiHelperService.DoubleBufDateGridView(dgvDrop);
            dataGridView1.ConfigureFirstRenderProbe(firstRenderProbeRun, CurrentDataTable.Columns.Count);
        }

        public event EventHandler? NewSqlTabRequested
        {
            add => dataGridView1.NewSqlTabRequested += value;
            remove => dataGridView1.NewSqlTabRequested -= value;
        }

        public event EventHandler? RowViewRequested;

        /// <summary>Unique accessibility name used to distinguish result grids in UI automation.</summary>
        public string ResultGridAccessibilityName
        {
            set => dataGridView1.AccessibleName = value;
        }

        public TabControl? ParentParent => this?.Parent?.Parent as TabControl;
        private void DataGridView1_DataError(object? sender, DataGridViewDataErrorEventArgs anError)
        {
            string message = anError.Context switch
            {
                DataGridViewDataErrorContexts.Commit => "Could not commit the cell value.",
                DataGridViewDataErrorContexts.CurrentCellChange => "Could not change the current cell.",
                DataGridViewDataErrorContexts.Parsing => "Could not parse the cell value.",
                DataGridViewDataErrorContexts.LeaveControl => "Could not leave the cell.",
                _ => "A data grid error occurred."
            };

            MessageBox.Show(message, "Data grid error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if ((anError.Exception) is ConstraintException)
            {
                if (sender is not DataGridView view)
                {
                    return;
                }
                view.Rows[anError.RowIndex].ErrorText = message;
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = message;

                anError.ThrowException = false;
            }
        }

        private static bool TestColumnForInteger(DataColumn dtc)
        {
            if ((dtc.DataType == typeof(Int16) || dtc.DataType == typeof(int) || dtc.DataType == typeof(long))
                    && !dtc.ColumnName.Contains("_DATE", StringComparison.OrdinalIgnoreCase)
                    && !dtc.ColumnName.Contains("DATA_ANALIZY", StringComparison.OrdinalIgnoreCase)
                    && !dtc.ColumnName.StartsWith("DATA_", StringComparison.OrdinalIgnoreCase)
                    )
            {
                return true;
            }
            return false; ;
        }

        private static int GetHeaderChromeWidth(int dpi)
        {
            int gap = ScaleDpi(3, dpi);
            int pinW = ScaleDpi(14, dpi);
            int filterW = ScaleDpi(16, dpi);
            int aggW = ScaleDpi(14, dpi);
            int sortW = ScaleDpi(12, dpi);
            int textPadX = ScaleDpi(4, dpi);
            return textPadX + gap + sortW + gap + aggW + gap + filterW + gap + pinW + textPadX;
        }

        private int GetAutoSizeColumnsWidth(int colNum)
        {
            if (CurrentDataTable == null)
                return ScaleDpi(100, DeviceDpi);

            int dpi = DeviceDpi;
            int minWidth = ScaleDpi(50, dpi);
            int maxWidth = ScaleDpi(500, dpi);
            int cellPadding = ScaleDpi(10, dpi);
            int headerChrome = GetHeaderChromeWidth(dpi);

            int colWidth = minWidth;

            int headerWidth = TextRenderer.MeasureText(
                CurrentDataTable.Columns[colNum].ColumnName,
                dataGridView1.Font).Width + headerChrome;

            int rowsToCheck = (WorkingRowsList.Count > 1000 ? 1000 : WorkingRowsList.Count);
            Dictionary<string, int> knownLengthsCache = [];

            for (int rowNum = 0; rowNum < rowsToCheck; rowNum++)
            {
                //var val1 = dataTable.Rows[rowNum][colNum];
                var val1 = WorkingRowsList[rowNum][colNum];
                string stringToMeasure = "";

                if (val1 is null)
                {
                    stringToMeasure = "NULL";
                }
                else if (val1.GetType() == typeof(DBNull))
                {
                    stringToMeasure = "NULL";
                }
                else if (CurrentDataTable.Columns[colNum].DataType == typeof(System.DateTime))
                {
                    stringToMeasure = ((DateTime)val1).ToString(DateTimeFormat);
                }
                else if (CurrentDataTable.Columns[colNum].DataType == typeof(decimal) && val1 is decimal decVal)
                {
                    stringToMeasure = decVal.ToString(getDecimalFormatFor(colNum));
                }
                else if (CurrentDataTable.Columns[colNum].DataType == typeof(double))
                {
                    stringToMeasure = ((double)val1).ToString(getDecimalFormatFor(colNum));
                }
                else if (CurrentDataTable.Columns[colNum].DataType == typeof(Single))
                {
                    stringToMeasure = ((Single)val1).ToString(getDecimalFormatFor(colNum));
                }
                else if (TestColumnForInteger(CurrentDataTable.Columns[colNum]))
                {
                    if (CurrentDataTable.Columns[colNum].DataType == typeof(int))
                        stringToMeasure = ((int)val1).ToString(IntegerFormat);
                    else if (CurrentDataTable.Columns[colNum].DataType == typeof(Int16))
                        stringToMeasure = ((Int16)val1).ToString(IntegerFormat);
                    else
                        stringToMeasure = ((long)val1).ToString(IntegerFormat);
                }
                else
                {
                    stringToMeasure = val1.ToString() ?? string.Empty;
                }

                int temp = 0;
                if (rowNum < 100 || knownLengthsCache.Count < 10)
                {
                    if (knownLengthsCache.TryGetValue(stringToMeasure, out int value))
                    {
                        temp = value;
                    }
                    else
                    {
                        temp = TextRenderer.MeasureText(stringToMeasure, dataGridView1.Font).Width;
                        knownLengthsCache[stringToMeasure] = temp;
                    }
                }

                if (temp > colWidth)
                    colWidth = temp;

                if (colWidth > maxWidth)
                {
                    colWidth = maxWidth;
                    break;
                }
            }

            colWidth += cellPadding;

            if (headerWidth > colWidth)
            {
                colWidth = headerWidth;
            }

            return Math.Max(minWidth, colWidth);
        }

        private void ApplyColumnWidthsForDpi()
        {
            if (ColumnsWidths == null || dataGridView1.Columns.Count == 0)
            {
                return;
            }

            int count = Math.Min(ColumnsWidths.Length, dataGridView1.Columns.Count);
            for (int i = 0; i < count; i++)
            {
                int width = GetAutoSizeColumnsWidth(i);
                ColumnsWidths[i] = width;
                dataGridView1.Columns[i].Width = width;
                if (i < dgvSummaries.Columns.Count)
                {
                    dgvSummaries.Columns[i].Width = width;
                }
            }
        }

        private void GetSizeOfAllCols()
        {
            try
            {
                Parallel.For(0, ColumnsWidths.Length/*, new ParallelOptions { MaxDegreeOfParallelism = 3 }*/, i =>
                {
                    ColumnsWidths[i] = GetAutoSizeColumnsWidth(i);
                });
            }
            catch (Exception)
            {
                for (int i = 0; i < ColumnsWidths.Length; i++)
                {
                    ColumnsWidths[i] = GetAutoSizeColumnsWidth(i);
                }
            }
        }

        private bool _wasPreview = false;

        public bool IsEmpty { get; set; }

        readonly Dictionary<int, bool> _isBoolean = new Dictionary<int, bool>();

        private void DoPreview()
        {
            dataGridView1.ColumnHeadersVisible = false;

            dgvSummaries.ColumnHeadersVisible = false;
            dgvSummaries.ScrollBars = ScrollBars.None;

            _wasPreview = true;

            // Deliberate wide-result fast path. Reset the virtual grid once,
            // construct every column off-grid, then attach the complete array
            // with AddRange below. Adding/binding columns one by one makes
            // DataGridView repeatedly recalculate layout and becomes
            // prohibitively slow for results with hundreds of columns.
            dataGridView1.ColumnCount = 0;

            DataGridViewColumn[] dgvCols = new DataGridViewColumn[CurrentDataTable.Columns.Count];
            DataGridViewColumn[] SummariesCols = new DataGridViewColumn[CurrentDataTable.Columns.Count];

            for (int i = 0; i < CurrentDataTable.Columns.Count; i++)
            {
                DataGridViewCell cl;

                if (CurrentDataTable.Columns[i].DataType == typeof(System.DateTime))
                {
                    cl = new DataGridViewTextBoxCell();
                    cl.Style.Format = DateTimeFormat;//"yyyy-MM-dd HH:mm:ss";
                }
                else if (CurrentDataTable.Columns[i].DataType == typeof(decimal)
                    || CurrentDataTable.Columns[i].DataType == typeof(double) || CurrentDataTable.Columns[i].DataType == typeof(float))
                {
                    cl = new DataGridViewTextBoxCell();
                    cl.Style.Format = getDecimalFormatFor(i);
                    cl.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (TestColumnForInteger(CurrentDataTable.Columns[i]))
                {
                    cl = new DataGridViewTextBoxCell();
                    cl.Style.Format = IntegerFormat;
                    cl.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (CurrentDataTable.Columns[i].DataType == typeof(bool))
                {
                    cl = new DataGridViewCheckBoxCell(true);
                    _isBoolean[i] = true;
                }
                else
                {
                    cl = new DataGridViewTextBoxCell();
                }

                dgvCols[i] = new DataGridViewColumn(cl)
                {
                    Name = CurrentDataTable.Columns[i].ColumnName,
                    HeaderText = CurrentDataTable.Columns[i].ColumnName,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = ColumnsWidths[i],
                };

                SummariesCols[i] = new DataGridViewColumn((DataGridViewCell)cl.Clone())
                {
                    Name = CurrentDataTable.Columns[i].ColumnName,
                    HeaderText = "",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = ColumnsWidths[i]
                };
            }

            float fw = 0; // FillWeight limit !
            for (int i = 0; i < dgvCols.Length; i++)
            {
                fw += dgvCols[i].FillWeight;
            }
            if (fw > 65_534)
            {
                float x = 65_000 / fw;
                for (int i = 0; i < dgvCols.Length; i++)
                {
                    dgvCols[i].FillWeight = x * dgvCols[i].FillWeight;
                    SummariesCols[i].FillWeight = x * dgvCols[i].FillWeight;
                }
            }


            dataGridView1.Columns.AddRange(dgvCols);
            dgvSummaries.Columns.AddRange(SummariesCols);
            dataGridView1.ColumnHeadersVisible = true;
            dgvSummaries.ColumnHeadersVisible = true;

        }

        readonly int[] ColumnsWidths;
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial int SendMessage(IntPtr hWnd, Int32 wMsg, [MarshalAs(UnmanagedType.Bool)] bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        private void DgvPaintStop()
        {
            if (dataGridView1 is not null)
            {
                SendMessage(dataGridView1.Handle, WM_SETREDRAW, false, 0);
            }
        }

        private void DgvPaintStart()
        {
            if (dataGridView1 is not null)
            {
                SendMessage(dataGridView1.Handle, WM_SETREDRAW, true, 0);
            }
        }

        private DataTable? _schemaDataTable;

        public DataTable ShemaDataTable
        {
            set
            {
                if (value is null)
                {
                    return;
                }
                _schemaDataTable = value;

                if (_schemaDataTable.Columns.Contains("DataType") && _schemaDataTable.Columns.Contains("NumericScale")
                    && !String.IsNullOrWhiteSpace(DecimalFormat) && Regex.IsMatch(DecimalFormat, @"^(N|F)\d+$")
                    )
                {
                    _decimalFormats = new string[_schemaDataTable.Rows.Count];
                    for (int i = 0; i < _decimalFormats.Length; i++)
                    {
                        var tmp = _schemaDataTable.Rows[i]["DataType"];
                        if (tmp is null || tmp == DBNull.Value)
                        {
                            continue;
                        }

                        var type = ((Type)tmp);
                        if (type == typeof(Decimal))
                        {
                            _decimalFormats[i] = $"{DecimalFormat[0]}{_schemaDataTable.Rows[i]["NumericScale"]}";
                        }
                        else
                        {
                            _decimalFormats[i] = "";
                        }
                    }
                }
            }
        }

        public void EnsureColumnList()
        {
            var dgv = dataGridView1;
            if (dgv is null)
                return;

            if (!IsHandleCreated)
                return;

            BeginInvoke(() =>
            {
                if (cbJumpToColumn is not null && cbJumpToColumn.Items.Count == 0)
                {
                    foreach (DataGridViewColumn item in dgv.Columns)
                    {
                        cbJumpToColumn.Items.Add(item.Name);
                    }
                }
            });

        }


        public void InitGrid(bool previewMode = false)
        {
            if (dataGridView1 is null)
            {
                return;
            }

            Stopwatch st = new Stopwatch();
            List<string> ls = new List<string>();
            st.Start();

            ls.Add($"{st.ElapsedMilliseconds} - dataTable binding");
            st.Restart();

            if (previewMode || !previewMode && !_wasPreview)
            {
                GetSizeOfAllCols();
            }

            ls.Add($"{st.ElapsedMilliseconds} - GetSizeOfAllColsAsync");
            st.Restart();

            if (dataGridView1?.IsHandleCreated != true)
            {
                return;
            }

            dataGridView1.Invoke(()=>
            {
                if (dataGridView1 is null)
                {
                    return;
                }
                DgvPaintStop();
                dataGridView1.RowCount = 0;

                if (previewMode || !previewMode && !_wasPreview)
                {
                    DoPreview();
                    if (previewMode)
                    {
                        dataGridView1.RowCount = 0;
                        lbCnt.Text = "500";
                        dataGridView1.RowCount = 500;
                    }
                    dataGridView1.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(DataGridView_CellFormatting);
                    dataGridView1.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(DataGridViewPreview_RowPostPaint);
                    dataGridView1.CellValueNeeded += new DataGridViewCellValueEventHandler(DataGridView1_CellValueNeededPreview);
                }

                if (!previewMode)
                {
                    dataGridView1.CellValueNeeded -= DataGridView1_CellValueNeededPreview;
                    dataGridView1.CellValueNeeded += new DataGridViewCellValueEventHandler(dataGridView1_CellValueNeeded);
                    dataGridView1.Rows.Clear();
                    dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(DataGridView1_CellClick);
                    dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(DataGridView1_CellDoubleClick);

                    dataGridView1.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(DataGridView1_CellToolTipTextNeeded);
                    dataGridView1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(DataGridView_ColumnHeaderMouseClick);
                    dataGridView1.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(DataGridView_ColumnHeaderMouseDoubleClick);
                    dataGridView1.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(DataGridView_ColumnWidthChanged);


                    dataGridView1.RowPostPaint -= DataGridViewPreview_RowPostPaint;
                    dataGridView1.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(DataGridView_RowPostPaint);

                    dataGridView1.SelectionChanged += new System.EventHandler(DataGridView1_SelectionChanged);
                    dataGridView1.DragDrop += new System.Windows.Forms.DragEventHandler(DataGridView1_DragDrop);
                    dataGridView1.DragOver += new System.Windows.Forms.DragEventHandler(DataGridView1_DragOver);
                    dataGridView1.MouseDown += new System.Windows.Forms.MouseEventHandler(DataGridView_MouseDown);
                    dataGridView1.MouseLeave += new System.EventHandler(DataGridView1_MouseLeave);
                    dataGridView1.MouseMove += new System.Windows.Forms.MouseEventHandler(DataGridView_MouseMove);
                    dataGridView1.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(DataGridView_CellPainting);

                    _source.DataSource = CurrentDataTable;

                    dataGridView1.RowCount = 0;
                    //dataGridView1.RowCount = dataTable.Rows.Count;
                    //dataGridView1.RowCount = DataList.Count;
                    _workingRowsList = new List<object[]>();
                    foreach (var item in RowsList)
                    {
                        _workingRowsList.Add(item);
                    }
                    lbCnt.Text = WorkingRowsList.Count.ToString("N0");
                    // Apply metrics while RowCount is still 0 so DPI/layout work
                    // never walks tens of thousands of virtual rows.
                    ApplyDpiMetrics();
                    dataGridView1.RowCount = WorkingRowsList.Count;
                }

                DgvPaintStart();
                dataGridView1?.Refresh();
            });

            ls.Add($"{st.ElapsedMilliseconds} - all tasks");

            if (_workingRowsList == RowsList)
            {
                _workingRowsList = new List<object[]>();
                foreach (var item in RowsList)
                {
                    _workingRowsList.Add(item);
                }
            }
        }

        private DataView? getDataView()
        {
            if (_source.Current == null)
            {
                return null;
            }
            return (_source.Current as DataRowView)?.DataView;
        }

        private void dataGridView1_CellValueNeeded(object? sender, System.Windows.Forms.DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= WorkingRowsList.Count || e.ColumnIndex >= CurrentDataTable.Columns.Count)
            {
                return;
            }

            var a = WorkingRowsList[e.RowIndex][e.ColumnIndex];
            if (_isBoolean.ContainsKey(e.ColumnIndex))
            {
                if (a is null || a == DBNull.Value)
                {
                    e.Value = System.Windows.Forms.CheckState.Indeterminate;
                }
                else
                {
                    e.Value = a;
                }
            }
            else
            {
                e.Value = a;
            }
        }

        private void DataGridView1_CellValueNeededPreview(object? sender, System.Windows.Forms.DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= WorkingRowsList.Count || e.ColumnIndex >= CurrentDataTable.Columns.Count)
            {
                return;
            }

            var a = WorkingRowsList[e.RowIndex][e.ColumnIndex];
            if (_isBoolean.ContainsKey(e.ColumnIndex))
            {
                if (a is null || a == DBNull.Value)
                {
                    e.Value = System.Windows.Forms.CheckState.Indeterminate;
                }
                else
                {
                    e.Value = a;
                }
            }
            else
            {
                e.Value = a;
            }
        }

        private DataTable _dataTable;
        public DataTable CurrentDataTable
        {
            get
            {
                return _dataTable;
            }
            set
            {
                for (int i = 0; i < value.Columns.Count; i++)
                {
                    if (value.Columns[i].ColumnName.Contains(','))
                    {
                        value.Columns[i].ColumnName = value.Columns[i].ColumnName.Replace(",", "_COMMA_");
                    }
                }

                _dataTable = value;
            }
        }


        private List<object[]> _originalDataList;

        public List<object[]> RowsList
        {
            get => _originalDataList;
            set => _originalDataList = value;
        }

        private List<object[]> _workingRowsList;
        public List<object[]> WorkingRowsList => _workingRowsList;

        private readonly List<(int index, SortInfo sortInfo)> sortInfoList = new List<(int, SortInfo)>();

        /// <summary>
        /// column index -> filter
        /// </summary>
        private readonly Dictionary<int, (object? filterValue, FilterType filterType)> standardFilterDict = new();

        private DataGridViewFilter? _dataGridViewFilter;

        private List<object[]> FilterWorkingList(string fullText, bool addRootGroupRowsOnly = false)
        {
            if (_dataGridViewFilter == null)
            {
                _dataGridViewFilter = new DataGridViewFilter(CurrentDataTable, RowsList);
            }

            return _dataGridViewFilter.Filter(fullText, cbAprox.Checked, standardFilterDict, addRootGroupRowsOnly, _groupByRows, _groupingLvlIndex, _groupByColumnNums);
        }

        private bool IsSortedBy(int columnIndex)
        {
            foreach (var (index, _) in sortInfoList)
            {
                if (index == columnIndex)
                {
                    return true;
                }
            }
            return false;
        }
        private SortInfo GetSortInfo(int columnIndex)
        {
            foreach (var (index, sortInfo) in sortInfoList)
            {
                if (index == columnIndex)
                {
                    return sortInfo;
                }
            }
            return SortInfo.NONE;
        }
        private int GetSortIndex(int columnIndex)
        {
            int i = 0;
            foreach (var (index, sortInfo) in sortInfoList)
            {
                if (index == columnIndex)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
        private void SortRows(List<object[]> rows)
        {
            rows.Sort(new SortedRowsComparer(sortInfoList));
        }

        private void AddToSortInfo(int columnIndex, SortInfo sortInfo)
        {
            int index = GetSortIndex(columnIndex);
            if (index == -1)
            {
                sortInfoList.Add((columnIndex, sortInfo));
            }
            else
            {
                sortInfoList[index] = (columnIndex, sortInfo);
            }
        }


        private BindingSource _source = new BindingSource();

        private const int RowPaddingLogical = 5;
        private const int HeaderExtraLogical = 3;
        private const int ToolbarHeightLogical = 30;

        public int GrifOffsetHeight { get; set; }

        public void ApplyDpiMetrics()
        {
            int dpi = DeviceDpi;
            int toolbarHeight = Math.Max(
                ScaleDpi(ToolbarHeightLogical, dpi),
                (int)Math.Ceiling(dataGridView1.Font.GetHeight()) + ScaleDpi(6, dpi));
            int rowPadding = ScaleDpi(RowPaddingLogical, dpi);
            int headerExtra = ScaleDpi(HeaderExtraLogical, dpi);

            GrifOffsetHeight = rowPadding;
            groupPanel.Height = toolbarHeight;
            dataGridView1.Location = new Point(0, toolbarHeight);
            dataGridView1.ColumnHeadersDefaultCellStyle.Padding = new Padding(
                ScaleDpi(3, dpi), ScaleDpi(2, dpi), HeaderRightPadding + ScaleDpi(2, dpi), ScaleDpi(2, dpi));

            int rowHeight = GetRowHeight(dataGridView1.Font, rowPadding);
            ApplyGridRowMetrics(dataGridView1, rowHeight, dpi, headerExtra);
            ApplyGroupingDropMetrics(dgvDrop, dpi);
            ApplyGridRowMetrics(dgvSummaries, rowHeight, dpi, headerExtra);

            foreach (Control control in groupPanel.Controls)
            {
                control.Font = dataGridView1.Font;
            }

            ApplyGroupingColumnWidths();
            LayoutGroupPanel(dpi);
            ApplyColumnWidthsForDpi();
        }

        private void LayoutGroupPanel(int dpi)
        {
            int gap = ScaleDpi(2, dpi);
            int barHeight = groupPanel.Height;
            int buttonSize = Math.Max(ScaleDpi(22, dpi), barHeight - gap * 2);
            int y = Math.Max(0, (barHeight - buttonSize) / 2);
            int x = 0;

            tbSearch.Height = buttonSize;
            tbSearch.Width = ScaleDpi(150, dpi);
            tbSearch.Location = new Point(x, y);
            x = tbSearch.Right + gap;

            cbAprox.Size = new Size(buttonSize, buttonSize);
            cbAprox.Location = new Point(x, y);
            x = cbAprox.Right + gap;

            foreach (Button button in new[] { btCopyAsExcel, btCopyAsText, btOpenInExcel, btDownload, btRowView })
            {
                button.Size = new Size(buttonSize, buttonSize);
                button.Location = new Point(x, y);
                button.BackgroundImageLayout = ImageLayout.None;
                x = button.Right + gap;
            }

            lbCnt.AutoSize = true;
            lbCnt.Location = new Point(x + gap, y + Math.Max(0, (buttonSize - lbCnt.Height) / 2));

            cbJumpToColumn.Height = buttonSize;
            cbJumpToColumn.Width = ScaleDpi(154, dpi);
            cbJumpToColumn.Location = new Point(Math.Max(x + gap, groupPanel.Width - cbJumpToColumn.Width), y);
            cbJumpToColumn.IntegralHeight = false;

            dgvDrop.Height = barHeight;
            dgvDrop.Location = new Point(Math.Max(lbCnt.Right + ScaleDpi(8, dpi), ScaleDpi(360, dpi)), 0);
            dgvLabel.Location = new Point(dgvDrop.Left + ScaleDpi(16, dpi), y + Math.Max(0, (buttonSize - dgvLabel.Height) / 2));
        }

        private void ToolbarButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            int dpi = DeviceDpi;
            bool darkTheme = IsDark();
            Color iconColor = !button.Enabled
                ? darkTheme ? Color.FromArgb(108, 118, 132) : Color.FromArgb(155, 163, 173)
                : darkTheme ? Color.FromArgb(222, 229, 238) : Color.FromArgb(69, 80, 94);
            float penWidth = Math.Max(1.5f, dpi / 96f);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            switch (button.Name)
            {
                case "btCopyAsExcel":
                    DrawToolbarCopyGlyph(e.Graphics, button.ClientRectangle, dpi, iconColor, penWidth);
                    break;
                case "btCopyAsText":
                    DrawToolbarClipboardGlyph(e.Graphics, button.ClientRectangle, dpi, iconColor, penWidth);
                    break;
                case "btOpenInExcel":
                    DrawToolbarExcelGlyph(e.Graphics, button.ClientRectangle, dpi, penWidth, darkTheme);
                    break;
                case "btDownload":
                    DrawToolbarDownloadGlyph(e.Graphics, button.ClientRectangle, dpi, iconColor, penWidth);
                    break;
                case "btRowView":
                    DrawToolbarRowViewGlyph(e.Graphics, button.ClientRectangle, dpi, iconColor, penWidth);
                    break;
            }
        }

        private static void DrawToolbarRowViewGlyph(Graphics graphics, Rectangle bounds, int dpi, Color color, float penWidth)
        {
            int pad = ScaleDpi(7, dpi);
            int width = Math.Max(ScaleDpi(12, dpi), bounds.Width - pad * 2);
            int height = Math.Max(ScaleDpi(10, dpi), bounds.Height - pad * 2);
            int left = bounds.Left + (bounds.Width - width) / 2;
            int top = bounds.Top + (bounds.Height - height) / 2;
            int split = left + width / 3;

            using var pen = new Pen(color, penWidth);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawRectangle(pen, left, top, width, height);
            graphics.DrawLine(pen, split, top, split, top + height);
            for (int row = 1; row <= 2; row++)
            {
                int y = top + row * height / 3;
                graphics.DrawLine(pen, left, y, split, y);
            }
        }

        private static void DrawToolbarCopyGlyph(Graphics graphics, Rectangle bounds, int dpi, Color color, float penWidth)
        {
            int size = Math.Max(ScaleDpi(14, dpi), Math.Min(bounds.Width, bounds.Height) - ScaleDpi(8, dpi));
            Rectangle circle = new Rectangle(
                bounds.Left + (bounds.Width - size) / 2,
                bounds.Top + (bounds.Height - size) / 2,
                size,
                size);
            int centerX = circle.Left + circle.Width / 2;
            int centerY = circle.Top + circle.Height / 2;
            int arm = Math.Max(ScaleDpi(3, dpi), size / 5);

            using var pen = new Pen(color, penWidth);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawEllipse(pen, circle);
            graphics.DrawLine(pen, centerX, centerY - arm, centerX, centerY + arm);
            graphics.DrawLine(pen, centerX - arm, centerY + arm - ScaleDpi(2, dpi), centerX, centerY + arm);
            graphics.DrawLine(pen, centerX + arm, centerY + arm - ScaleDpi(2, dpi), centerX, centerY + arm);
        }

        private static void DrawToolbarClipboardGlyph(Graphics graphics, Rectangle bounds, int dpi, Color color, float penWidth)
        {
            int iconWidth = Math.Max(ScaleDpi(12, dpi), bounds.Width / 2);
            int iconHeight = Math.Max(ScaleDpi(16, dpi), bounds.Height - ScaleDpi(9, dpi));
            Rectangle clipboard = new Rectangle(
                bounds.Left + (bounds.Width - iconWidth) / 2,
                bounds.Top + (bounds.Height - iconHeight) / 2 + ScaleDpi(2, dpi),
                iconWidth,
                iconHeight);
            int clipWidth = Math.Max(ScaleDpi(8, dpi), iconWidth - ScaleDpi(8, dpi));
            Rectangle clip = new Rectangle(
                clipboard.Left + (clipboard.Width - clipWidth) / 2,
                clipboard.Top - ScaleDpi(3, dpi),
                clipWidth,
                ScaleDpi(5, dpi));

            using var path = CreateRoundedRectanglePath(clipboard, ScaleDpi(2, dpi));
            using var clipPath = CreateRoundedRectanglePath(clip, ScaleDpi(2, dpi));
            using var pen = new Pen(color, penWidth);
            graphics.DrawPath(pen, path);
            graphics.DrawPath(pen, clipPath);
            graphics.DrawLine(pen,
                clipboard.Left + ScaleDpi(4, dpi),
                clipboard.Top + ScaleDpi(6, dpi),
                clipboard.Right - ScaleDpi(4, dpi),
                clipboard.Top + ScaleDpi(6, dpi));
            graphics.DrawLine(pen,
                clipboard.Left + ScaleDpi(4, dpi),
                clipboard.Top + ScaleDpi(10, dpi),
                clipboard.Right - ScaleDpi(4, dpi),
                clipboard.Top + ScaleDpi(10, dpi));
        }

        private static void DrawToolbarExcelGlyph(Graphics graphics, Rectangle bounds, int dpi, float penWidth, bool darkTheme)
        {
            int size = Math.Max(ScaleDpi(14, dpi), Math.Min(bounds.Width, bounds.Height) - ScaleDpi(8, dpi));
            Rectangle sheet = new Rectangle(
                bounds.Left + (bounds.Width - size) / 2,
                bounds.Top + (bounds.Height - size) / 2,
                size,
                size);
            Color green = darkTheme ? Color.FromArgb(45, 145, 91) : Color.FromArgb(33, 115, 70);
            Color paper = darkTheme ? Color.FromArgb(232, 241, 235) : Color.White;

            using var sheetPath = CreateRoundedRectanglePath(sheet, ScaleDpi(2, dpi));
            using var sheetBrush = new SolidBrush(green);
            using var foldBrush = new SolidBrush(Color.FromArgb(180, paper));
            graphics.FillPath(sheetBrush, sheetPath);
            graphics.FillPolygon(foldBrush, new[]
            {
                new Point(sheet.Right - ScaleDpi(5, dpi), sheet.Top),
                new Point(sheet.Right, sheet.Top + ScaleDpi(5, dpi)),
                new Point(sheet.Right - ScaleDpi(5, dpi), sheet.Top + ScaleDpi(5, dpi))
            });

            int arm = Math.Max(ScaleDpi(3, dpi), size / 5);
            int centerX = sheet.Left + sheet.Width / 2;
            int centerY = sheet.Top + sheet.Height / 2;
            using var xPen = new Pen(paper, Math.Max(2f, penWidth + ScaleDpi(1, dpi)));
            xPen.StartCap = LineCap.Round;
            xPen.EndCap = LineCap.Round;
            graphics.DrawLine(xPen, centerX - arm, centerY - arm, centerX + arm, centerY + arm);
            graphics.DrawLine(xPen, centerX + arm, centerY - arm, centerX - arm, centerY + arm);
        }

        private static void DrawToolbarDownloadGlyph(Graphics graphics, Rectangle bounds, int dpi, Color color, float penWidth)
        {
            int centerX = bounds.Left + bounds.Width / 2;
            int top = bounds.Top + ScaleDpi(6, dpi);
            int bottom = bounds.Bottom - ScaleDpi(8, dpi);
            int arm = Math.Max(ScaleDpi(3, dpi), bounds.Width / 5);

            using var pen = new Pen(color, penWidth);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawLine(pen, centerX, top, centerX, bottom - ScaleDpi(4, dpi));
            graphics.DrawLine(pen, centerX - arm, bottom - ScaleDpi(7, dpi), centerX, bottom - ScaleDpi(3, dpi));
            graphics.DrawLine(pen, centerX + arm, bottom - ScaleDpi(7, dpi), centerX, bottom - ScaleDpi(3, dpi));
            graphics.DrawLine(pen, centerX - ScaleDpi(8, dpi), bottom, centerX + ScaleDpi(8, dpi), bottom);
            graphics.DrawLine(pen, centerX - ScaleDpi(8, dpi), bottom, centerX - ScaleDpi(8, dpi), bottom - ScaleDpi(4, dpi));
            graphics.DrawLine(pen, centerX + ScaleDpi(8, dpi), bottom, centerX + ScaleDpi(8, dpi), bottom - ScaleDpi(4, dpi));
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDpiMetrics();
        }

        private static int ScaleDpi(int logicalPixels, int dpi) =>
            (int)Math.Round(logicalPixels * dpi / 96f);

        private static int GetRowHeight(Font font, int rowPadding) =>
            (int)Math.Ceiling(font.GetHeight()) + rowPadding;

        private static void ApplyGridRowMetrics(DataGridView grid, int rowHeight, int dpi, int headerExtra = 0)
        {
            int cellPadX = ScaleDpi(3, dpi);
            int cellPadY = ScaleDpi(2, dpi);

            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = rowHeight;
            grid.ColumnHeadersHeight = rowHeight + headerExtra;
            grid.RowHeadersWidth = ScaleDpi(50, dpi);
            grid.DefaultCellStyle.Padding = new Padding(cellPadX, cellPadY, cellPadX, cellPadY);

            // VirtualMode shares row objects. Touching Rows[i].Height unshares every
            // row and freezes/flickers the UI for large result sets. RowTemplate is
            // enough; only patch tiny non-virtual grids (e.g. summaries).
            if (VirtualGridRowMetricsPolicy.ShouldAssignIndividualRowHeights(grid.VirtualMode, grid.Rows.Count))
            {
                for (int i = 0; i < grid.Rows.Count; i++)
                {
                    grid.Rows[i].Height = rowHeight;
                }
            }
        }

        private void ApplyGroupingDropMetrics(DataGridView grid, int dpi)
        {
            int headerHeight = Math.Max(ScaleDpi(22, dpi), groupPanel.Height - ScaleDpi(2, dpi));
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = headerHeight;
            grid.RowTemplate.Height = ScaleDpi(1, dpi);
        }

        private void ApplyGroupingColumnWidths()
        {
            int dpi = DeviceDpi;
            int extraWidth = ScaleDpi(50, dpi);
            int minimumWidth = ScaleDpi(90, dpi);

            foreach (DataGridViewColumn column in dgvDrop.Columns)
            {
                int textWidth = TextRenderer.MeasureText(column.HeaderText, dgvDrop.Font).Width;
                column.Width = Math.Max(minimumWidth, textWidth + extraWidth);
            }
        }

        private int GetGroupRowHeight(int dpi)
        {
            int dataRowHeight = GetRowHeight(dataGridView1.Font, ScaleDpi(RowPaddingLogical, dpi));
            int glyphSize = GetGroupGlyphSize(dpi);
            int groupTextHeight = (int)Math.Ceiling(_groupsFont.GetHeight(dpi));
            int groupRowHeight = Math.Max(groupTextHeight, glyphSize) + ScaleDpi(8, dpi);
            return Math.Max(dataRowHeight, groupRowHeight);
        }

        private static int GetGroupGlyphSize(int dpi) => ScaleDpi(16, dpi);

        private void DataGridView1_RowHeightInfoNeeded(object? sender, DataGridViewRowHeightInfoNeededEventArgs e)
        {
            if (_groupingLvlIndex < 0 || e.RowIndex < 0 || WorkingRowsList is null || e.RowIndex >= WorkingRowsList.Count)
            {
                return;
            }

            int lvl = (int?)WorkingRowsList[e.RowIndex][_groupingLvlIndex] ?? 0;
            if (lvl > 0)
            {
                e.Height = GetGroupRowHeight(DeviceDpi);
                e.MinimumHeight = e.Height;
            }
        }

        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string DecimalFormat { get; set; } = "G";

        private string[]? _decimalFormats;

        private string getDecimalFormatFor(int i)
        {
            if (_decimalFormats != null && i < _decimalFormats.Length && !String.IsNullOrWhiteSpace(_decimalFormats[i]))
            {
                return _decimalFormats[i];
            }
            return DecimalFormat;
        }

        public string IntegerFormat { get; set; } = "G";
        public bool ForceDecimalFormat { get; set; }
        private static readonly NumberFormatInfo _numberWithDot = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; }

        FastColoredTextBox FctbX { get; set; }
        public string AttachedSQL { get; set; } = string.Empty;

        public DataGridView InnerDataGridView
        {
            get { return dataGridView1; }
        }
        public void ClearDataGridView()
        {
            Thread.Sleep(5);
            Summaries.Clear();
            RowsList.Clear();
            WorkingRowsList.Clear();

            filterForms.Clear();
            // BindingSource.Clear() delegates to the underlying DataView, which is
            // intentionally read-only and throws "Cannot clear this list". Detach
            // the view instead; the next data load assigns a fresh DataTable.
            _source.DataSource = null;
        }

        public const int TechColsNum = 5;
        private const string _groupingLvl = "GroupingLVL_092734";
        private const string _isVisibleName = "isVisible_092734";
        private const string _isTechColName = "techCol_092734";
        private const string _groupCountName = "groupCount_092734";
        private const string _groupInfoName = "groupInfo_092734";

        private int _groupingLvlIndex = -1;
        private int _isVisibleNameIndex = -1;
        private int _isTechColNameIndex = -1;
        private int _groupCountNameIndex = -1;
        private int _groupInfoNameIndex = -1;

        private static int GetPrecision(decimal d)
        {
            decimal y = Math.Abs(d);
            decimal x = Math.Round(y);
            return (y - x).ToString().Length - 2;
        }

        private void AddGroupColumns()
        {
            if (_groupingLvlIndex != -1)
            {
                return;
            }
            _groupingLvlIndex = CurrentDataTable.Columns.Count + 0;
            _isVisibleNameIndex = CurrentDataTable.Columns.Count + 1;
            _isTechColNameIndex = CurrentDataTable.Columns.Count + 2;
            _groupCountNameIndex = CurrentDataTable.Columns.Count + 3;
            _groupInfoNameIndex = CurrentDataTable.Columns.Count + 4;


            System.Data.DataColumn newColumn = new System.Data.DataColumn(_groupingLvl, typeof(int));
            newColumn.DefaultValue = 0;
            CurrentDataTable.Columns.Add(newColumn);

            newColumn = new System.Data.DataColumn(_isVisibleName, typeof(bool));
            newColumn.DefaultValue = false;
            CurrentDataTable.Columns.Add(newColumn);

            newColumn = new System.Data.DataColumn(_isTechColName, typeof(bool));
            newColumn.DefaultValue = false;
            CurrentDataTable.Columns.Add(newColumn);

            newColumn = new System.Data.DataColumn(_groupCountName, typeof(long));
            newColumn.DefaultValue = -1;
            CurrentDataTable.Columns.Add(newColumn);

            newColumn = new System.Data.DataColumn(_groupInfoName, typeof(string));
            newColumn.DefaultValue = "";
            CurrentDataTable.Columns.Add(newColumn);
        }

        private void RemoveGroupColumns()
        {
            CurrentDataTable.Columns.Remove(_groupingLvl);
            CurrentDataTable.Columns.Remove(_isVisibleName);
            CurrentDataTable.Columns.Remove(_isTechColName);
            CurrentDataTable.Columns.Remove(_groupCountName);
            CurrentDataTable.Columns.Remove(_groupInfoName);

            _groupingLvlIndex = -1;
            _isVisibleNameIndex = -1;
            _isTechColNameIndex = -1;
            _groupCountNameIndex = -1;
            _groupInfoNameIndex = -1;
        }

        private string BasicSortForGroupedData()
        {
            List<string> ls = new List<string>();
            sortInfoList.Clear();
            for (int i = 0; i < _groupByColumnNums.Count; i++)
            {
                ls.Add($"{CurrentDataTable.Columns[_groupByColumnNums[i]].ColumnName} asc");
                AddToSortInfo(_groupByColumnNums[i], SortInfo.ASC);
            }
            AddToSortInfo(_groupingLvlIndex, SortInfo.DESC);
            return String.Join(",", ls) + $", {_groupingLvl} desc";
        }

        readonly List<int> _groupByColumnNums = new List<int>();
        readonly List<object[]> _groupByRows = new List<object[]>();
        private void GroupBy()
        {
            _groupByRows.Clear();
            if (_groupByColumnNums.Count == 0)
            {
                return;
            }
            //dataGridView1.DataSource = null;
            AddGroupColumns();//add tech columns

            //if (source.Count == 0)
            if (WorkingRowsList.Count == 0)
            {
                return;
            }
            //var dataView = (source.CurrencyManager.Current as DataRowView).DataView;

            _ = BasicSortForGroupedData();
            //source.Sort = sort;
            SortRows(WorkingRowsList);
            // to do SORT !!!

            if (_groupByColumnNums.Count >= 1)
            {
                for (int cntX = 0; cntX < _groupByColumnNums.Count; cntX++)
                {
                    int cnt = cntX + 1;
                    string sep = " and ";
                    Dictionary<string, int> val2 = new Dictionary<string, int>(); // kol1 = a and kol2 = b and .. = ID of grouping ROW, for double click purouses
                    Dictionary<string, int> valFirst = new Dictionary<string, int>(); // first row of id grouping (for check values in orginal data type)
                    string[] p1 = new string[cnt];

                    //for (int i = 0; i < dataView.Count; i++)
                    for (int i = 0; i < WorkingRowsList.Count; i++)
                    {
                        //if ((bool)dataView[i][isTechColName] == true)
                        if ((bool?)WorkingRowsList[i][_isTechColNameIndex] == true)
                        {
                            continue;
                        }

                        for (int j = 0; j < cnt; j++)
                        {
                            Type tp = CurrentDataTable.Columns[_groupByColumnNums[j]].DataType;
                            object valX = WorkingRowsList[i][_groupByColumnNums[j]];
                            if (valX is DBNull || valX is null)
                            {
                                p1[j] = $"[{CurrentDataTable.Columns[_groupByColumnNums[j]].ColumnName}] is null";
                            }
                            else
                            {
                                p1[j] = $"[{CurrentDataTable.Columns[_groupByColumnNums[j]].ColumnName}] = {GetGoodValue(tp, WorkingRowsList[i][_groupByColumnNums[j]])}";
                            }
                        }
                        string groupFilter = String.Join(sep, p1);
                        if (!val2.ContainsKey(groupFilter))
                        {
                            val2[groupFilter] = 1;
                            valFirst[groupFilter] = i; // sorting needed ? 
                        }
                        else
                        {
                            val2[groupFilter]++;
                        }
                    }
                    var keys2 = val2.Keys.ToArray();

                    for (int i = 0; i < val2.Count; i++)
                    {
                        int rowNm = valFirst[keys2[i]];
                        var dr = (object[])WorkingRowsList[rowNm].Clone();
                        dr[_isVisibleNameIndex] = true;
                        dr[_groupingLvlIndex] = _groupByColumnNums.Count - cnt + 1;
                        dr[_isTechColNameIndex] = true;
                        dr[_groupCountNameIndex] = val2[keys2[i]];
                        dr[_groupInfoNameIndex] = $"{keys2[i]}";
                        _groupByRows.Add(dr);
                    }
                }
            }

            return;
        }

        async Task ClearGroupingSorting()
        {
            var clearGroupingCommand = new ClearGroupingSortingCommand(
                _groupByColumnNums,
                _source,
                sortInfoList,
                _isTechColNameIndex,
                RemoveGroupColumns,
                _expandedGroups,
                InnerDataGridView,
                (text) => FilterWorkingList(text),
                (rows) => _workingRowsList = rows,
                tbSearch);
            await clearGroupingCommand.ExecuteAsync();
        }

        private async void ReloadOrginalRows()
        {
            var reloadCommand = new ReloadDataCommand(WorkingRowsList, RowsList);
            await reloadCommand.ExecuteAsync();
        }

        public async void ClearFilters()
        {
            var clearCommand = new ClearFiltersCommand(
                dataGridView1,
                standardFilterDict,
                tbSearch,
                ReloadOrginalRows,
                lbCnt,
                () => WorkingRowsList);
            await clearCommand.ExecuteAsync();
        }

        /// <summary>
        /// clears grouping, and filter accepts techcols
        /// </summary>
        private async Task DoProperGroupBy()
        {
            if (dgvDrop.Columns.Count == 0)
            {
                return;
            }

            ApplyGroupingColumnWidths();
            await ClearGroupingSorting();

            _groupByColumnNums.Clear();

            for (int i = 0; i < dgvDrop.Columns.Count; i++) // initialize group by list
            {
                int nr = -1;
                for (int j = 0; j < dgvDrop.Columns.Count; j++)
                {
                    if (dgvDrop.Columns[j].DisplayIndex == i)
                    {
                        nr = j;
                        break;
                    }
                }
                if (nr < 0)
                {
                    continue;
                }
                string columnName = dgvDrop.Columns[nr].Name;
                if (CurrentDataTable.Columns[columnName] is DataColumn dataColumn)
                {
                    _groupByColumnNums.Add(dataColumn.Ordinal);
                }
            }
            if (_groupByColumnNums.Count > 0)
            {
                tbSearch.Enabled = false;
            }

            GroupBy();

            dataGridView1.RowCount = 0;

            _workingRowsList = FilterWorkingList(tbSearch.Text, addRootGroupRowsOnly: true);

            SortRows(WorkingRowsList);

            dataGridView1.RowCount = WorkingRowsList.Count;
            dataGridView1.Invalidate();
            ResetSummaries();
        }

        private void ResetSummaries()
        {
            List<(int, string)> sumCopy = new List<(int, string)>();
            _agrDataDic.Clear();
            foreach (var item in Summaries)
            {
                sumCopy.Add(item);
            }
            foreach (var item in sumCopy)
            {
                AddAgr(item.Item1, item.Item2);// = "stop"
                AddAgr(item.Item1, item.Item2); // = "start"
            }
            sumCopy.Clear();
        }

        private async Task SearchInDataGridView(string columnName, object? filterValue, FilterType filterType, bool over = false)
        {
            try
            {
                if (_groupByColumnNums.Count == 0) // grouping off
                {
                    try
                    {

                        dataGridView1.RowCount = 0;
                        if (columnName is not null &&
                            (filterValue is not null
                                || filterValue is null && filterType == FilterType.isNull
                                || filterValue is null && filterType == FilterType.isNotNull
                            ))
                        {
                            int index = CurrentDataTable.Columns.IndexOf(columnName);
                            if (index == -1)
                            {
                                throw new Exception("column nor found (filter)");
                            }

                            standardFilterDict[index] = (filterValue, filterType);
                            _workingRowsList = FilterWorkingList(tbSearch.Text);
                        }
                        lbCnt.Text = WorkingRowsList.Count.ToString("N0");
                        dataGridView1.RowCount = WorkingRowsList.Count;
                        dataGridView1.Invalidate();
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Applying grid filter failed: {exception.GetType().Name}");
                    }
                }
                else // grouping on
                {
                    await DoProperGroupBy();
                }
                ResetSummaries();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ClearFilter(int columnIndex)
        {
            standardFilterDict.Remove(columnIndex);
            dataGridView1.RowCount = 0;
            //tbSearch.Text = "";
            _workingRowsList = FilterWorkingList(tbSearch.Text);
            lbCnt.Text = WorkingRowsList.Count.ToString("N0");
            dataGridView1.RowCount = WorkingRowsList.Count;
            dataGridView1.Invalidate();
        }


        //https://stackoverflow.com/questions/21131157/drag-and-drop-cell-from-datagridview-to-another/21133200

        private void GroupPanel_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void DgvDrop_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private async void GroupPanel_DragDrop(object? sender, DragEventArgs e)
        {
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            //Point clientPoint = myDataGridView1.groupPanel.PointToClient(new Point(e.X, e.Y));
            // If the drag operation was a copy then add the row to the other control.
            dgvLabel.Visible = false;
            if (e.Effect == DragDropEffects.Copy)
            {
                DragData? cellvalueSet = e.Data?.GetData(typeof(DragData)) as DragData;

                if (cellvalueSet is null && _draggedSpecial is not null)
                {
                    cellvalueSet = _draggedSpecial;
                }

                if (cellvalueSet == null)
                    return;

                if (!dgvDrop.Columns.Contains(cellvalueSet.Cellvalue))
                {
                    int addedColumnIndex = dgvDrop.Columns.Add(cellvalueSet.Cellvalue, cellvalueSet.Cellvalue);
                    dgvDrop.Columns[addedColumnIndex].SortMode = DataGridViewColumnSortMode.NotSortable;
                    await DoProperGroupBy();
                }
            }
        }

        private async void DgvDrop_DragDrop(object? sender, DragEventArgs e)
        {
            dgvLabel.Visible = false;
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            Point clientPoint = dgvDrop.PointToClient(new Point(e.X, e.Y));
            var hittest = dgvDrop.HitTest(clientPoint.X, clientPoint.Y);
            // If the drag operation was a copy then add the row to the other control.
            if (e.Effect == DragDropEffects.Copy)
            {
                var cellvalueSet = e.Data?.GetData(typeof(DragData)) as DragData;

                if (cellvalueSet is null && _draggedSpecial is not null)
                {
                    cellvalueSet = _draggedSpecial;
                }

                if (cellvalueSet == null)
                    return;

                if (!dgvDrop.Columns.Contains(cellvalueSet.Cellvalue))
                {
                    if (hittest.ColumnIndex != -1)
                    {
                        dgvDrop.Columns.Insert(hittest.ColumnIndex, new DataGridViewColumn() { Name = cellvalueSet.Cellvalue, HeaderText = cellvalueSet.Cellvalue });
                    }
                    else
                    {
                        int addedColumnIndex = dgvDrop.Columns.Add(cellvalueSet.Cellvalue, cellvalueSet.Cellvalue);
                        dgvDrop.Columns[addedColumnIndex].SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                    await DoProperGroupBy();
                }
                else if (hittest.ColumnIndex != -1)// change order
                {
                    DataGridViewColumn? draggedColumn = dgvDrop.Columns[cellvalueSet.Cellvalue];
                    if (draggedColumn is not null
                        && draggedColumn.DisplayIndex != dgvDrop.Columns[hittest.ColumnIndex].DisplayIndex)
                    {
                        draggedColumn.DisplayIndex = dgvDrop.Columns[hittest.ColumnIndex].DisplayIndex;
                        await DoProperGroupBy();
                    }
                }
            }
        }

        private Rectangle _dragBoxFromMouseDown;
        private string _columnDraggedName = string.Empty;
        private string _columnDraggedNameSourceName = string.Empty;

        private void DataGridView_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (_dragBoxFromMouseDown != Rectangle.Empty && !_dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    // Proceed with the drag and drop, passing in the list item.
                    _draggedSpecial = new DragData(_columnDraggedName, _columnDraggedNameSourceName);
                    DragDropEffects dropEffect = dataGridView1.DoDragDrop(_draggedSpecial, DragDropEffects.Copy);
                }
                else
                {
                    _draggedSpecial = null;
                }
            }
            else if (e.Button == MouseButtons.None)
            {
                Point p = e.Location;
                if (sender is not DataGridView dgv)
                {
                    return;
                }

                int colFilterNum = -1;
                int colPinNum = -1;
                int aggNum = -1;
                int n = dgv.DisplayedColumnCount(false);
                for (int i = 0; i < n; i++)
                {
                    var rec = dgv.GetCellDisplayRectangle(i, -1, false);
                    GridHeaderZones zones = GetHeaderZones(rec, DeviceDpi);

                    if (colFilterNum == -1 && zones.FilterHit.Contains(p))
                    {
                        colFilterNum = i;
                    }
                    else if (colPinNum == -1 && zones.PinHit.Contains(p))
                    {
                        colPinNum = i;
                    }
                    else if (aggNum == -1 && zones.AggregateHit.Contains(p))
                    {
                        aggNum = i;
                    }

                    bool r1 = false;
                    if (MouseOverFilter.ContainsKey(i) && MouseOverFilter[i] == true && !zones.FilterHit.Contains(p))
                    {
                        MouseOverFilter[i] = false;
                        r1 = true;
                    }
                    if (MouseOverPin.ContainsKey(i) && MouseOverPin[i] == true && !zones.PinHit.Contains(p))
                    {
                        MouseOverPin[i] = false;
                        r1 = true;
                    }
                    if (MouseOverAggregate.ContainsKey(i) && MouseOverAggregate[i] == true && !zones.AggregateHit.Contains(p))
                    {
                        MouseOverAggregate[i] = false;
                        r1 = true;
                    }
                    if (r1)
                    {
                        dgv.Invalidate(rec);
                    }

                }

                if (colFilterNum != -1 && (MouseOverFilter.ContainsKey(colFilterNum) && MouseOverFilter[colFilterNum] == false || !MouseOverFilter.ContainsKey(colFilterNum)))
                {
                    MouseOverFilter[colFilterNum] = true;
                    dgv.Invalidate(dgv.GetCellDisplayRectangle(colFilterNum, -1, false));
                }
                else if (colPinNum != -1 && (MouseOverPin.ContainsKey(colPinNum) && MouseOverPin[colPinNum] == false || !MouseOverPin.ContainsKey(colPinNum)))
                {
                    MouseOverPin[colPinNum] = true;
                    dgv.Invalidate(dgv.GetCellDisplayRectangle(colPinNum, -1, false));
                }
                else if (aggNum != -1 && (MouseOverAggregate.ContainsKey(aggNum) && MouseOverAggregate[aggNum] == false || !MouseOverAggregate.ContainsKey(aggNum)))
                {
                    MouseOverAggregate[aggNum] = true;
                    dgv.Invalidate(dgv.GetCellDisplayRectangle(aggNum, -1, false));
                }
            }
        }

        private DragData? _draggedSpecial;
        public void HideFilters()
        {
            foreach (var cnt in dataGridView1.Controls)
            {
                if (cnt is FilterForm list)
                {
                    list.Visible = false;
                }
            }
        }

        public event MouseEventHandler? DataGridMouseDown;
        private void DataGridView_MouseDown(object? sender, MouseEventArgs e)
        {
            DataGridMouseDown?.Invoke(sender, e);
            HideFilters();

            if (e.Button == MouseButtons.Left)
            {
                foreach (var item in filterForms.Values)
                {
                    item.Hide();
                }

                // Get the index of the item the mouse is below.
                var hittestInfo = dataGridView1.HitTest(e.X, e.Y);

                if (hittestInfo.RowIndex == -1 && hittestInfo.ColumnIndex != -1 && Cursor.Current != Cursors.SizeWE)
                {
                    _columnDraggedName = dataGridView1.Columns[hittestInfo.ColumnIndex].Name;
                    _columnDraggedNameSourceName = "data";

                    if (_columnDraggedName != null)
                    {
                        // Remember the point where the mouse down occurred. 
                        // The DragSize indicates the size that the mouse can move 
                        // before a drag event should be started.                
                        Size dragSize = SystemInformation.DragSize;
                        // Create a rectangle using the DragSize, with the mouse position being
                        // at the center of the rectangle.
                        _dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
                    }
                }
                else
                {
                    _dragBoxFromMouseDown = Rectangle.Empty;
                }
            }
        }

        private Color _nullForeColor = Color.FromArgb(105, 105, 105);
        private Color _nullBackColor = Color.FromArgb(255, 255, 224);

        private void DataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is null || e.Value == DBNull.Value)
            {
                e.CellStyle.BackColor = _nullBackColor;
                e.CellStyle.ForeColor = _nullForeColor;
            }
        }

        public Color GroupBackgroundActiveStart { get; set; }
        public Color GroupBackgroundActiveMiddle { get; set; }
        public Color GroupBackgroundActiveEnd { get; set; }

        public Color GroupBackgroundStart { get; set; }
        public Color GroupBackgroundMiddle { get; set; }
        public Color GroupBackgroundEnd { get; set; }

        private readonly Font _groupsFont = new Font(DataGridView.DefaultFont, FontStyle.Bold);
        private readonly Font _groupsFontSymbol = new Font(DataGridView.DefaultFont.FontFamily, DataGridView.DefaultFont.Size + 3);
        private readonly Font _groupsFontAggregates = new Font(DataGridView.DefaultFont.FontFamily, DataGridView.DefaultFont.Size + 6, FontStyle.Bold);
        private readonly Font _groupsFontActiveAggregates = new Font(DataGridView.DefaultFont.FontFamily, DataGridView.DefaultFont.Size + 6, FontStyle.Bold | FontStyle.Underline);
        //readonly Stopwatch _stx = new Stopwatch();

        private void DataGridViewPreview_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dataGridView1.RowHeadersWidth, e.RowBounds.Height);
            var b = new SolidBrush(dataGridView1.DefaultCellStyle.ForeColor);
            e.Graphics.DrawString((e.RowIndex + 1).ToString(), dataGridView1.Font, b, headerBounds, centerFormat);
        }


        private Brush GroupFontBrush { get; set; } = SystemBrushes.ControlText;
        private void DataGridView_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            //if (e.RowIndex >= source.Count)
            if (e.RowIndex >= WorkingRowsList.Count)
                return;

            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };

            //if (dataTable.Columns.Contains(groupingLvl))
            if (_groupingLvlIndex != -1)
            {
                //var dataView = getDataView();
                var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dataGridView1.RowHeadersWidth, e.RowBounds.Height);
                //int lvl = (int)dataView[e.RowIndex][groupingLvl];
                int lvl = (int?)WorkingRowsList[e.RowIndex][_groupingLvlIndex] ?? 0;
                if (lvl > 0)
                {
                    int lvlX = (_groupByColumnNums.Count - lvl);

                    char[] spaceOffset = new char[(lvlX + 1) * 8];
                    for (int i = 0; i < (lvlX + 1) * 8; i++)
                    {
                        spaceOffset[i] = ' ';
                    }
                    string offset = new string(spaceOffset);

                    if (e.State.HasFlag(DataGridViewElementStates.Selected))//dgv[e.ColumnIndex, e.RowIndex].Selected
                    {
                        LinearGradientBrush br = new LinearGradientBrush(e.RowBounds, GroupBackgroundActiveStart, GroupBackgroundActiveEnd, 0, true);
                        ColorBlend cb = new ColorBlend();
                        cb.Positions = new[] { 0, (float)0.5, 1 };
                        cb.Colors = new[] { GroupBackgroundActiveStart, GroupBackgroundActiveMiddle, GroupBackgroundActiveEnd };
                        br.InterpolationColors = cb;

                        e.Graphics.FillRectangle(br, e.RowBounds);
                        e.Graphics.DrawString(null, dataGridView1.DefaultCellStyle.Font ?? dataGridView1.Font, GroupFontBrush, e.RowBounds);

                    }
                    else
                    {
                        LinearGradientBrush br = new LinearGradientBrush(e.RowBounds, GroupBackgroundStart, GroupBackgroundEnd, 0, true);
                        ColorBlend cb = new ColorBlend();
                        cb.Positions = new[] { 0, (float)0.5, 1 };
                        cb.Colors = new[] { GroupBackgroundStart, GroupBackgroundMiddle, GroupBackgroundEnd };
                        br.InterpolationColors = cb;

                        //var rec = e.RowBounds;
                        //rec.Offset(lvlX * 10, 0);
                        //e.Graphics.FillRectangle(br, rec);
                        e.Graphics.FillRectangle(br, e.RowBounds);
                        e.Graphics.DrawString(null, dataGridView1.DefaultCellStyle.Font ?? dataGridView1.Font, GroupFontBrush, e.RowBounds);

                    }
                    string currentGroupName = Convert.ToString(WorkingRowsList[e.RowIndex][_groupInfoNameIndex]) ?? string.Empty;
                    
                    string addInfo = "";
                    if (_agrDataDic.ContainsKey(currentGroupName))
                    {
                        if (_agrDataDic[currentGroupName] == null)
                        {
                            addInfo = $"| null";
                        }
                        else
                        {
                            addInfo = $"| {_agrDataDic[currentGroupName]?.ToString("N3")}";
                        }
                    }

                    e.Graphics.DrawString($"{offset} {currentGroupName}|count: {((int)WorkingRowsList[e.RowIndex][_groupCountNameIndex]).ToString("N0")}{addInfo}", _groupsFont, GroupFontBrush, e.RowBounds);
                    var rec = e.RowBounds;
                    int dpi = DeviceDpi;
                    rec.Offset(ScaleDpi(lvlX * 2, dpi), 0);

                    // Manually draw the expand/collapse glyph
                    int glyphSize = GetGroupGlyphSize(dpi);
                    int glyphPadding = ScaleDpi(4, dpi);
                    Rectangle glyphRect = new Rectangle(
                        rec.Left + glyphPadding,
                        rec.Top + (rec.Height - glyphSize) / 2,
                        glyphSize,
                        glyphSize);
                    float glyphPenWidth = Math.Max(1f, dpi / 96f);
                    using var glyphBorderPen = new Pen(Color.Gray, glyphPenWidth);
                    using var glyphSignPen = new Pen(Color.Black, glyphPenWidth);
                    e.Graphics.DrawRectangle(glyphBorderPen, glyphRect);
                    
                    // Draw Plus/Minus sign
                    int glyphCenterX = glyphRect.X + glyphRect.Width / 2;
                    int glyphCenterY = glyphRect.Y + glyphRect.Height / 2;
                    int glyphArm = ScaleDpi(4, dpi);
                    e.Graphics.DrawLine(glyphSignPen, glyphCenterX - glyphArm, glyphCenterY, glyphCenterX + glyphArm, glyphCenterY); // Horizontal line
                    
                    if (!_expandedGroups.Contains((currentGroupName, lvl)))
                    {
                        e.Graphics.DrawLine(glyphSignPen, glyphCenterX, glyphCenterY - glyphArm, glyphCenterX, glyphCenterY + glyphArm); // Vertical line for '+'
                    }
                    
                    e.Graphics.DrawRectangle(Pens.White, e.RowBounds);
                }
                else
                {
                    //e.Graphics.DrawString(null, groupsFont, SystemBrushes.ControlText, headerBounds, centerFormat);
                }
            }
            else
            {
                var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dataGridView1.RowHeadersWidth, e.RowBounds.Height);
                var b = new SolidBrush(dataGridView1.DefaultCellStyle.ForeColor);
                e.Graphics.DrawString((e.RowIndex + 1).ToString(), dataGridView1.Font, b, headerBounds, centerFormat);
            }
        }

        private int ScaleHdr(int value) => ScaleDpi(value, DeviceDpi);

        private readonly struct GridHeaderZones
        {
            public Rectangle PinHit { get; init; }
            public Rectangle FilterHit { get; init; }
            public Rectangle AggregateHit { get; init; }
            public Rectangle SortHit { get; init; }
            public Rectangle TextRect { get; init; }
            public int IconCenterY { get; init; }
        }

        private GridHeaderZones GetHeaderZones(Rectangle cell, int dpi)
        {
            int gap = ScaleDpi(3, dpi);
            int pinW = ScaleDpi(14, dpi);
            int filterW = ScaleDpi(16, dpi);
            int aggW = ScaleDpi(14, dpi);
            int sortW = ScaleDpi(12, dpi);
            int textPadX = ScaleDpi(4, dpi);

            int right = cell.Right;
            int top = cell.Top;
            int height = cell.Height;

            var pinHit = new Rectangle(right - pinW - gap, top, pinW, height);
            var filterHit = new Rectangle(pinHit.Left - gap - filterW, top, filterW, height);
            var aggHit = new Rectangle(filterHit.Left - gap - aggW, top, aggW, height);
            var sortHit = new Rectangle(aggHit.Left - gap - sortW, top, sortW, height);
            var textRect = new Rectangle(
                cell.X + textPadX,
                top,
                Math.Max(0, sortHit.Left - cell.X - textPadX - gap),
                height);

            return new GridHeaderZones
            {
                PinHit = pinHit,
                FilterHit = filterHit,
                AggregateHit = aggHit,
                SortHit = sortHit,
                TextRect = textRect,
                IconCenterY = top + height / 2
            };
        }

        private int HeaderRightPadding => GetHeaderChromeWidth(DeviceDpi);

        private static float HeaderPenWidth(int dpi) => Math.Max(1f, dpi / 96f);

        private Color PaintHeaderIconButton(
            Graphics g,
            Rectangle zone,
            int dpi,
            bool active,
            bool hovered,
            bool enabled,
            bool darkTheme)
        {
            Rectangle button = zone;
            button.Inflate(-ScaleDpi(1, dpi), -ScaleDpi(3, dpi));
            Color accent = Color.FromArgb(86, 156, 214);
            Color buttonBack = !enabled
                ? darkTheme ? Color.FromArgb(49, 53, 61) : Color.FromArgb(239, 241, 244)
                : hovered
                    ? darkTheme ? Color.FromArgb(58, 82, 108) : Color.FromArgb(232, 241, 250)
                    : active
                        ? darkTheme ? Color.FromArgb(55, 94, 132) : Color.FromArgb(224, 238, 250)
                        : darkTheme ? Color.FromArgb(57, 63, 73) : Color.FromArgb(242, 245, 248);
            Color border = !enabled
                ? darkTheme ? Color.FromArgb(71, 76, 86) : Color.FromArgb(220, 224, 230)
                : hovered || active
                    ? accent
                    : darkTheme ? Color.FromArgb(92, 102, 118) : Color.FromArgb(211, 218, 227);
            Color icon = !enabled
                ? darkTheme ? Color.FromArgb(112, 120, 132) : Color.FromArgb(154, 162, 172)
                : hovered || active
                    ? accent
                    : darkTheme ? Color.FromArgb(220, 226, 234) : Color.FromArgb(82, 91, 104);

            using var buttonPath = CreateRoundedRectanglePath(button, ScaleDpi(4, dpi));
            using var buttonBrush = new SolidBrush(buttonBack);
            using var buttonPen = new Pen(border, HeaderPenWidth(dpi));
            g.FillPath(buttonBrush, buttonPath);
            g.DrawPath(buttonPen, buttonPath);
            return icon;
        }

        private void DrawHeaderPlus(Graphics g, Rectangle zone, int dpi, bool active, bool hovered, bool darkTheme)
        {
            Color icon = PaintHeaderIconButton(g, zone, dpi, active, hovered, true, darkTheme);
            int centerX = zone.Left + zone.Width / 2;
            int centerY = zone.Top + zone.Height / 2;
            int arm = ScaleDpi(3, dpi);
            using Pen iconPen = new Pen(icon, HeaderPenWidth(dpi) + (active || hovered ? 0.25f : 0f));
            iconPen.StartCap = LineCap.Round;
            iconPen.EndCap = LineCap.Round;
            g.DrawLine(iconPen, centerX - arm, centerY, centerX + arm, centerY);
            g.DrawLine(iconPen, centerX, centerY - arm, centerX, centerY + arm);
        }

        private void DrawHeaderFilter(Graphics g, Rectangle zone, int dpi, bool active, bool hovered, bool enabled, bool darkTheme)
        {
            Color icon = PaintHeaderIconButton(g, zone, dpi, active, hovered, enabled, darkTheme);
            int centerX = zone.Left + zone.Width / 2;
            int centerY = zone.Top + zone.Height / 2;
            int filterW = ScaleDpi(9, dpi);
            int filterH = ScaleDpi(8, dpi);
            int filterX = centerX - filterW / 2;
            int filterY = centerY - filterH / 2;

            Point[] filterPoints =
            {
                new Point(filterX, filterY),
                new Point(filterX + filterW, filterY),
                new Point(filterX + ScaleDpi(6, dpi), filterY + ScaleDpi(4, dpi)),
                new Point(filterX + ScaleDpi(6, dpi), filterY + filterH),
                new Point(filterX + ScaleDpi(3, dpi), filterY + filterH),
                new Point(filterX + ScaleDpi(3, dpi), filterY + ScaleDpi(4, dpi))
            };

            using Pen iconPen = new Pen(icon, HeaderPenWidth(dpi));
            using Brush iconBrush = new SolidBrush(icon);
            if (active || hovered)
            {
                g.FillPolygon(iconBrush, filterPoints);
            }
            else
            {
                g.DrawPolygon(iconPen, filterPoints);
            }
        }

        private void DrawHeaderPin(Graphics g, Rectangle zone, int dpi, bool frozen, bool hovered, bool darkTheme)
        {
            Color icon = PaintHeaderIconButton(g, zone, dpi, frozen, hovered, true, darkTheme);
            int centerX = zone.Left + zone.Width / 2;
            int centerY = zone.Top + zone.Height / 2;
            int headW = ScaleDpi(7, dpi);
            int headH = ScaleDpi(5, dpi);
            int headY = centerY - ScaleDpi(6, dpi);
            Rectangle head = new Rectangle(centerX - headW / 2, headY, headW, headH);

            using var headPath = CreateRoundedRectanglePath(head, ScaleDpi(2, dpi));
            using var iconBrush = new SolidBrush(icon);
            using var iconPen = new Pen(icon, HeaderPenWidth(dpi));
            g.FillPath(iconBrush, headPath);
            g.DrawLine(iconPen,
                centerX - ScaleDpi(5, dpi),
                centerY,
                centerX + ScaleDpi(5, dpi),
                centerY);
            g.DrawLine(iconPen,
                centerX,
                centerY,
                centerX,
                centerY + ScaleDpi(7, dpi));
            if (frozen || hovered)
            {
                g.DrawLine(iconPen,
                    centerX - ScaleDpi(3, dpi),
                    centerY + ScaleDpi(7, dpi),
                    centerX + ScaleDpi(3, dpi),
                    centerY + ScaleDpi(7, dpi));
            }
        }

        private void DrawHeaderSort(Graphics g, Rectangle zone, int dpi, bool ascending, bool darkTheme)
        {
            Color icon = PaintHeaderIconButton(g, zone, dpi, true, false, true, darkTheme);
            int centerX = zone.Left + zone.Width / 2;
            int centerY = zone.Top + zone.Height / 2;
            int half = ScaleDpi(4, dpi);
            Point[] arrow = ascending
                ? new[]
                {
                    new Point(centerX, centerY - half),
                    new Point(centerX - half, centerY + half),
                    new Point(centerX + half, centerY + half)
                }
                : new[]
                {
                    new Point(centerX, centerY + half),
                    new Point(centerX - half, centerY - half),
                    new Point(centerX + half, centerY - half)
                };

            using var arrowBrush = new SolidBrush(icon);
            g.FillPolygon(arrowBrush, arrow);
        }

        private Color _forSmothColor;
        private Pen _forSmothPen = SystemPens.ControlText;
        private Brush _forSmothBrush = SystemBrushes.ControlText;

        bool IsActiveFilter(int colNum)
        {
            if (standardFilterDict.ContainsKey(colNum))
            {
                return true;
            }
            return false;
        }

        private List<(int, string)> Summaries = new List<(int, string)>();
        private Dictionary<int, FilterForm> filterForms = new Dictionary<int, FilterForm>();
        private readonly Dictionary<int, bool> MouseOverFilter = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> MouseOverPin = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> MouseOverAggregate = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> AggregatePossibleDic = new Dictionary<int, bool>();
        private int _groupRemoveHoverColumn = -1;

        private static Rectangle GetGroupingTileBounds(Rectangle cellBounds, int dpi)
        {
            int inset = ScaleDpi(4, dpi);
            cellBounds.Inflate(-inset, -inset);
            return cellBounds;
        }

        private static Rectangle GetGroupingRemoveButtonBounds(Rectangle cellBounds, int dpi)
        {
            Rectangle tileBounds = GetGroupingTileBounds(cellBounds, dpi);
            int buttonSize = Math.Min(
                ScaleDpi(20, dpi),
                Math.Max(ScaleDpi(14, dpi), tileBounds.Height - ScaleDpi(6, dpi)));
            int rightMargin = ScaleDpi(5, dpi);

            return new Rectangle(
                tileBounds.Right - rightMargin - buttonSize,
                tileBounds.Top + (tileBounds.Height - buttonSize) / 2,
                buttonSize,
                buttonSize);
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

        private void DgvDrop_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0)
            {
                return;
            }

            e.Handled = true;
            int dpi = DeviceDpi;
            bool darkTheme = IsDark();
            Rectangle tileBounds = GetGroupingTileBounds(e.CellBounds, dpi);
            Rectangle removeBounds = GetGroupingRemoveButtonBounds(e.CellBounds, dpi);
            if (e.Graphics is not Graphics g)
            {
                return;
            }

            Color panelBack = groupPanel.BackColor;
            Color tileBack = darkTheme ? Color.FromArgb(48, 53, 62) : Color.FromArgb(250, 251, 253);
            Color tileBorder = darkTheme ? Color.FromArgb(98, 108, 124) : Color.FromArgb(207, 214, 224);
            Color titleColor = darkTheme ? Color.FromArgb(238, 241, 245) : Color.FromArgb(38, 43, 51);
            Color gripColor = darkTheme ? Color.FromArgb(145, 155, 170) : Color.FromArgb(145, 153, 165);
            bool removeHovered = _groupRemoveHoverColumn == e.ColumnIndex;
            Color removeBack = removeHovered
                ? Color.FromArgb(198, 76, 82)
                : darkTheme ? Color.FromArgb(78, 88, 103) : Color.FromArgb(232, 236, 242);
            Color removeBorder = removeHovered
                ? Color.FromArgb(224, 113, 117)
                : darkTheme ? Color.FromArgb(123, 135, 153) : Color.FromArgb(198, 206, 218);
            Color removeFore = removeHovered
                ? Color.White
                : darkTheme ? Color.FromArgb(225, 231, 239) : Color.FromArgb(82, 91, 104);

            using var panelBrush = new SolidBrush(panelBack);
            using var tileBrush = new SolidBrush(tileBack);
            using var tileBorderPen = new Pen(tileBorder, HeaderPenWidth(dpi));
            using var tilePath = CreateRoundedRectanglePath(tileBounds, ScaleDpi(7, dpi));
            g.FillRectangle(panelBrush, e.CellBounds);
            g.FillPath(tileBrush, tilePath);
            g.DrawPath(tileBorderPen, tilePath);

            int centerY = tileBounds.Top + tileBounds.Height / 2;
            int gripDotSize = Math.Max(2, ScaleDpi(2, dpi));
            int gripX = tileBounds.Left + ScaleDpi(9, dpi);
            using (var gripBrush = new SolidBrush(gripColor))
            {
                for (int i = -1; i <= 1; i++)
                {
                    g.FillEllipse(
                        gripBrush,
                        gripX,
                        centerY + i * ScaleDpi(5, dpi) - gripDotSize / 2,
                        gripDotSize,
                        gripDotSize);
                }
            }

            int textLeft = tileBounds.Left + ScaleDpi(19, dpi);
            int textRight = removeBounds.Left - ScaleDpi(7, dpi);
            Rectangle textBounds = new Rectangle(
                textLeft,
                tileBounds.Top,
                Math.Max(0, textRight - textLeft),
                tileBounds.Height);
            using (var titleFont = new Font(dgvDrop.Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    e.FormattedValue?.ToString() ?? string.Empty,
                    titleFont,
                    textBounds,
                    titleColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            using var removeBackBrush = new SolidBrush(removeBack);
            using var removeBorderPen = new Pen(removeBorder, HeaderPenWidth(dpi));
            g.FillEllipse(removeBackBrush, removeBounds);
            g.DrawEllipse(removeBorderPen, removeBounds);
            int removeCenterX = removeBounds.Left + removeBounds.Width / 2;
            int removeCenterY = removeBounds.Top + removeBounds.Height / 2;
            int removeArm = ScaleDpi(4, dpi);
            using var removeSignPen = new Pen(removeFore, Math.Max(1.25f, HeaderPenWidth(dpi)));
            removeSignPen.StartCap = LineCap.Round;
            removeSignPen.EndCap = LineCap.Round;
            g.DrawLine(removeSignPen,
                removeCenterX - removeArm,
                removeCenterY - removeArm,
                removeCenterX + removeArm,
                removeCenterY + removeArm);
            g.DrawLine(removeSignPen,
                removeCenterX + removeArm,
                removeCenterY - removeArm,
                removeCenterX - removeArm,
                removeCenterY + removeArm);
        }

        private void DgvSummaries_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView grid || e.Graphics is not Graphics graphics
                || e.RowIndex != -1 || e.ColumnIndex < 0)
            {
                return;
            }

            e.Handled = true;
            int dpi = DeviceDpi;
            bool darkTheme = IsDark();
            Color baseBack = grid.BackgroundColor;
            if (baseBack.IsEmpty)
            {
                baseBack = grid.DefaultCellStyle.BackColor;
            }

            Color gridLine = grid.GridColor.IsEmpty
                ? darkTheme ? Color.FromArgb(66, 73, 84) : Color.FromArgb(220, 225, 232)
                : grid.GridColor;
            using var baseBrush = new SolidBrush(baseBack);
            using var gridPen = new Pen(gridLine, HeaderPenWidth(dpi));
            graphics.FillRectangle(baseBrush, e.CellBounds);
            graphics.DrawLine(gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

            string title = e.FormattedValue?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            Rectangle chipBounds = e.CellBounds;
            chipBounds.Inflate(-ScaleDpi(4, dpi), -ScaleDpi(3, dpi));
            Color accent = GetSummaryAccent(title, darkTheme);
            Color chipBack = darkTheme
                ? Color.FromArgb(47, 59, 74)
                : Color.FromArgb(239, 245, 252);
            Color chipBorder = darkTheme
                ? Color.FromArgb(89, 111, 139)
                : Color.FromArgb(190, 208, 230);
            Color textColor = darkTheme ? Color.FromArgb(232, 239, 248) : Color.FromArgb(42, 53, 67);

            using var chipPath = CreateRoundedRectanglePath(chipBounds, ScaleDpi(5, dpi));
            using var chipBrush = new SolidBrush(chipBack);
            using var chipPen = new Pen(chipBorder, HeaderPenWidth(dpi));
            graphics.FillPath(chipBrush, chipPath);
            graphics.DrawPath(chipPen, chipPath);

            Rectangle accentBounds = new Rectangle(
                chipBounds.Left + ScaleDpi(5, dpi),
                chipBounds.Top + ScaleDpi(4, dpi),
                ScaleDpi(3, dpi),
                Math.Max(ScaleDpi(7, dpi), chipBounds.Height - ScaleDpi(8, dpi)));
            using var accentBrush = new SolidBrush(accent);
            graphics.FillRectangle(accentBrush, accentBounds);

            Rectangle textBounds = chipBounds;
            textBounds.X += ScaleDpi(12, dpi);
            textBounds.Width = Math.Max(0, textBounds.Width - ScaleDpi(15, dpi));
            using var summaryFont = new Font(grid.Font, FontStyle.Bold);
            TextRenderer.DrawText(
                graphics,
                title,
                summaryFont,
                textBounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private static Color GetSummaryAccent(string title, bool darkTheme)
        {
            Color light = title[0] switch
            {
                'S' => Color.FromArgb(70, 142, 220),
                'C' => Color.FromArgb(128, 105, 204),
                'D' => Color.FromArgb(40, 161, 145),
                'm' => Color.FromArgb(222, 145, 54),
                'M' => Color.FromArgb(205, 91, 104),
                'A' => Color.FromArgb(61, 157, 103),
                _ => Color.FromArgb(120, 135, 154)
            };

            return darkTheme
                ? ControlPaint.Light(light, .15f)
                : light;
        }

        private void PaintColumnHeaderCell(DataGridView dgv, DataGridViewCellPaintingEventArgs e)
        {
            int dpi = DeviceDpi;
            GridHeaderZones zones = GetHeaderZones(e.CellBounds, dpi);
            if (e.Graphics is not Graphics g)
            {
                return;
            }
            bool darkTheme = IsDark();
            bool selected = e.State.HasFlag(DataGridViewElementStates.Selected);
            Color headerBack = dgv.ColumnHeadersDefaultCellStyle.BackColor.IsEmpty
                ? dgv.DefaultCellStyle.BackColor
                : dgv.ColumnHeadersDefaultCellStyle.BackColor;
            if (headerBack.IsEmpty)
            {
                headerBack = dgv.BackgroundColor;
            }

            Color headerText = dgv.ColumnHeadersDefaultCellStyle.ForeColor.IsEmpty
                ? dgv.DefaultCellStyle.ForeColor
                : dgv.ColumnHeadersDefaultCellStyle.ForeColor;
            if (headerText.IsEmpty)
            {
                headerText = darkTheme ? Color.FromArgb(239, 242, 246) : Color.FromArgb(35, 40, 48);
            }

            Color headerBorder = selected
                ? Color.FromArgb(86, 156, 214)
                : dgv.GridColor.IsEmpty ? ControlPaint.Dark(headerBack) : dgv.GridColor;
            using var headerBackBrush = new SolidBrush(headerBack);
            using var headerBorderPen = new Pen(headerBorder, HeaderPenWidth(dpi));

            g.FillRectangle(headerBackBrush, e.CellBounds);
            g.DrawLine(headerBorderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            if (e.ColumnIndex < dgv.Columns.Count)
            {
                g.DrawLine(headerBorderPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
            }

            string title = e.FormattedValue?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(title))
            {
                Rectangle titleBounds = zones.TextRect;
                titleBounds.X = Math.Max(titleBounds.X, e.CellBounds.Left + ScaleDpi(7, dpi));
                titleBounds.Width = Math.Max(0, titleBounds.Right - titleBounds.X);
                using var titleFont = new Font(dgv.Font, FontStyle.Bold);
                TextRenderer.DrawText(
                    g,
                    title,
                    titleFont,
                    titleBounds,
                    headerText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            bool filterEnabled = tbSearch.Enabled;
            bool filterHovered = MouseOverFilter.TryGetValue(e.ColumnIndex, out bool isMouseOverFilter) && isMouseOverFilter;
            bool pinHovered = MouseOverPin.TryGetValue(e.ColumnIndex, out bool isPin) && isPin;
            bool aggregateHovered = MouseOverAggregate.TryGetValue(e.ColumnIndex, out bool isAggregate) && isAggregate;

            DrawHeaderFilter(
                g,
                zones.FilterHit,
                dpi,
                IsActiveFilter(e.ColumnIndex),
                filterHovered,
                filterEnabled,
                darkTheme);
            DrawHeaderPin(
                g,
                zones.PinHit,
                dpi,
                dgv.Columns[e.ColumnIndex].Frozen,
                pinHovered,
                darkTheme);

            bool hasSummary = Summaries.Contains((e.ColumnIndex, "SUM"))
                || Summaries.Contains((e.ColumnIndex, "COUNT"))
                || Summaries.Contains((e.ColumnIndex, "COUNT DISTINCT"))
                || Summaries.Contains((e.ColumnIndex, "MIN"))
                || Summaries.Contains((e.ColumnIndex, "MAX"))
                || Summaries.Contains((e.ColumnIndex, "AVG"));
            DrawHeaderPlus(g, zones.AggregateHit, dpi, hasSummary, aggregateHovered, darkTheme);

            if (!string.IsNullOrWhiteSpace(_source.Sort) && IsSortedBy(e.ColumnIndex))
            {
                DrawHeaderSort(
                    g,
                    zones.SortHit,
                    dpi,
                    GetSortInfo(e.ColumnIndex) == SortInfo.ASC,
                    darkTheme);
            }
        }

        private void DataGridView_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (WorkingRowsList is null || e.RowIndex >= WorkingRowsList.Count)
            {
                e.Handled = true;
                return;
            }

            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                if (sender is DataGridView grid)
                {
                    PaintColumnHeaderCell(grid, e);
                }
                e.Handled = true;
            }
            else if (_groupingLvlIndex != -1 && e.RowIndex != -1 && (int?)WorkingRowsList[e.RowIndex][_groupingLvlIndex] > 0)
            {
                dataGridView1.InvalidateRow(e.RowIndex);
                // Prevent default painting
                e.Handled = true;
                // Calculate the original cell position (compensating for horizontal scroll)
                Rectangle originalCellBounds  = e.CellBounds;
                originalCellBounds .X += dataGridView1.HorizontalScrollingOffset;

                // Only paint if the cell is visible within the grid bounds
                if (e.Graphics is not Graphics graphics || e.CellStyle is null)
                {
                    return;
                }
                using var cellBrush = new SolidBrush(e.CellStyle.BackColor);
                graphics.FillRectangle(cellBrush, originalCellBounds);

                if (e.Value != null)
                {
                    TextRenderer.DrawText(graphics, e.Value.ToString() ?? string.Empty,
                        e.CellStyle.Font ?? dataGridView1.Font, originalCellBounds, e.CellStyle.ForeColor);
                }
            }
        }

        private static bool AggregatePossible(DataTable dt, int cl)
        {
            if (dt == null || (dt.Columns[cl].DataType != typeof(decimal) && dt.Columns[cl].DataType != typeof(int) && dt.Columns[cl].DataType != typeof(long)) && dt.Columns[cl].DataType != typeof(double))
            {
                return false;
            }
            return true;
        }

        private bool IsDark() =>
            _colorTheme.IsDark(InnerDataGridView.DefaultCellStyle.BackColor);

        private void DataGridView_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            int dpi = DeviceDpi;
            Rectangle rect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            GridHeaderZones zones = GetHeaderZones(rect, DeviceDpi);
            Point clickPoint = new Point(rect.Left + e.X, rect.Top + e.Y);

            if (zones.FilterHit.Contains(clickPoint))
            {
                HideFilters();

                if (tbSearch.Enabled)
                {
                    if (filterForms == null)
                    {
                        filterForms = new Dictionary<int, FilterForm>();
                    }

                    List<object> lt = new();
                    //ltStart.AddRange(new string[] { "(All)", "(Blanks)", "(NonBlanks)" });

                    HashSet<object> d1 = new HashSet<object>();

                    //var dataView = getDataView();
                    //if (!String.IsNullOrWhiteSpace(source.Filter) && dataView != null)
                    if (WorkingRowsList != null)
                    {
                        //foreach (DataRowView item in dataView)
                        foreach (var item in WorkingRowsList)
                        {
                            if (item == null)
                            {
                                continue;
                            }
                            Type colType = CurrentDataTable.Columns[e.ColumnIndex].DataType;
                            //var raw = item.Row.ItemArray[e.ColumnIndex];
                            var raw = item[e.ColumnIndex];
                            if (raw is null || raw is DBNull)
                            {
                                continue;
                            }
                            d1.Add(raw);
                        }
                    }
                    filterForms[e.ColumnIndex] = new FilterForm(e.ColumnIndex, IsDark(), dataGridView1.DefaultCellStyle);
                    filterForms[e.ColumnIndex].OnSearch += SearchInDataGridView;
                    filterForms[e.ColumnIndex].OnClear += ClearFilter;
                    filterForms[e.ColumnIndex].Name = dataGridView1.Columns[e.ColumnIndex].Name;
                    filterForms[e.ColumnIndex].Tag = new FilterFormTag();
                    var keysList = d1.ToList();
                    keysList.Sort();
                    lt.AddRange(keysList);

                    filterForms[e.ColumnIndex].ValuesInFilter = lt;


                    FilterForm mybox = filterForms[e.ColumnIndex];
                    if (mybox.Tag is FilterFormTag filterTag)
                    {
                        filterTag.ColumnNumber = e.ColumnIndex;
                    }

                    if (dataGridView1?.Parent?.Parent?.Parent?.Parent?.Parent is SplitContainer splitContainer && splitContainer is not null
                        && splitContainer.Height - splitContainer.SplitterDistance < 270
                        )
                    {
                        splitContainer.SplitterDistance = splitContainer.Height - 270;
                    }

                    int filterWidth = ScaleDpi(240, dpi);
                    mybox.ApplyDpiMetrics(filterWidth, dpi);
                    mybox.Location = new Point(rect.X, rect.Y + rect.Height);
                    mybox.MinimumSize = new Size(filterWidth, InnerDataGridView.Height / 3);
                    mybox.MaximumSize = new Size(filterWidth, InnerDataGridView.Height - ScaleDpi(35, dpi));

                    if (!InnerDataGridView.Controls.Contains(mybox))
                    {
                        InnerDataGridView.Controls.Add(mybox);
                    }
                    mybox.Visible = true;
                }

            }
            else if (zones.PinHit.Contains(clickPoint))
            {
                FrozeAct(dataGridView1, e.ColumnIndex);
            }
            else if (zones.AggregateHit.Contains(clickPoint))
            {
                bool isNumber = AggregatePossible(CurrentDataTable, e.ColumnIndex);

                var sumForms = new SummariesChooseForm(IsDark(), dataGridView1.DefaultCellStyle);
                var p2 = dataGridView1.PointToScreen(dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false).Location);
                sumForms.Location = p2;
                if (!isNumber)
                {
                    sumForms.textMode();
                }

                int indx = -1;
                for (int i = 0; i < Summaries.Count; i++)
                {
                    if (Summaries[i].Item1 == e.ColumnIndex)
                    {
                        indx = i;
                    }
                }
                if (indx != -1)
                {
                    sumForms.chose(Summaries[indx].Item2);
                }


                var res = sumForms.ShowDialog();
                string summary;
                if (res == DialogResult.OK && sumForms.Choosed != null)
                {
                    summary = sumForms.Choosed;
                    _agrDataDic.Clear();
                    AddAgr(e.ColumnIndex, summary);
                }
                else if (res == DialogResult.OK && sumForms.Choosed == null)
                {
                    if (indx != -1)
                    {
                        _agrDataDic.Clear();
                        AddAgr(e.ColumnIndex, Summaries[indx].Item2);
                    }
                }

                try
                {
                    sumForms.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                string colName = $"[{dataGridView1.Columns[e.ColumnIndex].Name}]";
                var cleanedColumnForRegex = Regex.Escape(colName);
                if (Control.ModifierKeys != Keys.Control)
                {
                    List<string> lSort = new List<string>();
                    sortInfoList.Clear();
                    if (_groupByColumnNums.Count > 0)
                    {
                        lSort.Add(BasicSortForGroupedData());
                    }


                    if (_groupByColumnNums.Count > 0 && Regex.IsMatch(lSort[0], @$"(\W|^){cleanedColumnForRegex}(\W|$)"))
                    {
                        return;
                    }
                    else if (!String.IsNullOrWhiteSpace(_source.Sort) && Regex.IsMatch(_source.Sort, @$"(\W|^){cleanedColumnForRegex} ASC(\W|$)"))
                    {
                        lSort.Add($"{colName} DESC");
                        //sortInfoList.Add((e.ColumnIndex, SortInfo.DESC));
                        AddToSortInfo(e.ColumnIndex, SortInfo.DESC);
                    }
                    else
                    {
                        lSort.Add($"{colName} ASC");
                        AddToSortInfo(e.ColumnIndex, SortInfo.ASC);
                    }

                    SortRows(WorkingRowsList);
                    _source.Sort = String.Join(',', lSort);
                }
                else
                {
                    var tmlColName = $"[{dataGridView1.Columns[e.ColumnIndex].Name}]";
                    if (_groupByColumnNums.Count > 0 && Regex.IsMatch(BasicSortForGroupedData(), @$"(\W|^){cleanedColumnForRegex}(\W|$)"))
                    {
                        return;
                    }
                    else if (String.IsNullOrWhiteSpace(_source.Sort))
                    {
                        _source.Sort = $"{tmlColName} ASC";
                        AddToSortInfo(e.ColumnIndex, SortInfo.ASC);
                    }
                    else if (_source.Sort.Contains($"{tmlColName} DESC"))
                    {
                        _source.Sort = _source.Sort.Replace($"{tmlColName} desc", $"{tmlColName} ASC");
                        AddToSortInfo(e.ColumnIndex, SortInfo.DESC);
                    }
                    else if (_source.Sort.Contains($"{tmlColName} ASC"))
                    {
                        _source.Sort = _source.Sort.Replace($"{tmlColName} ASC", $"{tmlColName} DESC");
                        AddToSortInfo(e.ColumnIndex, SortInfo.DESC);
                    }
                    else
                    {
                        _source.Sort += $", {tmlColName} ASC";
                        AddToSortInfo(e.ColumnIndex, SortInfo.ASC);
                    }
                    SortRows(WorkingRowsList);
                }
                dataGridView1.Invalidate();
            }
            InnerDataGridView.Invalidate(rect);
        }

        readonly Dictionary<string, decimal?> _agrDataDic = new Dictionary<string, decimal?>();

        private void AddAgr(int cl, string agrType)
        {
            _agrDataDic.Clear();

            if (Summaries.Contains((cl, agrType)))
            {
                Summaries.Remove((cl, agrType));
                dgvSummaries.Columns[cl].HeaderText = "";
                dataGridView1.Refresh();
            }
            else
            {
                decimal? s = null;
                bool isGrouped = false;
                //var dataView = getDataView();
                //string oryginalFilter = source.Filter;

                //if (dataTable.Columns.Contains(groupingLvl))
                if (_groupingLvlIndex != -1)
                {
                    isGrouped = true;
                    //string tempFilter = "(" + Regex.Replace(oryginalFilter, @$"\b{groupingLvl}\s=\s\d+\b", "1=1") + $") and {groupingLvl} >=0";
                    DgvPaintStop();
                    //source.Filter = tempFilter;
                }

                //int cnt = source.Count;
                //var _filteredTemp = WorkingRowsList;//
                var _filteredTemp = FilterWorkingList(tbSearch.Text);
                _filteredTemp.AddRange(_groupByRows);
                SortRows(_filteredTemp);
                int cnt = _filteredTemp.Count;

                switch (agrType)
                {
                    case "SUM":
                        s = 0;
                        decimal tmpDec = (decimal)0.0;
                        Dictionary<int, int> lastGroupRowDic = [];
                        decimal? subTotal = 0;
                        int groupNum = -1;
                        if (isGrouped && cnt > 0)
                        {
                            groupNum = ((int?)_filteredTemp[0][_groupingLvlIndex] ?? 0) - 1;
                            lastGroupRowDic = new Dictionary<int, int>(groupNum);
                        }

                        for (int j = 0; j < cnt; j++)
                        {
                            //var currentRow = dataView[j];
                            var currentRow = _filteredTemp[j];
                            var currentCell = currentRow[cl];

                            if (isGrouped)
                            {
                                int currentGroupingLvl = (int?)currentRow[_groupingLvlIndex] ?? 0;//(int) currentRow[groupingLvl];

                                if (currentGroupingLvl > 0) // group row
                                {
                                    if (!lastGroupRowDic.ContainsKey(currentGroupingLvl))
                                    {
                                        lastGroupRowDic[currentGroupingLvl] = j;
                                        continue;
                                    }
                                    //agrDataDic[(string)dataView[lastGroupRowDic[1]][groupInfoName]] = subTotal;
                                    _agrDataDic[(string)_filteredTemp[lastGroupRowDic[1]][_groupInfoNameIndex]] = subTotal;
                                    for (int i = 1; i < lastGroupRowDic.Count; i++)
                                    {
                                        int t1 = lastGroupRowDic[i + 1];
                                        var tmpTxt = (string)_filteredTemp[t1][_groupInfoNameIndex];
                                        if (!_agrDataDic.ContainsKey(tmpTxt))
                                        {
                                            _agrDataDic[tmpTxt] = subTotal;
                                        }
                                        else
                                        {
                                            _agrDataDic[tmpTxt] += subTotal;
                                        }
                                    }

                                    subTotal = 0;
                                    for (int i = currentGroupingLvl; i > 0; i--)
                                    {
                                        lastGroupRowDic[i] = j;
                                    }

                                    continue;
                                }
                            }
                            if (!(currentCell is null || currentCell is DBNull))
                            {
                                tmpDec = Convert.ToDecimal(currentCell);
                                s += tmpDec;
                            }
                            else
                            {
                                tmpDec = 0;
                            }

                            if (isGrouped && lastGroupRowDic.Count > 0)
                            {
                                subTotal += tmpDec;
                            }
                        }
                        if (isGrouped && lastGroupRowDic.Count > 0)
                        {
                            for (int i = 0; i < lastGroupRowDic.Count; i++)
                            {
                                string h = (string)_filteredTemp[lastGroupRowDic[i + 1]][_groupInfoNameIndex];
                                if (!_agrDataDic.ContainsKey(h))
                                {
                                    _agrDataDic[h] = subTotal;
                                }
                                else
                                {
                                    _agrDataDic[h] += subTotal;
                                }
                            }
                        }

                        dgvSummaries.Columns[cl].HeaderText = "S: " + s?.ToString("N1");
                        break;
                    case "COUNT":
                        s = 0;
                        for (int j = 0; j < cnt; j++)
                        {
                            //var y = dataView[j];
                            var y = _filteredTemp[j];
                            var x = y[cl];

                            if (x is null || x is DBNull || isGrouped && ((int?)y[_groupingLvlIndex] ?? 0) > 0)
                            {
                                continue;
                            }
                            s++;
                        }

                        dgvSummaries.Columns[cl].HeaderText = "C: " + s?.ToString("N0");
                        break;
                    case "COUNT DISTINCT":
                        if (!isGrouped)
                        {
                            s = 0;
                            Dictionary<object, bool> ob = new Dictionary<object, bool>();
                            for (int j = 0; j < cnt; j++)
                            {
                                //var y = dataView[j];
                                var y = _filteredTemp[j];
                                var x = y[cl];
                                if (x is null || x is DBNull || isGrouped && ((int?)y[_groupingLvlIndex] ?? 0) > 0)
                                {
                                    continue;
                                }
                                if (!ob.ContainsKey(x))
                                {
                                    ob[x] = true;
                                    s++;
                                }
                            }

                            dgvSummaries.Columns[cl].HeaderText = "D: " + s?.ToString("N0");
                        }
                        else // isGrouped
                        {
                            Dictionary<object, bool> countTotal = new Dictionary<object, bool>();
                            Dictionary<int, Dictionary<object, bool>> dicOfTempDics = new Dictionary<int, Dictionary<object, bool>>();
                            Dictionary<int, bool> isUsed = new Dictionary<int, bool>(); //used info
                            Dictionary<int, string> groupNames = new Dictionary<int, string>();//group names

                            int maxGroupLevels = 0;

                            for (int j = 0; j < cnt; j++)
                            {
                                //var currentRow = dataView[j];
                                var currentRow = _filteredTemp[j];
                                var currentCell = currentRow[cl];

                                int currentGroupingLvl = (int?)currentRow[_groupingLvlIndex] ?? 0;//(int) currentRow[groupingLvl];

                                if (j == 0) // only first time
                                {
                                    maxGroupLevels = currentGroupingLvl;
                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        dicOfTempDics[i] = new Dictionary<object, bool>();
                                        isUsed[i] = false;
                                        groupNames[i] = string.Empty;
                                    }
                                }

                                if (currentGroupingLvl > 0) // group row
                                {
                                    if (isUsed[currentGroupingLvl] == true) // first time for this level
                                    {
                                        _agrDataDic[groupNames[currentGroupingLvl]] = dicOfTempDics[currentGroupingLvl].Keys.Count;
                                    }
                                    isUsed[currentGroupingLvl] = true;
                                    dicOfTempDics[currentGroupingLvl].Clear();
                                    groupNames[currentGroupingLvl] = (string)_filteredTemp[j][_groupInfoNameIndex];
                                }
                                else // non group row = all groups or "open"
                                {
                                    //var row = dataView[j];
                                    var row = _filteredTemp[j];
                                    var cell = row[cl];

                                    if (cell is null || cell is DBNull)
                                    {
                                        continue;
                                    }
                                    countTotal[cell] = true;

                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        dicOfTempDics[i][cell] = true;
                                    }
                                }
                            }

                            for (int i = 1; i <= maxGroupLevels; i++)
                            {
                                _agrDataDic[groupNames[i]] = dicOfTempDics[i].Keys.Count;
                            }

                            dgvSummaries.Columns[cl].HeaderText = "D: " + countTotal.Keys.Count.ToString("N0");
                        }
                        break;
                    case "MIN":
                        if (!isGrouped)
                        {
                            for (int j = 0; j < cnt; j++)
                            {
                                //var x = dataView[j][cl];
                                var y = _filteredTemp[j];
                                var x = y[cl];
                                if (x is null || x is DBNull || isGrouped && ((int?)y[_groupingLvlIndex] ?? 0) > 0)
                                {
                                    continue;
                                }
                                if (s == null || Convert.ToDecimal(x) < s)
                                {
                                    s = Convert.ToDecimal(x);
                                }
                            }

                            dgvSummaries.Columns[cl].HeaderText = "m: " + s?.ToString("N1");
                        }
                        else
                        {
                            decimal? totalMin = null;
                            Dictionary<int, decimal?> actualValues = new Dictionary<int, decimal?>();
                            Dictionary<int, bool> isUsed = new Dictionary<int, bool>(); //used info
                            Dictionary<int, string> groupNames = new Dictionary<int, string>();//group names

                            int maxGroupLevels = 0;

                            for (int j = 0; j < cnt; j++)
                            {
                                //var currentRow = dataView[j];
                                var currentRow = _filteredTemp[j];
                                var currentCell = currentRow[cl];

                                int currentGroupingLvl = (int?)currentRow[_groupingLvlIndex] ?? 0;//(int) currentRow[groupingLvl];

                                if (j == 0) // only first time
                                {
                                    maxGroupLevels = currentGroupingLvl;
                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        actualValues[i] = null;
                                        isUsed[i] = false;
                                        groupNames[i] = string.Empty;
                                    }
                                }

                                if (currentGroupingLvl > 0) // group row
                                {
                                    if (isUsed[currentGroupingLvl] == true) // first time for this level
                                    {
                                        _agrDataDic[groupNames[currentGroupingLvl]] = actualValues[currentGroupingLvl];
                                    }
                                    isUsed[currentGroupingLvl] = true;
                                    actualValues[currentGroupingLvl] = null;
                                    groupNames[currentGroupingLvl] = (string)_filteredTemp[j][_groupInfoNameIndex];
                                }
                                else // non group row = all groups or "open"
                                {
                                    //var row = dataView[j];
                                    var row = _filteredTemp[j];
                                    var cell = row[cl];

                                    if (cell is null || cell is DBNull)
                                    {
                                        continue;
                                    }
                                    var cellDec = Convert.ToDecimal(cell);
                                    if (totalMin == null || totalMin > cellDec)
                                    {
                                        totalMin = cellDec;
                                    }

                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        if (actualValues[i] == null || actualValues[i] > cellDec)
                                        {
                                            actualValues[i] = cellDec;
                                        }
                                    }
                                }
                            }

                            for (int i = 1; i <= maxGroupLevels; i++)
                            {
                                _agrDataDic[groupNames[i]] = actualValues[i];
                            }

                            dgvSummaries.Columns[cl].HeaderText = "m: " + totalMin?.ToString("N0");
                        }

                        break;
                    case "MAX":
                        if (!isGrouped)
                        {
                            for (int j = 0; j < cnt; j++)
                            {
                                var y = _filteredTemp[j];
                                var x = y[cl];

                                if (x is null || x is DBNull || isGrouped && ((int?)y[_groupingLvlIndex] ?? 0) > 0)
                                {
                                    continue;
                                }
                                if (s == null || Convert.ToDecimal(x) > s)
                                {
                                    s = Convert.ToDecimal(x);
                                }
                            }

                            dgvSummaries.Columns[cl].HeaderText = "M: " + s?.ToString("N1");
                        }
                        else
                        {
                            decimal? totalMin = null;
                            Dictionary<int, decimal?> actualValues = new Dictionary<int, decimal?>();
                            Dictionary<int, bool> isUsed = new Dictionary<int, bool>(); //used info
                            Dictionary<int, string> groupNames = new Dictionary<int, string>();//group names

                            int maxGroupLevels = 0;

                            for (int j = 0; j < cnt; j++)
                            {
                                //var currentRow = dataView[j];
                                var currentRow = _filteredTemp[j];
                                var currentCell = currentRow[cl];

                                int currentGroupingLvl = (int?)currentRow[_groupingLvlIndex] ?? 0;//(int) currentRow[groupingLvl];

                                if (j == 0) // only first time
                                {
                                    maxGroupLevels = currentGroupingLvl;
                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        actualValues[i] = null;
                                        isUsed[i] = false;
                                        groupNames[i] = string.Empty;
                                    }
                                }

                                if (currentGroupingLvl > 0) // group row
                                {
                                    if (isUsed[currentGroupingLvl] == true) // first time for this level
                                    {
                                        _agrDataDic[groupNames[currentGroupingLvl]] = actualValues[currentGroupingLvl];
                                    }
                                    isUsed[currentGroupingLvl] = true;
                                    actualValues[currentGroupingLvl] = null;
                                    groupNames[currentGroupingLvl] = (string)_filteredTemp[j][_groupInfoNameIndex];
                                }
                                else // non group row = all groups or "open"
                                {
                                    //var row = dataView[j];
                                    var row = _filteredTemp[j];
                                    var cell = row[cl];

                                    if (cell is null || cell is DBNull)
                                    {
                                        continue;
                                    }
                                    var cellDec = Convert.ToDecimal(cell);
                                    if (totalMin == null || totalMin < cellDec)
                                    {
                                        totalMin = cellDec;
                                    }

                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        if (actualValues[i] == null || actualValues[i] < cellDec)
                                        {
                                            actualValues[i] = cellDec;
                                        }
                                    }
                                }
                            }

                            for (int i = 1; i <= maxGroupLevels; i++)
                            {
                                _agrDataDic[groupNames[i]] = actualValues[i];
                            }

                            dgvSummaries.Columns[cl].HeaderText = "M: " + totalMin?.ToString("N0");
                        }

                        break;
                    case "AVG":
                        s = 0;
                        long l = 0;
                        if (!isGrouped)
                        {
                            for (int j = 0; j < cnt; j++)
                            {
                                //var x = dataView[j][cl];
                                var x = _filteredTemp[j][cl];
                                if (x is null || x is DBNull)
                                {
                                    continue;
                                }
                                s += Convert.ToDecimal(x);
                                l++;
                            }
                            if (l > 0)
                            {
                                dgvSummaries.Columns[cl].HeaderText = "A: " + ((decimal)s / l).ToString("N1");
                            }
                        }
                        else
                        {
                            decimal totalSum = 0;
                            long totalCnt = 0;
                            Dictionary<int, decimal> actualValues = new Dictionary<int, decimal>();
                            Dictionary<int, long> actualCounts = new Dictionary<int, long>();
                            Dictionary<int, bool> isUsed = new Dictionary<int, bool>(); //used info
                            Dictionary<int, string> groupNames = new Dictionary<int, string>();//group names

                            int maxGroupLevels = 0;

                            for (int j = 0; j < cnt; j++)
                            {
                                //var currentRow = dataView[j];
                                var currentRow = _filteredTemp[j];
                                var currentCell = currentRow[cl];
                                int currentGroupingLvl = (int?)currentRow[_groupingLvlIndex] ?? 0;//(int) currentRow[groupingLvl];

                                if (j == 0) // only first time
                                {
                                    maxGroupLevels = currentGroupingLvl;
                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        actualValues[i] = 0;
                                        actualCounts[i] = 0;
                                        isUsed[i] = false;
                                        groupNames[i] = string.Empty;
                                    }
                                }

                                if (currentGroupingLvl > 0) // group row
                                {
                                    if (isUsed[currentGroupingLvl] == true) // first time for this level
                                    {
                                        if (actualCounts[currentGroupingLvl] != 0)
                                        {
                                            _agrDataDic[groupNames[currentGroupingLvl]] = actualValues[currentGroupingLvl] / actualCounts[currentGroupingLvl];
                                        }
                                        else
                                        {
                                            _agrDataDic[groupNames[currentGroupingLvl]] = 0;
                                        }

                                    }
                                    isUsed[currentGroupingLvl] = true;
                                    actualValues[currentGroupingLvl] = 0;
                                    actualCounts[currentGroupingLvl] = 0;
                                    groupNames[currentGroupingLvl] = (string)_filteredTemp[j][_groupInfoNameIndex];
                                }
                                else // non group row = all groups or "open"
                                {
                                    //var row = dataView[j];
                                    var row = _filteredTemp[j];
                                    var cell = row[cl];

                                    if (cell is null || cell is DBNull)
                                    {
                                        continue;
                                    }
                                    var cellDec = Convert.ToDecimal(cell);
                                    totalSum += cellDec;
                                    totalCnt++;

                                    for (int i = 1; i <= maxGroupLevels; i++)
                                    {
                                        actualValues[i] += cellDec;
                                        actualCounts[i]++;
                                    }
                                }
                            }

                            for (int i = 1; i <= maxGroupLevels; i++)
                            {
                                if (actualCounts[i] != 0)
                                {
                                    _agrDataDic[groupNames[i]] = actualValues[i] / actualCounts[i];
                                }
                                else
                                {
                                    _agrDataDic[groupNames[i]] = 0;
                                }
                            }

                            if (totalCnt != 0)
                            {
                                dgvSummaries.Columns[cl].HeaderText = "A: " + (totalSum / totalCnt).ToString("N1");
                            }
                            else
                            {
                                dgvSummaries.Columns[cl].HeaderText = "A: " + 0.ToString("N1");
                            }
                        }
                        break;
                    default:
                        break;
                }

                int w = TextRenderer.MeasureText(dgvSummaries.Columns[cl].HeaderText, dataGridView1.Font).Width
                    + GetHeaderChromeWidth(DeviceDpi);
                if (w > dgvSummaries.Columns[cl].Width)
                {
                    dgvSummaries.Columns[cl].Width = w;
                    dataGridView1.Columns[cl].Width = w;

                    // Force horizontal scrollbar to recalculate visibility after column width change
                    dataGridView1.PerformLayout();
                    dataGridView1.Invalidate();
                }

                if (isGrouped)
                {
                    //source.Filter = oryginalFilter;
                    DgvPaintStart();
                    dataGridView1.Refresh();
                }

                for (int i = Summaries.Count - 1; i >= 0; i--)
                {
                    if (Summaries[i].Item1 == cl)
                    {
                        Summaries.RemoveAt(i);
                    }
                }

                Summaries.Add((cl, agrType));
                dgvSummaries.HorizontalScrollingOffset = dataGridView1.HorizontalScrollingOffset;
            }
        }

        private void FrozeAct(DataGridView dgv, int column)
        {
            if (column != -1 && dgv.Columns[column].Frozen == false)
            {
                int n = FirstNonFrozen();
                dgv.Columns[column].DisplayIndex = n;
                dgvSummaries.Columns[column].DisplayIndex = n;
                dgv.Columns[column].Frozen = true;
                dgvSummaries.Columns[column].Frozen = true;
                dgv.Columns[column].DefaultCellStyle.BackColor = dgv.AlternatingRowsDefaultCellStyle.BackColor;

            }
            else if (column != -1)
            {
                dgv.Columns[column].Frozen = false;
                dgvSummaries.Columns[column].Frozen = false;
                dgv.Columns[column].DefaultCellStyle.BackColor = dgv.DefaultCellStyle.BackColor;
            }
        }

        private int FirstNonFrozen()
        {
            int n = -1;
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                if (dataGridView1.Columns[i].Frozen == true && dataGridView1.Columns[i].DisplayIndex > n)
                {
                    n = dataGridView1.Columns[i].DisplayIndex;
                }
            }
            return n + 1;
        }

        private string GetGoodValue(Type colType, Object valueX)
        {
            if (valueX is null || valueX is DBNull)
            {
                return "";
            }
            else if (colType == typeof(string))
            {
                var valS = (string)valueX;
                if (valS.Contains('\''))
                {
                    valS = valS.Replace("'", "''");
                }
                return $"'{valS}'";
            }
            else if (colType == typeof(DateTime))
            {
                return $"'{((DateTime)valueX).ToString(DateTimeFormat)}'";
            }
            else if (colType == typeof(decimal))
            {
                return $"{((decimal)valueX).ToString(_numberWithDot)}";
            }
            else if (colType == typeof(double))
            {
                return $"{((double)valueX).ToString(_numberWithDot)}";
            }
            else if (colType == typeof(float))
            {
                return $"{((float)valueX).ToString(_numberWithDot)}";
            }
            else
            {
                return valueX.ToString() ?? string.Empty;
            }
        }

        private void DataGridView_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (dgvSummaries.Columns.Count == dataGridView1.Columns.Count)
            {
                dgvSummaries.Columns[e.Column.Index].Width = dataGridView1.Columns[e.Column.Index].Width;
                // Force horizontal scrollbar to update
                dataGridView1.PerformLayout();
            }
        }

        private void DataGridView1_Scroll(object? sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                // Sync horizontal scroll position to summary grid
                dgvSummaries.HorizontalScrollingOffset = dataGridView1.HorizontalScrollingOffset;
            }
        }
        private void DataGridView_ColumnHeaderMouseDoubleClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (FctbX != null)
            {
                int pos = FctbX.SelectionStart;
                if (sender is DataGridView sourceGrid)
                {
                    string toInsert = sourceGrid.Columns[e.ColumnIndex].Name;
                    FctbX.InsertText(toInsert);
                    FctbX.SelectionStart = pos + toInsert.Length;
                    FctbX.Focus();
                }
            }
        }
        private void DataGridView1_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }

            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                if (MouseOverFilter.ContainsKey(i) && MouseOverFilter[i] == true || MouseOverPin.ContainsKey(i) && MouseOverPin[i] == true
                    || MouseOverAggregate.ContainsKey(i) && MouseOverAggregate[i] == true)
                {
                    MouseOverFilter[i] = false;
                    MouseOverPin[i] = false;
                    MouseOverAggregate[i] = false;
                    dgv.Invalidate(dgv.GetCellDisplayRectangle(i, -1, false));
                }
            }
        }


        readonly List<(string filter, int level)> _expandedGroups = new List<(string filter, int level)>();

        private void CollapsePartOfDataDridView(int rowIndex)
        {
            //var dataView = getDataView();
            //string filterExpandedGroups = (string)dataView[rowIndex][groupInfoName];
            string filterExpandedGroups = (string)WorkingRowsList[rowIndex][_groupInfoNameIndex];
            //int lvl = (int)dataView[rowIndex][groupingLvl];
            int lvl = ((int?)WorkingRowsList[rowIndex][_groupingLvlIndex]) ?? 0;
            bool expand = false;
            if (!_expandedGroups.Contains((filterExpandedGroups, lvl)))
            {
                expand = true;
                _expandedGroups.Add((filterExpandedGroups, lvl));
            }
            else
            {
                expand = false;
                _expandedGroups.Remove((filterExpandedGroups, lvl));
                for (int i = _expandedGroups.Count - 1; i >= 0; i--)
                {
                    if (_expandedGroups[i].level <= lvl && Regex.IsMatch(_expandedGroups[i].filter, @$"(^|[^a-zA-Z1-9])+{filterExpandedGroups.Replace(@"[", @"\[").Replace(@"]", @"\]")}($|[^a-zA-Z1-9])+", RegexOptions.IgnoreCase))
                    {
                        _expandedGroups.RemoveAt(i);
                    }
                }
            }

            DgvPaintStop();
            dataGridView1.RowCount = 0;
            if (expand)
            {
                //List<string> adx = new List<string>();

                //foreach (var (filter, level) in expandedGroups)
                //{
                //    adx.Add($"{groupingLvl} = {level - 1} and {filter}");
                //}

                List<object[]> newList = new();

                for (int i = 0; i <= rowIndex; i++)
                {
                    newList.Add(WorkingRowsList[i]);
                }

                if (lvl == 1)
                {
                    //dataGridView1.RowCount = source.Count;

                    int cnt1 = filterExpandedGroups.Count(a => a == '[');

                    List<int> columnIndexes = new List<int>();
                    List<object> filterValues = new List<object>();
                    List<bool> isFilterStrings = new List<bool>();
                    List<bool> isValueTypes = new List<bool>();
                    int prevIndex = -1;
                    for (int i = 0; i < cnt1; i++)
                    {
                        int ind1 = filterExpandedGroups.IndexOf('[', prevIndex + 1) + 1;
                        int ind2 = filterExpandedGroups.IndexOf(']', prevIndex + 1);
                        prevIndex = ind2;
                        string colName = filterExpandedGroups[ind1..ind2];
                        int columnIndex = CurrentDataTable.Columns.IndexOf(colName);
                        columnIndexes.Add(columnIndex);

                        object filterValue = WorkingRowsList[rowIndex][columnIndex];
                        filterValues.Add(filterValue);
                        bool isFilterString = filterValue is string;
                        isFilterStrings.Add(isFilterString);
                        bool isValueType = filterValue is null ? false : filterValue.GetType().IsValueType;
                        isValueTypes.Add(isValueType);
                    }

                    var tmpRows = FilterWorkingList(tbSearch.Text);
                    int cnt = tmpRows.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        var row = tmpRows[i];

                        int ok1 = 0;

                        for (int j = 0; j < cnt1; j++)
                        {
                            var columnIndex = columnIndexes[j];
                            var filterValue = filterValues[j];
                            var isFilterString = isFilterStrings[j];
                            var isValueType = isValueTypes[j];

                            var actualVal = row[columnIndex];

                            if (filterValue is null && actualVal is null
                                || filterValue is DBNull && actualVal is DBNull)
                            {
                                ok1++;
                            }
                            else if (filterValue is not null && actualVal is not null
                                && filterValue is not DBNull && actualVal is not DBNull
                                && actualVal.GetType() == filterValue.GetType())
                            {
                                if (isFilterString && (string)actualVal == (string)filterValue || !isFilterString && isValueType && actualVal.Equals(filterValue))
                                {
                                    ok1++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (ok1 == cnt1)
                        {
                            newList.Add(row);
                        }
                    }
                }
                else
                {
                    foreach (var item in _groupByRows)
                    {
                        if ((int)item[_groupingLvlIndex] == lvl - 1 && ((string)item[_groupInfoNameIndex]).StartsWith(filterExpandedGroups))
                        {
                            newList.Add(item);
                        }
                    }
                }

                for (int i = rowIndex + 1; i < WorkingRowsList.Count; i++)
                {
                    newList.Add(WorkingRowsList[i]);
                }
                _workingRowsList = newList;


                //if (lvl > 1)
                //{
                //    SortFilteredData();
                //}

                //if (String.IsNullOrWhiteSpace(fctbFilter.Text))
                //{
                //    try
                //    {
                //        source.Filter = $"({groupingLvl} = {groupByColumnNums.Count} or {String.Join(" or ", adx)})";
                //    }
                //    catch (Exception ex)
                //    {
                //        MessageBox.Show(ex.Message);
                //    }
                //    dataGridView1.RowCount = 0;
                //    //dataGridView1.RowCount = source.Count;
                //    dataGridView1.RowCount = WorkingRowsList.Count;
                //    dataGridView1.Invalidate();
                //}
                //else
                //{
                //    try
                //    {
                //        source.Filter = $"({fctbFilter.Text} or {groupingLvl} > 0) and ({groupingLvl} = {groupByColumnNums.Count} or {String.Join(" or ", adx)})";
                //    }
                //    catch (Exception ex )
                //    {
                //        MessageBox.Show(ex.Message);
                //        source.Filter = $"({groupingLvl} = {groupByColumnNums.Count} or {String.Join(" or ", adx)})";
                //        fctbFilter.Text = "";
                //    }

                //    dataGridView1.RowCount = 0;
                //    //dataGridView1.RowCount = source.Count;
                //    dataGridView1.RowCount = WorkingRowsList.Count;
                //    dataGridView1.Invalidate();
                //}

            }
            else // collapse
            {
                List<object[]> newList = new();

                for (int i = 0; i <= rowIndex; i++)
                {
                    newList.Add(WorkingRowsList[i]);
                }

                bool skip = true;
                for (int i = rowIndex + 1; i < WorkingRowsList.Count; i++)
                {
                    var row = WorkingRowsList[i];
                    if (skip && ((int?)row[_groupingLvlIndex] ?? 0) < lvl)
                    {

                    }
                    else
                    {
                        skip = false;
                        newList.Add(row);
                    }
                }
                _workingRowsList = newList;
            }

            lbCnt.Text = WorkingRowsList.Count.ToString("N0");
            dataGridView1.RowCount = WorkingRowsList.Count;
            DgvPaintStart();
            dataGridView1.Invalidate();

        }

        private void DataGridView1_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1)
            {
                return;
            }

            //var dataView = getDataView();

            if (_groupByColumnNums.Count > 0 && (int?)WorkingRowsList[e.RowIndex][_groupingLvlIndex] > 0)
            {
                PerformCollapseExpnad(e.RowIndex);
            }
            else if (FctbX != null) // nor grouping row/ not header etc.
            {
                int pos = FctbX.SelectionStart;
                var valueX = dataGridView1[e.ColumnIndex, e.RowIndex].Value;
                if (valueX != null)
                {
                    string toInsert;
                    if (valueX.GetType() == typeof(string))
                    {
                        toInsert = $"'{valueX.ToString()}'";
                    }
                    else if (valueX.GetType() == typeof(DateTime))
                    {
                        toInsert = $"'{((DateTime)valueX).ToString(DateTimeFormat)}'";
                    }
                    else if (valueX.GetType() == typeof(decimal))
                    {
                        toInsert = $"{((decimal)valueX).ToString(_numberWithDot)}";
                    }
                    else if (valueX.GetType() == typeof(double))
                    {
                        toInsert = $"{((double)valueX).ToString(_numberWithDot)}";
                    }
                    else if (valueX.GetType() == typeof(byte[]))
                    {
                        string tmp = Path.GetTempPath() + "Exported\\";
                        if (!Directory.Exists(tmp))
                            Directory.CreateDirectory(tmp);


                        string filePath = tmp + StringExtensions.RandomName() + ".";
                        try
                        {
                            bool isImage = false;
                            byte[] blob = (byte[])valueX;
                            //JPEG:
                            if (blob.Length >= 10 &&
                                blob[0] == 0xFF &&//FF D8
                                blob[1] == 0xD8 &&
                                (
                                 (blob[6] == 0x4A &&//'JFIF'
                                  blob[7] == 0x46 &&
                                  blob[8] == 0x49 &&
                                  blob[9] == 0x46)
                                  ||
                                 (blob[6] == 0x45 &&//'EXIF'
                                  blob[7] == 0x78 &&
                                  blob[8] == 0x69 &&
                                  blob[9] == 0x66)
                                ) &&
                                blob[10] == 00)
                            {
                                filePath = filePath + "jpg";
                                isImage = true;
                            }
                            //PNG 
                            else if (
                                blob.Length >= 7 &&
                                blob[0] == 0x89 && //89 50 4E 47 0D 0A 1A 0A
                                blob[1] == 0x50 &&
                                blob[2] == 0x4E &&
                                blob[3] == 0x47 &&
                                blob[4] == 0x0D &&
                                blob[5] == 0x0A &&
                                blob[6] == 0x1A &&
                                blob[7] == 0x0A)
                            {
                                filePath = filePath + "png";
                                isImage = true;
                            }
                            //GIF
                            else if (
                                blob.Length >= 3 &&
                                blob[0] == 0x47 &&//'GIF'
                                blob[1] == 0x49 &&
                                blob[2] == 0x46)
                            {
                                filePath = filePath + "gif";
                                isImage = true;
                            }
                            //BMP
                            else if (blob.Length >= 2 &&
                                blob[0] == 0x42 &&//42 4D
                                blob[1] == 0x4D)
                            {
                                filePath = filePath + "bmp";
                                isImage = true;
                            }
                            //TIFF
                            else if (
                                blob.Length >= 4 &&
                                (blob[0] == 0x49 &&//49 49 2A 00
                                 blob[1] == 0x49 &&
                                 blob[2] == 0x2A &&
                                 blob[3] == 0x00)
                                 ||
                                (blob[0] == 0x4D &&//4D 4D 00 2A
                                 blob[1] == 0x4D &&
                                 blob[2] == 0x00 &&
                                 blob[3] == 0x2A))
                            {
                                filePath = filePath + "tiff";
                                isImage = true;
                            }
                            else
                            {
                                filePath = filePath + "unknown";
                                isImage = false;
                            }

                            using (FileStream fileStream = new(filePath, FileMode.Create))
                            {
                                fileStream.Write(blob, 0, blob.Length);
                            }
                            if (isImage)
                            {
                                Form fm = new Form();

                                PictureBox pb = new PictureBox();
                                pb.Image = System.Drawing.Image.FromFile(filePath);
                                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                                pb.Dock = DockStyle.Fill;
                                int width = pb.Image.Width;
                                int height = pb.Image.Height;
                                double ff = 1;
                                if (width > 500)
                                {
                                    ff = width / 500;
                                    width = 500;
                                    height = (int)(height / ff);
                                }
                                else if (height > 500)
                                {
                                    ff = height / 500;
                                    height = 500;
                                    width = (int)(width / ff);
                                }

                                fm.Width = width;
                                fm.Height = height + 40;
                                fm.Controls.Add(pb);
                                fm.ShowDialog();
                                pb.Image.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        toInsert = $"\r\n--file saved to {filePath}\r\n";
                    }
                    else
                    {
                        toInsert = valueX.ToString() ?? string.Empty;
                    }
                    FctbX.InsertText(toInsert);
                    FctbX.SelectionStart = pos + toInsert.Length;
                    FctbX.Focus();
                }
            }
        }

        private void DataGridView1_RowHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (_groupByColumnNums.Count > 0 && (int?)WorkingRowsList[e.RowIndex][_groupingLvlIndex] > 0)
            {
                PerformCollapseExpnad(e.RowIndex);
            }
        }

        private void PerformCollapseExpnad(int rowindex)
        {
            int a = dataGridView1.FirstDisplayedScrollingRowIndex;
            CollapsePartOfDataDridView(rowindex);
            dataGridView1.FirstDisplayedScrollingRowIndex = (a > 0 ? a : 0);
        }

        public event Action<string>? WriteStats;

        private const int MaxSelectionStatsCells = 20_000;
        private const int MaxWholeGridStatsCells = 25_000_000;
        private int _wholeGridStatsVersion;

        private readonly System.Windows.Forms.Timer _statsDebounceTimer = new()
        {
            Interval = 80,
        };

        private void DataGridView1_SelectionChanged(object? sender, EventArgs e)
        {
            Int32 selectedCellCount = dataGridView1.GetCellCount(DataGridViewElementStates.Selected);

            if (selectedCellCount == dataGridView1.ColumnCount * dataGridView1.RowCount)
            {
                dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            }
            else if (dataGridView1.ClipboardCopyMode != DataGridViewClipboardCopyMode.EnableWithoutHeaderText)
            {
                dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            }

            ScheduleSelectionStatsUpdate(selectedCellCount);

            //https://github.com/KrzysztofDusko/Just-Data/issues/166
            if (selectedCellCount == 1)
            {
                var cell = dataGridView1.SelectedCells[0];
                int colNum = cell.ColumnIndex;
                if (colNum == dataGridView1.ColumnCount - 1)
                {
                    int n1 = dataGridView1.FirstDisplayedScrollingColumnIndex;//partial included
                    int n2 = dataGridView1.DisplayedColumnCount(false);//partial included
                    if (n1 + n2 != dataGridView1.ColumnCount && !dataGridView1.Columns[colNum].Frozen)
                    {
                        dataGridView1.FirstDisplayedScrollingColumnIndex = colNum;
                    }
                }
            }
        }

        private void ScheduleSelectionStatsUpdate(int selectedCellCount)
        {
            _statsDebounceTimer.Stop();
            if (selectedCellCount > MaxSelectionStatsCells)
            {
                if (selectedCellCount == dataGridView1.ColumnCount * dataGridView1.RowCount
                    && selectedCellCount < MaxWholeGridStatsCells)
                {
                    // Whole-grid selection: aggregate the sum in the background (old
                    // behavior) so large result sets stay responsive.
                    ScheduleWholeGridStats(selectedCellCount);
                }
                else
                {
                    WriteStats?.Invoke($"selected: {selectedCellCount.ToString("N0")}");
                }
                return;
            }
            _statsDebounceTimer.Start();
        }

        private void ScheduleWholeGridStats(int selectedCellCount)
        {
            int version = ++_wholeGridStatsVersion;
            DataTable table = CurrentDataTable;
            // The grid is virtual and serves cells from WorkingRowsList, not from
            // the schema-only CurrentDataTable (which has no rows). Capture the row
            // list before the background run so a later filter can't shift it under us.
            List<object[]> rows = WorkingRowsList;
            Task.Run(() =>
            {
                decimal sum = 0;
                for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                {
                    if (!AggregatePossible(table, columnIndex))
                    {
                        continue;
                    }

                    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    {
                        object? value = rows[rowIndex][columnIndex];
                        if (value is null || value is DBNull || value is not IConvertible convertible)
                        {
                            continue;
                        }

                        try
                        {
                            sum += convertible.ToDecimal(CultureInfo.InvariantCulture);
                        }
                        catch (OverflowException)
                        {
                            // A double beyond the decimal range (~7.9e28) cannot be
                            // summed as decimal; skip the cell instead of faulting.
                        }
                        catch (InvalidCastException)
                        {
                        }
                    }
                }

                return sum;
            }).ContinueWith(task =>
            {
                if (IsDisposed
                    || dataGridView1.IsDisposed
                    || _wholeGridStatsVersion != version
                    || task.IsFaulted)
                {
                    return;
                }

                WriteStats?.Invoke($"all cells: {selectedCellCount.ToString("N0")}, sum {task.Result.ToString("N3", CultureInfo.CurrentCulture)}");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void DataGridView1_SelectionStatsTick(object? sender, EventArgs e)
        {
            _statsDebounceTimer.Stop();
            if (IsDisposed || dataGridView1.IsDisposed)
            {
                return;
            }

            int selectedCellCount = dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
            if (selectedCellCount == 0)
            {
                WriteStats?.Invoke("Selected 0 cells | Sum 0.000 | Count 0 | Distinct 0 | Min - | Max -");
                return;
            }
            if (selectedCellCount > MaxSelectionStatsCells)
            {
                WriteStats?.Invoke($"selected: {selectedCellCount.ToString("N0")}");
                return;
            }

            var cellValues = new List<object?>(selectedCellCount);
            foreach (var item in dataGridView1.SelectedCells)
            {
                if (item is not DataGridViewCell cell)
                {
                    continue;
                }

                cellValues.Add(cell.Value);
            }

            // Infer the numeric type from each cell's runtime value (the grid columns are
            // all object-typed), so Sum/Min/Max/Count match the old behavior.
            WriteStats?.Invoke(FormatSelectionStats(CellStatsCalculator.Calculate(cellValues)));
        }

        private static string FormatSelectionStats(CellStats stats)
        {
            string minText = stats.Minimum.HasValue ? stats.Minimum.Value.ToString("N3", CultureInfo.CurrentCulture) : "-";
            string maxText = stats.Maximum.HasValue ? stats.Maximum.Value.ToString("N3", CultureInfo.CurrentCulture) : "-";
            string sumText = (stats.Sum ?? 0m).ToString("N3", CultureInfo.CurrentCulture);
            int notNullCount = stats.Count - stats.NullCount;
            return $"Selected {stats.Count.ToString("N0")} cells | Sum {sumText} | Count {notNullCount.ToString("N0")} | Distinct {stats.DistinctCount.ToString("N0")} | Min {minText} | Max {maxText}";
        }

        private void DataGridView1_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.ColumnIndex == -1 || e.RowIndex == -1)
            {
                return;
            }
            try
            {
                if (sender is not DataGridView grid)
                {
                    return;
                }
                string? w = grid[e.ColumnIndex, e.RowIndex].Value as string;
                if (w is not null && w.Length > 200)
                {
                    e.ToolTipText = $"{w.Substring(0, 200)}{Environment.NewLine}...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Data grid error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DataGridView1_DragDrop(object? sender, DragEventArgs e)
        {
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
            var hittest = dataGridView1.HitTest(clientPoint.X, clientPoint.Y);
            var cellvalueSet = e.Data?.GetData(typeof(DragData)) as DragData;

            if (cellvalueSet is null && _draggedSpecial is not null)
            {
                cellvalueSet = _draggedSpecial;
            }

            if (cellvalueSet == null)
                return;

            if (e.Effect == DragDropEffects.Copy && hittest.ColumnIndex != -1 && cellvalueSet.DgvType == "data")
            {
                try
                {
                    DataGridViewColumn? dataColumn = dataGridView1.Columns[cellvalueSet.Cellvalue];
                    DataGridViewColumn? summaryColumn = dgvSummaries.Columns[cellvalueSet.Cellvalue];
                    if (dataColumn is not null && summaryColumn is not null)
                    {
                        dataColumn.DisplayIndex = dataGridView1.Columns[hittest.ColumnIndex].DisplayIndex;
                        summaryColumn.DisplayIndex = dgvSummaries.Columns[hittest.ColumnIndex].DisplayIndex;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            else if (cellvalueSet.DgvType == "groups")
            {
                if (dgvDrop.Columns.Contains(cellvalueSet.Cellvalue))
                {
                    dgvDrop.Columns.Remove(cellvalueSet.Cellvalue);
                }

                if (dgvDrop.Columns.Count > 0)
                {
                    await DoProperGroupBy();
                }
                else
                {
                    await ClearGroupingSorting();
                    dataGridView1.RowCount = 0;
                    //dataGridView1.RowCount = source.Count;
                    lbCnt.Text = WorkingRowsList.Count.ToString("N0");
                    dataGridView1.RowCount = WorkingRowsList.Count;
                    dgvLabel.Visible = true;
                }
            }
        }
        private void DataGridView1_DragOver(object? sender, DragEventArgs e)
        {
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
            var hittest = dgvDrop.HitTest(clientPoint.X, clientPoint.Y);
            if (hittest.RowIndex == -1)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void DataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1)
                return;

            if (dataGridView1.Columns.Contains(_groupingLvl)
                && Convert.ToInt32(dataGridView1[_groupingLvl, e.RowIndex].Value) > 0)
                InnerDataGridView.Rows[e.RowIndex].Selected = true;
        }

        private void DgvDrop_MouseMove(object? sender, MouseEventArgs e)
        {
            int hoverColumn = -1;
            var hoverHit = dgvDrop.HitTest(e.X, e.Y);
            if (hoverHit.RowIndex == -1 && hoverHit.ColumnIndex >= 0)
            {
                Rectangle cellBounds = dgvDrop.GetCellDisplayRectangle(hoverHit.ColumnIndex, -1, false);
                if (GetGroupingRemoveButtonBounds(cellBounds, DeviceDpi).Contains(e.Location))
                {
                    hoverColumn = hoverHit.ColumnIndex;
                }
            }

            if (_groupRemoveHoverColumn != hoverColumn)
            {
                _groupRemoveHoverColumn = hoverColumn;
                dgvDrop.Invalidate();
            }

            if (e.Button == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (_dragBoxFromMouseDown != Rectangle.Empty && !_dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    // Proceed with the drag and drop, passing in the list item.
                    _draggedSpecial = new DragData(_columnDraggedName, _columnDraggedNameSourceName);
                    DragDropEffects dropEffect = dgvDrop.DoDragDrop(_draggedSpecial, DragDropEffects.Copy);
                }
                else
                {
                    _draggedSpecial = null;
                }
            }
        }

        private void DgvDrop_MouseLeave(object? sender, EventArgs e)
        {
            if (_groupRemoveHoverColumn != -1)
            {
                _groupRemoveHoverColumn = -1;
                dgvDrop.Invalidate();
            }
        }

        private async void DgvDrop_MouseDown(object? sender, MouseEventArgs e)
        {
            var hittestInfo = dgvDrop.HitTest(e.X, e.Y);

            if (e.Button == MouseButtons.Left && hittestInfo.RowIndex == -1 && hittestInfo.ColumnIndex >= 0)
            {
                Rectangle cellBounds = dgvDrop.GetCellDisplayRectangle(hittestInfo.ColumnIndex, -1, false);
                if (GetGroupingRemoveButtonBounds(cellBounds, DeviceDpi).Contains(e.Location))
                {
                    _dragBoxFromMouseDown = Rectangle.Empty;
                    _groupRemoveHoverColumn = -1;
                    await RemoveGroupingColumnAsync(hittestInfo.ColumnIndex);
                    return;
                }
            }

            if (hittestInfo.RowIndex == -1 && hittestInfo.ColumnIndex != -1 && Cursor.Current != Cursors.SizeWE)
            {
                _columnDraggedName = dgvDrop.Columns[hittestInfo.ColumnIndex].Name;
                _columnDraggedNameSourceName = "groups";
                if (_columnDraggedName != null)
                {
                    // Remember the point where the mouse down occurred. 
                    // The DragSize indicates the size that the mouse can move 
                    // before a drag event should be started.                
                    Size dragSize = SystemInformation.DragSize;
                    // Create a rectangle using the DragSize, with the mouse position being
                    // at the center of the rectangle.
                    _dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
                }
            }
            else
            {
                _dragBoxFromMouseDown = Rectangle.Empty;
            }
        }

        private async Task RemoveGroupingColumnAsync(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= dgvDrop.Columns.Count)
            {
                return;
            }

            dgvDrop.Columns.RemoveAt(columnIndex);
            dgvLabel.Visible = dgvDrop.Columns.Count == 0;
            dataGridView1.RowCount = 0;

            if (dgvDrop.Columns.Count > 0)
            {
                await DoProperGroupBy();
            }
            else
            {
                await ClearGroupingSorting();
                lbCnt.Text = WorkingRowsList.Count.ToString("N0");
                dataGridView1.RowCount = WorkingRowsList.Count;
            }

            dataGridView1.Invalidate();
        }

        private async void ItemRemoveGrouping_Click(object? sender, EventArgs e)
        {
            dgvDrop.Columns.Clear();
            dgvLabel.Visible = true;
            dataGridView1.RowCount = 0;
            await ClearGroupingSorting();
            lbCnt.Text = WorkingRowsList.Count.ToString("N0");
            dataGridView1.RowCount = WorkingRowsList.Count;
        }

        private void ItemRemoveSorting_Click(object? sender, EventArgs e)
        {
            if (_source?.IsSorted == true)
            {
                _source.RemoveSort();
                if (_groupByColumnNums.Count > 0)
                {
                    _source.Sort = BasicSortForGroupedData();
                }
                dataGridView1.Invalidate();
            }
        }

        public void FinishColorize(IColorTheme colorTheme, bool useSpecialColoring)
        {
            BackColor = dataGridView1.BackgroundColor;
            ForeColor = dataGridView1.ForeColor;
            groupPanel.BackColor = dataGridView1.BackColor;
            groupPanel.ForeColor = dataGridView1.ForeColor;

            bool darkTheme = useSpecialColoring && IsDark();
            Color gridBack = dataGridView1.BackColor;
            Color gridFore = dataGridView1.ForeColor;

            colorTheme.ColorDataGridView(dgvSummaries);
            dgvSummaries.BackgroundColor = gridBack;
            dgvSummaries.ForeColor = gridFore;

            colorTheme.ColorDataGridView(dgvDrop);
            dgvDrop.BackgroundColor = gridBack;
            dgvDrop.ForeColor = gridFore;

            tbSearch.BackColor = gridBack;
            tbSearch.ForeColor = gridFore;
            // Match the single-line chrome used by the column selector ComboBox.
            tbSearch.BorderStyle = BorderStyle.FixedSingle;

            Color toolbarBorder = darkTheme ? Color.FromArgb(89, 101, 119) : Color.FromArgb(205, 213, 224);
            Color toolbarHover = darkTheme ? Color.FromArgb(55, 79, 105) : Color.FromArgb(232, 241, 250);
            Color toolbarPressed = darkTheme ? Color.FromArgb(64, 96, 130) : Color.FromArgb(218, 232, 247);

            cbAprox.BackColor = gridBack;
            cbAprox.ForeColor = gridFore;
            cbAprox.UseVisualStyleBackColor = false;
            cbAprox.FlatStyle = FlatStyle.Flat;
            cbAprox.FlatAppearance.BorderSize = 1;
            cbAprox.FlatAppearance.BorderColor = toolbarBorder;
            cbAprox.FlatAppearance.MouseOverBackColor = toolbarHover;
            cbAprox.FlatAppearance.MouseDownBackColor = toolbarPressed;
            cbAprox.FlatAppearance.CheckedBackColor = darkTheme ? Color.FromArgb(55, 94, 132) : Color.FromArgb(224, 238, 250);

            foreach (Button button in new[] { btCopyAsExcel, btCopyAsText, btDownload, btOpenInExcel, btRowView })
            {
                button.BackColor = gridBack;
                button.ForeColor = gridFore;
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = toolbarBorder;
                button.FlatAppearance.MouseOverBackColor = toolbarHover;
                button.FlatAppearance.MouseDownBackColor = toolbarPressed;
                button.BackgroundImage = null;
                button.BackgroundImageLayout = ImageLayout.None;
            }

            if (useSpecialColoring)
            {
                cbJumpToColumn.DrawMode = DrawMode.OwnerDrawFixed;
                cbJumpToColumn.DrawItem -= CbJumpToColumn_DrawItem;
                cbJumpToColumn.DrawItem += CbJumpToColumn_DrawItem;
            }
            else
            {
                cbJumpToColumn.DrawItem -= CbJumpToColumn_DrawItem;
                cbJumpToColumn.DrawMode = DrawMode.Normal;
            }

            cbJumpToColumn.BackColor = gridBack;
            cbJumpToColumn.ForeColor = gridFore;
            cbJumpToColumn.FlatStyle = FlatStyle.Flat;
            dgvLabel.ForeColor = darkTheme ? Color.FromArgb(170, 170, 170) : SystemColors.ButtonShadow;
            lbCnt.BackColor = Color.Transparent;
            dgvLabel.BackColor = Color.Transparent;

            if (darkTheme)
            {
                _forSmothColor = Color.FromArgb(120, 120, 120);
                _forSmothPen = new Pen(_forSmothColor);
                _forSmothBrush = new SolidBrush(_forSmothColor);
                tbSearch.ForeColor = dataGridView1.ForeColor;
                lbCnt.ForeColor = dataGridView1.ForeColor;
                _nullForeColor = Color.FromArgb(130, 130, 130);
                _nullBackColor = Color.FromArgb(50, 50, 48);
            }
            else
            {
                _forSmothColor = Color.FromArgb(135, 135, 135);
                _forSmothPen = new Pen(_forSmothColor);
                _forSmothBrush = new SolidBrush(_forSmothColor);
                tbSearch.ForeColor = Color.Black;
                lbCnt.ForeColor = ForeColor;
                _nullForeColor = Color.FromArgb(105, 105, 105);
                _nullBackColor = Color.FromArgb(255, 255, 224);
            }

            if (GroupBackgroundMiddle.R + GroupBackgroundMiddle.G + GroupBackgroundMiddle.B <= 450)
            {
                GroupFontBrush = Brushes.White;
            }
            else
            {
                GroupFontBrush = Brushes.Black;
            }

        }

        private void CbJumpToColumn_DrawItem(object? sender, DrawItemEventArgs e) =>
            _uiHelperService.ColorComboBox_DrawItem(
                sender ?? cbJumpToColumn,
                e,
                useSpecialColoring: true,
                generalBrush: _colorTheme.GeneralBrush);

        System.Windows.Forms.Timer? _searchTimer;

        private void TbSearch_TextChanged(object? sender, EventArgs e)
        {
            if (_searchTimer is null)
            {
                var searchTimer = new System.Windows.Forms.Timer()
                {
                    Interval = 50
                };
                searchTimer.Tick += (_, _) =>
                {
                    searchTimer.Stop();
                    DgvPaintStop();
                    dataGridView1.RowCount = 0;
                    _workingRowsList = FilterWorkingList(tbSearch.Text);
                    lbCnt.Text = WorkingRowsList.Count.ToString("N0");
                    dataGridView1.RowCount = WorkingRowsList.Count;
                    DgvPaintStart();
                    dataGridView1.Invalidate();
                };
                _searchTimer = searchTimer;
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void CbJumpToColumn_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? txt = cbJumpToColumn.SelectedItem as string;
            DataGridViewCell? firstDisplayedCell = dataGridView1.FirstDisplayedCell;
            DataGridViewColumn? selectedColumn = string.IsNullOrWhiteSpace(txt) ? null : dataGridView1.Columns[txt];
            if (selectedColumn is not null && firstDisplayedCell is not null)
            {
                dataGridView1.FirstDisplayedCell = dataGridView1.Rows[firstDisplayedCell.RowIndex].Cells[selectedColumn.Index];
            }
        }

        public Action<string> DoMessageAction = _ => { };

        private async void BtCopyAsExcel_Click(object? sender, EventArgs e)
        {
            try
            {
                btCopyAsExcel.Enabled = false;
                var exportCommand = new ExportFullCommand(CurrentDataTable, RowsList, this, _importExportTasks);
                await exportCommand.ExecuteAsync();
            }
            finally
            {
                btCopyAsExcel.Enabled = true;
            }
        }

        private async void BtOpenInExcel_Click(object? sender, EventArgs e)
        {
            try
            {
                btOpenInExcel.Enabled = false;
                var openCommand = new OpenInExcelCommand(_importExportTasks, CurrentDataTable, RowsList, AttachedSQL, DoMessageAction);
                await openCommand.ExecuteAsync();
            }
            finally
            {
                btOpenInExcel.Enabled = true;
            }
        }

        private async void BtCopyAsText_Click(object? sender, EventArgs e)
        {
            var copyCommand = new CopyAsTextCommand(dataGridView1);
            await copyCommand.ExecuteAsync();
        }

        SaveFileDialog? _saveFileDialog;
        private void BtRowView_Click(object? sender, EventArgs e) => RowViewRequested?.Invoke(this, EventArgs.Empty);

        private async void BtDownload_Click(object? sender, EventArgs e)
        {
            btDownload.Enabled = false;
            try
            {
                var downloadCommand = new DownloadCommand(_importExportTasks, CurrentDataTable, RowsList, AttachedSQL, DoMessageAction, () =>
                {
                    if (_saveFileDialog is null)
                    {
                        _saveFileDialog = new SaveFileDialog();
                    }
                    return _saveFileDialog;
                });
                await downloadCommand.ExecuteAsync();
            }
            finally
            {
                btDownload.Enabled = true;
            }
        }
        private void CbJumpToColumn_DropDown(object? sender, EventArgs e)
        {
            EnsureColumnList();
        }
    }
}


