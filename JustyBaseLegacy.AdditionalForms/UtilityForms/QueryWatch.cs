using AppBase.Common;
using JustData.Application.QueryWatch;
using JustData.ViewModels.QueryWatch;
using System.ComponentModel;
using System.Drawing;

namespace JustyBaseLegacy.UI;

public partial class QueryWatch : Form
{
    private const int AutoRefreshIntervalMs = 30_000;
    private const string DropColumnName = "Drop";
    private const int MaxSummaryColumns = 7;

    private static readonly string[] SummaryColumnPriority =
    [
        "ID", "APPLICATION_HANDLE", "pid",
        "ELAPSED_SECS", "ELAPSED",
        "USERNAME", "usename", "SESSION_AUTH_ID",
        "DBNAME", "datname",
        "STATUS", "state",
        "COMMAND", "query", "STMT_TEXT", "QS_SQL",
        "CONNTIME",
    ];

    private static readonly string[] ElapsedColumnNames =
    [
        "ELAPSED_SECS", "ELAPSED", "elapsed_secs", "elapsed",
    ];

    private readonly QueryWatchViewModel _viewModel;
    private readonly ILogger _logger;
    private readonly System.Windows.Forms.Timer _autoRefreshTimer;
    private bool _columnsBuilt;
    private bool _suppressAutoRefreshToggle;
    private bool _splitDistanceInitialized;
    private IReadOnlyList<string> _summaryColumns = [];

    public QueryWatch(
        QueryWatchViewModel viewModel,
        Action<Form> doColorize,
        Action<DataGridView> doubleBuff,
        ILogger logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeComponent();
        ArgumentNullException.ThrowIfNull(doColorize);
        ArgumentNullException.ThrowIfNull(doubleBuff);
        doColorize(this);
        doubleBuff(queryWatchDataGridView);
        doubleBuff(detailsDataGridView);
        SyncHeaderSelectionColors(queryWatchDataGridView);
        SyncHeaderSelectionColors(detailsDataGridView);

        _autoRefreshTimer = new System.Windows.Forms.Timer
        {
            Interval = AutoRefreshIntervalMs,
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

        buttonRefresh.Click += async (_, _) => await RefreshNowAsync();
        checkBoxAutoRefresh.CheckedChanged += CheckBoxAutoRefresh_CheckedChanged;
        queryWatchDataGridView.CellContentClick += QueryWatchDataGridView_CellContentClick;
        queryWatchDataGridView.CellFormatting += QueryWatchDataGridView_CellFormatting;
        queryWatchDataGridView.SelectionChanged += QueryWatchDataGridView_SelectionChanged;
        Load += QueryWatch_Load;
        FormClosed += QueryWatch_FormClosed;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.Rows.CollectionChanged += (_, _) => RebuildGridRows();

        SyncFromViewModel();
        ShowDetails(null);
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

    public Task RefreshNowAsync() => _viewModel.RefreshAsync();

    private async void QueryWatch_Load(object? sender, EventArgs e)
    {
        ApplyDpiLayout();
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Query Watch initial refresh failed: {ex.GetType().Name}");
        }
    }

    private void QueryWatch_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _autoRefreshTimer.Stop();
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void CheckBoxAutoRefresh_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressAutoRefreshToggle)
        {
            return;
        }

        _viewModel.AutoRefreshEnabled = checkBoxAutoRefresh.Checked;
        if (_viewModel.AutoRefreshEnabled)
        {
            _autoRefreshTimer.Start();
        }
        else
        {
            _autoRefreshTimer.Stop();
        }
    }

    private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!_viewModel.AutoRefreshEnabled || _viewModel.IsBusy)
        {
            return;
        }

        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Query Watch auto-refresh failed: {ex.GetType().Name}");
        }
    }

    private async void QueryWatchDataGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (queryWatchDataGridView.Columns[e.ColumnIndex].Name != DropColumnName)
        {
            return;
        }

        if (e.RowIndex >= _viewModel.Rows.Count)
        {
            return;
        }

        QueryWatchRow row = _viewModel.Rows[e.RowIndex];
        string? dropSql = _viewModel.RequestDropSession(row);
        if (dropSql is null)
        {
            return;
        }

        string summary = BuildDropSummary(row);
        DialogResult confirm = _logger.MessageBox_Show(
            this,
            $"Drop this session?\n\n{summary}",
            "Confirm drop session",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _viewModel.DropSessionAsync(row);
        }
        catch (Exception ex)
        {
            _logger.MessageBox_Show(this, ex.Message, "Drop session failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void QueryWatchDataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (queryWatchDataGridView.Columns[e.ColumnIndex].Name != DropColumnName)
        {
            return;
        }

        if (e.RowIndex >= _viewModel.Rows.Count)
        {
            return;
        }

        if (!_viewModel.Rows[e.RowIndex].CanDrop)
        {
            e.Value = "";
            e.FormattingApplied = true;
        }
    }

    private void QueryWatchDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        ShowDetails(GetSelectedRow());
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ViewModel_PropertyChanged(sender, e));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(QueryWatchViewModel.IsBusy):
            case nameof(QueryWatchViewModel.ErrorMessage):
            case nameof(QueryWatchViewModel.LastRefreshed):
            case nameof(QueryWatchViewModel.ConnectionLabel):
            case nameof(QueryWatchViewModel.ColumnNames):
                SyncFromViewModel();
                if (e.PropertyName is nameof(QueryWatchViewModel.ColumnNames)
                    or nameof(QueryWatchViewModel.IsBusy))
                {
                    RebuildGridRows();
                }
                break;
            case nameof(QueryWatchViewModel.AutoRefreshEnabled):
                _suppressAutoRefreshToggle = true;
                checkBoxAutoRefresh.Checked = _viewModel.AutoRefreshEnabled;
                _suppressAutoRefreshToggle = false;
                break;
        }
    }

    private void SyncFromViewModel()
    {
        labelConnection.Text = string.IsNullOrWhiteSpace(_viewModel.ConnectionLabel)
            ? "Connection"
            : _viewModel.ConnectionLabel;
        buttonRefresh.Enabled = !_viewModel.IsBusy;

        if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
        {
            labelStatus.ForeColor = Color.FromArgb(220, 53, 69);
            labelStatus.Text = _viewModel.ErrorMessage;
        }
        else if (_viewModel.IsBusy)
        {
            labelStatus.ForeColor = Color.FromArgb(108, 117, 125);
            labelStatus.Text = "Refreshing…";
        }
        else if (_viewModel.LastRefreshed is DateTime refreshed)
        {
            labelStatus.ForeColor = Color.FromArgb(108, 117, 125);
            labelStatus.Text = $"Last refreshed: {refreshed:HH:mm:ss}  ·  {_viewModel.Rows.Count} session(s)";
        }
        else
        {
            labelStatus.ForeColor = Color.FromArgb(108, 117, 125);
            labelStatus.Text = "Not refreshed";
        }

        EnsureColumns();
    }

    private void EnsureColumns()
    {
        if (_viewModel.ColumnNames.Count == 0)
        {
            if (_columnsBuilt)
            {
                queryWatchDataGridView.Columns.Clear();
                _columnsBuilt = false;
                _summaryColumns = [];
                ShowDetails(null);
            }

            return;
        }

        IReadOnlyList<string> summary = BuildSummaryColumns(_viewModel.ColumnNames);
        if (_columnsBuilt
            && _summaryColumns.Count == summary.Count
            && _summaryColumns.SequenceEqual(summary, StringComparer.Ordinal))
        {
            ApplyHeaderStyles(queryWatchDataGridView);
            return;
        }

        _summaryColumns = summary;
        queryWatchDataGridView.Columns.Clear();

        queryWatchDataGridView.Columns.Add(new DataGridViewButtonColumn
        {
            Name = DropColumnName,
            HeaderText = DropColumnName,
            Text = "Drop",
            UseColumnTextForButtonValue = true,
            FlatStyle = FlatStyle.Flat,
            Width = 72,
            MinimumWidth = 64,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });

        foreach (string columnName in _summaryColumns)
        {
            bool isWide = columnName.Contains("SQL", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("COMMAND", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("query", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("STMT_TEXT", StringComparison.OrdinalIgnoreCase);

            queryWatchDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = columnName,
                HeaderText = IsElapsedColumn(columnName) ? "ELAPSED" : columnName,
                ReadOnly = true,
                AutoSizeMode = isWide
                    ? DataGridViewAutoSizeColumnMode.Fill
                    : DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = isWide ? 160 : 80,
                FillWeight = isWide ? 200 : 100,
            });
        }

        ApplyHeaderStyles(queryWatchDataGridView);
        ApplyHeaderStyles(detailsDataGridView);
        _columnsBuilt = true;
    }

    private void RebuildGridRows()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RebuildGridRows);
            return;
        }

        EnsureColumns();
        queryWatchDataGridView.Rows.Clear();
        if (!_columnsBuilt)
        {
            ShowDetails(null);
            return;
        }

        foreach (QueryWatchRow row in OrderByElapsedDescending(_viewModel.Rows))
        {
            object?[] cells = new object[_summaryColumns.Count + 1];
            cells[0] = row.CanDrop ? "Drop" : "";
            for (int i = 0; i < _summaryColumns.Count; i++)
            {
                string name = _summaryColumns[i];
                cells[i + 1] = row.Values.TryGetValue(name, out object? value)
                    ? FormatCellValue(value)
                    : DBNull.Value;
            }

            int rowIndex = queryWatchDataGridView.Rows.Add(cells);
            DataGridViewRow gridRow = queryWatchDataGridView.Rows[rowIndex];
            gridRow.Tag = row;
            DataGridViewCell dropCell = gridRow.Cells[DropColumnName];
            dropCell.ReadOnly = !row.CanDrop;
            if (!row.CanDrop)
            {
                dropCell.Style.ForeColor = Color.FromArgb(173, 181, 189);
                dropCell.Style.SelectionForeColor = Color.FromArgb(173, 181, 189);
            }
        }

        if (queryWatchDataGridView.Rows.Count > 0)
        {
            queryWatchDataGridView.ClearSelection();
            queryWatchDataGridView.Rows[0].Selected = true;
            if (queryWatchDataGridView.Columns.Count > 1)
            {
                queryWatchDataGridView.CurrentCell = queryWatchDataGridView.Rows[0].Cells[1];
            }
        }

        ShowDetails(GetSelectedRow());
    }

    private QueryWatchRow? GetSelectedRow()
    {
        if (queryWatchDataGridView.SelectedRows.Count == 0)
        {
            return null;
        }

        return queryWatchDataGridView.SelectedRows[0].Tag as QueryWatchRow;
    }

    private void ShowDetails(QueryWatchRow? row)
    {
        detailsDataGridView.Rows.Clear();
        if (row is null)
        {
            labelDetails.Text = "Session details";
            return;
        }

        string? id = Pick(row, "ID", "APPLICATION_HANDLE", "pid", "QS_SESSIONID");
        labelDetails.Text = id is null ? "Session details" : $"Session details · {id}";

        foreach (KeyValuePair<string, object?> pair in row.Values)
        {
            detailsDataGridView.Rows.Add(pair.Key, FormatCellValue(pair.Value) ?? "");
        }

        ApplyHeaderStyles(detailsDataGridView);
        foreach (DataGridViewRow detailsRow in detailsDataGridView.Rows)
        {
            string? property = detailsRow.Cells[0].Value?.ToString();
            if (property is not null
                && (property.Contains("SQL", StringComparison.OrdinalIgnoreCase)
                    || property.Equals("COMMAND", StringComparison.OrdinalIgnoreCase)
                    || property.Equals("query", StringComparison.OrdinalIgnoreCase)
                    || property.Equals("STMT_TEXT", StringComparison.OrdinalIgnoreCase)))
            {
                detailsRow.Height = Math.Max(56, detailsRow.GetPreferredHeight(1, DataGridViewAutoSizeRowMode.AllCellsExceptHeader, true));
            }
        }
    }

    private static IReadOnlyList<string> BuildSummaryColumns(IReadOnlyList<string> allColumns)
    {
        var result = new List<string>(MaxSummaryColumns);
        foreach (string candidate in SummaryColumnPriority)
        {
            string? match = allColumns.FirstOrDefault(c =>
                c.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (match is null || result.Contains(match, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(match);
            if (result.Count >= MaxSummaryColumns)
            {
                break;
            }
        }

        if (result.Count == 0)
        {
            result.AddRange(allColumns.Take(MaxSummaryColumns));
        }

        return result;
    }

    private static bool IsElapsedColumn(string columnName) =>
        ElapsedColumnNames.Any(name => name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<QueryWatchRow> OrderByElapsedDescending(IEnumerable<QueryWatchRow> rows) =>
        rows
            .OrderByDescending(GetElapsedSeconds)
            .ThenBy(GetConnectionTime);

    private static double GetElapsedSeconds(QueryWatchRow row)
    {
        foreach (string key in ElapsedColumnNames)
        {
            if (TryGetNumeric(row, key, out double value))
            {
                return value;
            }
        }

        return double.NegativeInfinity;
    }

    private static DateTime GetConnectionTime(QueryWatchRow row)
    {
        foreach (string key in new[] { "CONNTIME", "backend_start", "query_start", "QS_TSTART" })
        {
            if (!row.Values.TryGetValue(key, out object? value) || value is null or DBNull)
            {
                continue;
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (DateTime.TryParse(
                    value.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Globalization.DateTimeStyles.AssumeLocal,
                    out DateTime parsed)
                || DateTime.TryParse(
                    value.ToString(),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeLocal,
                    out parsed))
            {
                return parsed;
            }
        }

        return DateTime.MaxValue;
    }

    private static bool TryGetNumeric(QueryWatchRow row, string key, out double value)
    {
        value = 0;
        if (!row.Values.TryGetValue(key, out object? raw) || raw is null or DBNull)
        {
            return false;
        }

        if (raw is IConvertible convertible)
        {
            try
            {
                value = Convert.ToDouble(convertible, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                // Fall through to string parse.
            }
        }

        return double.TryParse(
                raw.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out value)
            || double.TryParse(
                raw.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture,
                out value);
    }

    private static void SyncHeaderSelectionColors(DataGridView grid)
    {
        DataGridViewCellStyle header = grid.ColumnHeadersDefaultCellStyle;
        header.SelectionBackColor = header.BackColor;
        header.SelectionForeColor = header.ForeColor;

        DataGridViewCellStyle rowHeader = grid.RowHeadersDefaultCellStyle;
        rowHeader.BackColor = header.BackColor;
        rowHeader.ForeColor = header.ForeColor;
        rowHeader.SelectionBackColor = header.BackColor;
        rowHeader.SelectionForeColor = header.ForeColor;

        grid.EnableHeadersVisualStyles = false;
        grid.TopLeftHeaderCell.Style.BackColor = header.BackColor;
        grid.TopLeftHeaderCell.Style.ForeColor = header.ForeColor;
        grid.TopLeftHeaderCell.Style.SelectionBackColor = header.BackColor;
        grid.TopLeftHeaderCell.Style.SelectionForeColor = header.ForeColor;
    }

    private static void ApplyHeaderStyles(DataGridView grid)
    {
        SyncHeaderSelectionColors(grid);
        DataGridViewCellStyle header = grid.ColumnHeadersDefaultCellStyle;
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.HeaderCell.Style.BackColor = header.BackColor;
            column.HeaderCell.Style.ForeColor = header.ForeColor;
            column.HeaderCell.Style.SelectionBackColor = header.BackColor;
            column.HeaderCell.Style.SelectionForeColor = header.ForeColor;
            column.HeaderCell.Style.Font = header.Font;
            column.HeaderCell.Style.Alignment = header.Alignment;
        }
    }

    private static object? FormatCellValue(object? value)
    {
        if (value is null or DBNull)
        {
            return null;
        }

        string text = value.ToString() ?? "";
        if (text.Length > 500)
        {
            return text[..500] + "…";
        }

        return text;
    }

    private static string BuildDropSummary(QueryWatchRow row)
    {
        string? id = Pick(row, "ID", "APPLICATION_HANDLE", "pid", "QS_SESSIONID");
        string? user = Pick(row, "USERNAME", "usename", "SESSION_AUTH_ID");
        string? sql = Pick(row, "QS_SQL", "query", "STMT_TEXT", "COMMAND");

        var parts = new List<string>();
        if (id is not null)
        {
            parts.Add($"Session: {id}");
        }

        if (user is not null)
        {
            parts.Add($"User: {user}");
        }

        if (sql is not null)
        {
            string clipped = sql.Length > 180 ? sql[..180] + "…" : sql;
            parts.Add($"SQL: {clipped}");
        }

        if (parts.Count == 0 && row.DropSessionSql is not null)
        {
            parts.Add(row.DropSessionSql);
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static string? Pick(QueryWatchRow row, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (row.Values.TryGetValue(key, out object? value)
                && value is not null
                && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString();
            }
        }

        return null;
    }

    private void ApplyDpiLayout()
    {
        int dpi = DeviceDpi;
        int margin = DpiScale.Scale(20, dpi);
        int controlHeight = DpiScale.Scale(32, dpi);
        int buttonWidth = DpiScale.Scale(120, dpi);

        panelHeader.Padding = new Padding(margin);
        panelHeader.Height = DpiScale.Scale(112, dpi);
        splitMain.Panel1.Padding = new Padding(margin, 0, margin, DpiScale.Scale(8, dpi));
        splitMain.Panel2.Padding = new Padding(margin, 0, margin, margin);

        labelTitle.Location = new Point(0, DpiScale.Scale(4, dpi));
        labelConnection.Location = new Point(labelTitle.Right + DpiScale.Scale(16, dpi), DpiScale.Scale(10, dpi));

        buttonRefresh.SetBounds(0, DpiScale.Scale(44, dpi), buttonWidth, controlHeight);
        checkBoxAutoRefresh.Location = new Point(buttonRefresh.Right + DpiScale.Scale(16, dpi), DpiScale.Scale(50, dpi));
        labelStatus.Location = new Point(checkBoxAutoRefresh.Right + DpiScale.Scale(20, dpi), DpiScale.Scale(52, dpi));

        if (!_splitDistanceInitialized && splitMain.Height > DpiScale.Scale(200, dpi))
        {
            splitMain.SplitterDistance = Math.Max(
                DpiScale.Scale(180, dpi),
                (int)(splitMain.Height * 0.55));
            _splitDistanceInitialized = true;
        }
    }
}
