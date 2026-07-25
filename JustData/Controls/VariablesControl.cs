using AppBase.Common;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBaseLegacy.UI.Helpers;
using JustData.Application.Variables;
using JustData.ViewModels.Variables;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls
{
    public partial class VariablesControl : UserControl
    {
        private readonly BaseWindow _baseWindow;
        private readonly VariablesViewModel _viewModel;
        private readonly Func<string?> _documentKeyProvider;
        private readonly IUiHelperService _uiHelperService;
        private readonly IColorTheme _colorTheme;
        private readonly List<VariableRow> _variableRows = new();

        private readonly struct VariableRow
        {
            public VariableRow(string key, bool isSession)
            {
                Key = key;
                IsSession = isSession;
            }

            public string Key { get; }
            public bool IsSession { get; }
        }

        public VariablesControl()
        {
            InitializeComponent();
        }

        public VariablesControl(
            BaseWindow baseWindow,
            VariablesViewModel viewModel,
            Func<string?> documentKeyProvider,
            IUiHelperService uiHelperService,
            IColorTheme colorTheme)
        {
            _baseWindow = baseWindow;
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _documentKeyProvider = documentKeyProvider ?? throw new ArgumentNullException(nameof(documentKeyProvider));
            _uiHelperService = uiHelperService;
            _colorTheme = colorTheme;

            InitializeComponent();
            _viewModel.Entries.ListChanged += Entries_ListChanged;
            _viewModel.InsertVariableRequested += ViewModel_InsertVariableRequested;
            InitializeVariablesControl();
        }

        public DataGridView DataGridView => _dgvVariables;

        private void InitializeVariablesControl()
        {
            _dgvVariables.CellDoubleClick += DgvVariables_CellDoubleClick;
            _dgvVariables.CellToolTipTextNeeded += DgvVariables_CellToolTipTextNeeded;
            _dgvVariables.CellValueNeeded += DgvVariables_CellValueNeeded;
            _btClearVariables.Click += BtClearVariables_Click;
            _headerPanel.Paint += HeaderPanel_Paint;

            if (_uiHelperService != null)
            {
                _uiHelperService.DoubleBufDateGridView(_dgvVariables);
            }

            if (_colorTheme != null)
            {
                _colorTheme.InitColors();
            }

            ApplyModernStyling();
            RefreshVariables();
            ApplyDpiMetrics();
        }

        private void ApplyModernStyling()
        {
            Color mainBack = _colorTheme?.MainBack ?? SystemColors.Control;
            Color mainFore = _colorTheme?.MainFore ?? SystemColors.ControlText;
            bool darkTheme = _colorTheme?.IsDark(mainBack) ?? false;

            Color headerBack = darkTheme
                ? ControlPaint.Light(mainBack, 0.08f)
                : Color.FromArgb(248, 249, 251);
            Color gridBack = darkTheme
                ? ControlPaint.Light(mainBack, 0.035f)
                : Color.White;
            Color alternateBack = darkTheme
                ? ControlPaint.Light(mainBack, 0.06f)
                : Color.FromArgb(250, 251, 252);
            Color border = darkTheme
                ? ControlPaint.Light(mainBack, 0.18f)
                : Color.FromArgb(226, 232, 240);
            Color mutedFore = darkTheme
                ? Color.FromArgb(165, 165, 165)
                : Color.FromArgb(100, 116, 139);
            Color nameFore = darkTheme
                ? Color.FromArgb(86, 156, 214)
                : Color.FromArgb(0, 102, 153);
            Color selectionBack = darkTheme
                ? Color.FromArgb(38, 79, 120)
                : Color.FromArgb(204, 228, 247);
            Color selectionFore = darkTheme
                ? Color.FromArgb(241, 241, 241)
                : Color.FromArgb(30, 41, 59);

            BackColor = mainBack;
            ForeColor = mainFore;
            _headerPanel.BackColor = headerBack;
            _titleLabel.ForeColor = mainFore;
            _titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btClearVariables.BackColor = headerBack;
            _btClearVariables.ForeColor = mutedFore;
            _btClearVariables.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            _btClearVariables.FlatAppearance.BorderColor = border;
            _btClearVariables.FlatAppearance.MouseOverBackColor = darkTheme
                ? ControlPaint.Light(headerBack, 0.10f)
                : Color.FromArgb(235, 242, 249);
            _btClearVariables.FlatAppearance.MouseDownBackColor = selectionBack;
            _btClearVariables.Cursor = Cursors.Hand;

            _dgvVariables.BackgroundColor = gridBack;
            _dgvVariables.BackColor = gridBack;
            _dgvVariables.ForeColor = mainFore;
            _dgvVariables.GridColor = border;
            _dgvVariables.BorderStyle = BorderStyle.None;
            _dgvVariables.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvVariables.EnableHeadersVisualStyles = false;
            _dgvVariables.Font = new Font("Consolas", 9F, FontStyle.Regular);
            _dgvVariables.DefaultCellStyle.BackColor = gridBack;
            _dgvVariables.DefaultCellStyle.ForeColor = mainFore;
            _dgvVariables.DefaultCellStyle.SelectionBackColor = selectionBack;
            _dgvVariables.DefaultCellStyle.SelectionForeColor = selectionFore;
            _dgvVariables.DefaultCellStyle.NullValue = "null";
            _dgvVariables.AlternatingRowsDefaultCellStyle.BackColor = alternateBack;
            _dgvVariables.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBack;
            _dgvVariables.AlternatingRowsDefaultCellStyle.SelectionForeColor = selectionFore;

            _dgvVariables.ColumnHeadersDefaultCellStyle.BackColor = headerBack;
            _dgvVariables.ColumnHeadersDefaultCellStyle.ForeColor = mutedFore;
            _dgvVariables.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBack;
            _dgvVariables.ColumnHeadersDefaultCellStyle.SelectionForeColor = mutedFore;
            _dgvVariables.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _dgvVariables.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _dgvVariables.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            _nameColumn.DefaultCellStyle.ForeColor = nameFore;
            _nameColumn.DefaultCellStyle.SelectionBackColor = selectionBack;
            _nameColumn.DefaultCellStyle.SelectionForeColor = selectionFore;
            _valueColumn.DefaultCellStyle.ForeColor = mainFore;
            _valueColumn.DefaultCellStyle.SelectionBackColor = selectionBack;
            _valueColumn.DefaultCellStyle.SelectionForeColor = selectionFore;
        }

        private void HeaderPanel_Paint(object sender, PaintEventArgs e)
        {
            Color border = _colorTheme?.IsDark(_headerPanel.BackColor) == true
                ? ControlPaint.Light(_headerPanel.BackColor, 0.18f)
                : Color.FromArgb(226, 232, 240);

            using Pen pen = new(border);
            e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        }

        public void ApplyDpiMetrics()
        {
            if (_dgvVariables == null)
            {
                return;
            }

            int dpi = DeviceDpi;
            _headerPanel.Height = DpiScale.Scale(34, dpi);
            GridDpiMetrics.Apply(_dgvVariables, dpi, paddingLogical: 6);
            _dgvVariables.ColumnHeadersDefaultCellStyle.Padding = new Padding(
                DpiScale.Scale(8, dpi),
                DpiScale.Scale(1, dpi),
                DpiScale.Scale(8, dpi),
                DpiScale.Scale(1, dpi));
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDpiMetrics();
        }

        private void ViewModel_InsertVariableRequested(string value)
        {
            if (_baseWindow?.CurrentTB is null)
            {
                return;
            }

            _baseWindow.CurrentTB.InsertText(value);
            _baseWindow.CurrentTB.Focus();
        }

        public void RefreshVariables()
        {
            if (_dgvVariables == null || _viewModel == null)
            {
                return;
            }

            _viewModel.Refresh(_documentKeyProvider());
            SyncRowsFromViewModel();
        }

        private void Entries_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(SyncRowsFromViewModel);
            }
            else
            {
                SyncRowsFromViewModel();
            }
        }

        private void SyncRowsFromViewModel()
        {
            _variableRows.Clear();
            foreach (VariableEntry entry in _viewModel.Entries)
            {
                _variableRows.Add(new VariableRow(entry.Name, entry.IsSession));
            }

            _dgvVariables.RowCount = _variableRows.Count;
            _dgvVariables.Refresh();
        }

        private void DgvVariables_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= _variableRows.Count || _baseWindow?.CurrentTB == null)
                {
                    return;
                }

                _viewModel.InsertVariableCommand.Execute(_viewModel.Entries[e.RowIndex]);
                _baseWindow.CurrentTB.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Inserting a variable failed: {ex.GetType().Name}");
            }
        }

        private void DgvVariables_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _variableRows.Count)
            {
                return;
            }

            VariableRow row = _variableRows[e.RowIndex];
            if (e.ColumnIndex == 0)
            {
                e.ToolTipText = row.IsSession ? "Session variable" : "Global variable";
            }
            else if (e.ColumnIndex == 1)
            {
                e.ToolTipText = GetVariableValue(row) ?? "null";
            }
        }

        private void DgvVariables_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= _variableRows.Count)
                {
                    return;
                }

                VariableRow row = _variableRows[e.RowIndex];
                e.Value = e.ColumnIndex == 0
                    ? row.Key
                    : GetVariableValue(row) ?? "null";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Reading a variable failed: {ex.GetType().Name}");
            }
        }

        private string GetVariableValue(VariableRow row)
        {
            int rowIndex = _variableRows.IndexOf(row);
            return rowIndex >= 0 && rowIndex < _viewModel.Entries.Count
                ? _viewModel.Entries[rowIndex].Value
                : null;
        }

        private void BtClearVariables_Click(object sender, EventArgs e)
        {
            try
            {
                _viewModel.ClearGlobalsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing variables: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public int RowCount
        {
            get => _dgvVariables?.RowCount ?? 0;
            set
            {
                if (_dgvVariables != null && value != _dgvVariables.RowCount)
                {
                    RefreshVariables();
                }
            }
        }

        public DataGridViewSelectedRowCollection SelectedRows => _dgvVariables?.SelectedRows;

        public void ClearSelection()
        {
            _dgvVariables?.ClearSelection();
        }

        public void InvalidateRow(int rowIndex)
        {
            _dgvVariables?.InvalidateRow(rowIndex);
        }

        public new void Refresh()
        {
            RefreshVariables();
            base.Refresh();
        }
    }
}
