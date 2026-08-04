// BaseWindow result grid UI helpers partial.
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
using AppBase.Services.Sql;
using JustyBaseLegacy.UI.Sql;
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
        private readonly Image _normalXimage = JustData.Properties.Resources.close;
        private readonly Image _normalPinImage = JustData.Properties.Resources.gray_pin;
        private readonly Image _activePinImage = JustData.Properties.Resources.Black_pin;

        private void DataGridViewNowe_writeStats(string obj) => mainTextBox.Text = obj;

        private void DataGridViewNowe_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            string rowIdx = (e.RowIndex + 1).ToString();
            var centerFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, _colorTheme.GeneralBrush, headerBounds, centerFormat);
        }

        private void CopyWithHeadersStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentDataGrid.AreAllCellsSelected(false))
                {
                    _currentDataGrid.ClearSelection();
                    _currentDataGrid.SelectionMode = DataGridViewSelectionMode.ColumnHeaderSelect;
                    _currentDataGrid.SelectAll();
                    _currentDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
                    DataObject dataObj = _currentDataGrid.GetClipboardContent();

                    Clipboard.SetDataObject(dataObj);
                    _currentDataGrid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
                    _currentDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
                }
                else
                {
                    _currentDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
                    DataObject dataObj = _currentDataGrid.GetClipboardContent();
                    Clipboard.SetDataObject(dataObj);
                    _currentDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copyWithOutHeadersStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var prevCopyMode = _currentDataGrid.ClipboardCopyMode;
                _currentDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
                DataObject dataObj = _currentDataGrid.GetClipboardContent();
                Clipboard.SetDataObject(dataObj);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Result grid operation failed: {exception.GetType().Name}");
            }
        }

        private void ClearFilters_Click(object sender, EventArgs e)
        {
            _currentMyGrid.ClearFilters();
        }

        private readonly Color _nullForeColor = Color.FromArgb(105, 105, 105);
        private readonly Color _nullBackColor = Color.FromArgb(255, 255, 224);

        internal void ConfigureResultDataGrid(CustomDataGridView grid)
        {
            grid.ContextMenuStrip = cmGridContextMenuStrip1;
            grid.DataGridMouseDown -= DataGrid_MouseDown_MouseDown;
            grid.DataGridMouseDown += DataGrid_MouseDown_MouseDown;
            grid.WriteStats -= DataGridViewNowe_writeStats;
            grid.WriteStats += DataGridViewNowe_writeStats;
            grid.RowViewRequested -= ResultGrid_RowViewRequested;
            grid.RowViewRequested += ResultGrid_RowViewRequested;
        }

        private void ResultGrid_RowViewRequested(object? sender, EventArgs e)
        {
            if (sender is not CustomDataGridView grid)
            {
                return;
            }

            _currentDataGrid = grid.InnerDataGridView;
            _currentMyGrid = grid;
            SingleRow_Click(sender, e);
        }

        private void SingleRow_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add($"colname", typeof(string));
            int nn = 3;
            int i = 0;
            if (_currentDataGrid?.CurrentCell != null && _currentDataGrid.CurrentCell.RowIndex >= 0)
            {
                int ind = _currentDataGrid.CurrentCell.RowIndex;
                var r = _currentDataGrid.Rows[ind];

                int j = ind;
                while (j < _currentDataGrid.Rows.Count && j < ind + nn)
                {
                    dt.Columns.Add($"Row {j + 1}", typeof(string));
                    j++;
                }

                int m = j - ind;
                object[] colData = new object[m + 1];

                foreach (DataGridViewColumn item in _currentDataGrid.Columns)
                {
                    colData[0] = item.HeaderText;
                    for (int l = 0; l < m; l++)
                    {
                        var val = _currentDataGrid.Rows[ind + l].Cells[i].Value;

                        if (val == DBNull.Value || val is null)
                        {
                            colData[l + 1] = DBNull.Value;
                        }

                        if (val is decimal decVal)
                        {
                            colData[l + 1] = decVal.ToString(_numberFormattingContext.NumberWithDot);
                        }
                        else if (val is double doubleVal)
                        {
                            colData[l + 1] = doubleVal.ToString(_numberFormattingContext.NumberWithDot);
                        }
                        else if (val is Single singleVal)
                        {
                            colData[l + 1] = singleVal.ToString(_numberFormattingContext.NumberWithDot);
                        }
                        else if (val is DateTime dateTime)
                        {
                            colData[l + 1] = dateTime.ToString(_applicationSettingsContext.Config.DateTimeFormat);
                        }
                        else
                        {
                            colData[l + 1] = (val ?? "").ToString();
                        }
                    }
                    dt.Rows.Add(colData);
                    //dt.Rows.Add(item.HeaderText, r.Cells[i].Value.ToString());
                    i++;
                }
            }
            else
            {
                foreach (DataGridViewColumn item in _currentDataGrid.Columns)
                {
                    dt.Rows.Add(item.HeaderText);
                }
            }

            ThemedDataGridView dataGridViewT = new ThemedDataGridView
            {
                AllowUserToOrderColumns = true,
                ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = System.Windows.Forms.DockStyle.Fill,
                Location = new System.Drawing.Point(3, 3),
                Size = new System.Drawing.Size(746, 77),
                ReadOnly = true,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = true
            };

            dataGridViewT.ContextMenuStrip = cmGridContextMenuStripRowView;
            dataGridViewT.ReadOnly = true;

            dataGridViewT.RowTemplate.Height = _currentDataGrid.RowTemplate.Height;

            dataGridViewT.EnableHeadersVisualStyles = false;
            dataGridViewT.RowPostPaint += DataGridViewNowe_RowPostPaint;//number of rows
            dataGridViewT.MouseClick += DataGridViewT_MouseClick;
            dataGridViewT.CellDoubleClick += DataGridViewT_CellDoubleClick;


            dataGridViewT.CellFormatting += (s, e) =>
            {
                if (e.Value == DBNull.Value && _currentMyGrid != null)
                {
                    e.CellStyle.BackColor = _nullForeColor;
                    e.CellStyle.ForeColor = _nullBackColor;
                }
            };

            dataGridViewT.RowHeadersVisible = true;

            TabControl tc = (CurrentSplitContainer.Tag as ResultData).TabControlSQLResults;
            TabPagePicture tp = new TabPagePicture();
            tp.Text = $"*{tc.SelectedTab.Text.Replace("@", "")}";
            tp.CloseImage = _normalXimage;
            tp.PinImage = _normalPinImage;

            tp.Tag = new TabPageResultsTag()
            {
                Docked = false,
                DocumentId = CurrentEditorDocumentId
            };
            tp.Controls.Add(dataGridViewT);

            tc.TabPages.Add(tp);
            tc.SelectedTab = tp;

            PopulateRowViewGrid(dataGridViewT, dt);

            for (i = 0; i < dataGridViewT.Columns.Count; i++)
            {
                dataGridViewT.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                dataGridViewT.Columns[i].Width = (int)(TextRenderer.MeasureText(dataGridViewT.Columns[i].Name, dataGridViewT.Font).Width) + 10;

                int ll = 10;
                if (ll > dt.Rows.Count)
                {
                    ll = dt.Rows.Count;
                }
                for (int j = 0; j < ll; j++)
                {
                    int newVal = (int)(TextRenderer.MeasureText(dt.Rows[j][i].ToString(), dataGridViewT.Font).Width) + 10;
                    if (newVal > dataGridViewT.Columns[i].Width)
                    {
                        dataGridViewT.Columns[i].Width = newVal;
                    }
                }
            }

            if (dataGridViewT.Columns.Count > 0)
            {
                dataGridViewT.Columns[0].Frozen = true;
            }

            _colorTheme.ColorDataGridView(dataGridViewT, true);
            _uiHelperService.DoubleBufDateGridView(dataGridViewT);
        }

        /// <summary>
        /// Fills a row-view grid without <see cref="DataGridView.DataSource"/> binding,
        /// which is unsupported under WinForms trimming / Native AOT.
        /// </summary>
        private static void PopulateRowViewGrid(DataGridView grid, DataTable table)
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.Rows.Clear();

            foreach (DataColumn column in table.Columns)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = column.ColumnName,
                    HeaderText = column.ColumnName,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ReadOnly = true,
                });
            }

            foreach (DataRow row in table.Rows)
            {
                grid.Rows.Add(row.ItemArray);
            }
        }

        private void DataGridViewT_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1)
            {
                return;
            }

            var fctbX = CurrentTB;
            var dataGridView1 = sender as DataGridView;
            int pos = fctbX.SelectionStart;

            var valueX = dataGridView1[e.ColumnIndex, e.RowIndex].Value;
            if (valueX != null)
            {
                string toInsert = "";
                if (e.ColumnIndex == 0)
                {
                    toInsert = $"{valueX}";
                }
                else
                {
                    toInsert = $"'{valueX}'";
                }

                fctbX.InsertText(toInsert);
                fctbX.SelectionStart = pos + toInsert.Length;
                fctbX.Focus();
            }
        }

        DataGridView gridViewCurrentRow = null;
        private void DataGridViewT_MouseClick(object sender, MouseEventArgs e)
        {
            gridViewCurrentRow = sender as DataGridView;
        }

        private void pokazSQL_Click(object sender, EventArgs e)
        {
            if (_currentMyGrid != null)
            {
                AddMainTab(null, "SQL", _currentMyGrid.AttachedSQL);
            }
            else
            {
                AddMainTab(null, "SQL - empty", "no SQL");
            }
        }


        private void ShowDiff_Click(object sender, EventArgs e)
        {
            if (gridViewCurrentRow != null)
            {
                int n = gridViewCurrentRow.RowCount;
                int m = gridViewCurrentRow.ColumnCount;
                if (m <= 1)
                {
                    _loggerLoud.MessageBox_Show(this, "Too few columns.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var c1 = gridViewCurrentRow.DefaultCellStyle.BackColor;
                var c2 = Color.FromArgb(255 - c1.R, 255 - c1.G, 255 - c1.B);
                var c3 = gridViewCurrentRow.DefaultCellStyle.ForeColor;
                var c4 = Color.FromArgb(255 - c3.R, 255 - c3.G, 255 - c3.B);

                for (int i = 0; i < n; i++)
                {
                    var row = gridViewCurrentRow.Rows[i].Cells;
                    for (int j = 1; j < m - 1; j++)
                    {
                        if (row[j].Value.ToString() != row[j + 1].Value.ToString())
                        {
                            gridViewCurrentRow.Rows[i].DefaultCellStyle.BackColor = c2;
                            gridViewCurrentRow.Rows[i].DefaultCellStyle.ForeColor = c4;
                            break;
                        }
                    }
                }
                gridViewCurrentRow.Invalidate();
            }
        }
    }
}
