using DatabaseDataGridView.WinForms;
using JustData.Application.Schema;
using JustData.ViewModels.Explorer;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>DataGridView/focus adapter for the provider-neutral object explorer VM.</summary>
public sealed class MvvmObjectExplorerControl : UserControl
{
    private readonly ObjectExplorerViewModel _viewModel;
    private readonly ThemedDataGridView _grid;
    private static readonly int BaseRowHeight = 26;
    private static readonly int HeaderHeight = 28;

    // Shared fonts — created once per app domain, never disposed (safe for app-lifetime singletons).
    private static readonly Font _gridFont = new Font("Segoe UI", 9F, FontStyle.Regular);
    private static readonly Font _headerFont = new Font("Segoe UI", 9F, FontStyle.Bold);

    // Static colors derived from system theme for basic dark-mode awareness
    private static readonly Color _headerBack = SystemColors.ControlLight;
    private static readonly Color _headerFore = SystemColors.ControlText;
    private static readonly Color _cellBack = SystemColors.Window;
    private static readonly Color _cellFore = SystemColors.WindowText;
    private static readonly Color _altRowBack = SystemColors.Control;
    private static readonly Color _gridLine = SystemColors.ActiveBorder;
    private static readonly Color _kindFore = SystemColors.GrayText;
    private static readonly Color _selectionBack = SystemColors.Highlight;
    private static readonly Color _selectionFore = SystemColors.HighlightText;

    public MvvmObjectExplorerControl(ObjectExplorerViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Name = "mvvmObjectExplorerControl";
        Dock = DockStyle.Fill;

        _grid = new ThemedDataGridView();

        // Enable double-buffering via reflection (the property is protected on DataGridView).
        typeof(Control).InvokeMember("DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, _grid, new object[] { true });

        ((ISupportInitialize)_grid).BeginInit();
        _grid.Name = "dgvObjectExplorer";
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.VirtualMode = true;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _grid.RowTemplate.Height = BaseRowHeight;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = HeaderHeight;
        _grid.BackgroundColor = _cellBack;
        _grid.Font = _gridFont;
        _grid.EnableHeadersVisualStyles = false;

        // Header styling
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _headerBack,
            ForeColor = _headerFore,
            Font = _headerFont,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 4, 0),
            SelectionBackColor = _headerBack,
            SelectionForeColor = _headerFore
        };

        // Default cell style
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = _gridFont,
            ForeColor = _cellFore,
            BackColor = _cellBack,
            SelectionBackColor = _selectionBack,
            SelectionForeColor = _selectionFore,
            Padding = new Padding(4, 1, 4, 1),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _altRowBack
        };
        ((ISupportInitialize)_grid).EndInit();

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "clObjectName", HeaderText = "Object", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "clObjectKind", HeaderText = "Kind", Width = 90, DefaultCellStyle = { ForeColor = _kindFore } });
        _grid.CellValueNeeded += OnCellValueNeeded;
        _grid.CellClick += (_, args) =>
        {
            if (args.RowIndex >= 0 && args.RowIndex < _viewModel.References.Count)
                _viewModel.SelectedReference = _viewModel.References[args.RowIndex];
        };
        _grid.RowCount = _viewModel.References.Count;
        _viewModel.References.CollectionChanged += OnReferencesChanged;
        Controls.Add(_grid);
    }

    private bool _gridRefreshPending;

    private void OnReferencesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Batch multiple Clear()+Add()+Add()+... into a single grid update.
        // Without this, every CollectionChanged event triggers a full layout
        // recalculation, causing visible lag during tab switching.
        if (_gridRefreshPending || IsDisposed || Disposing)
            return;

        _gridRefreshPending = true;
        if (!IsHandleCreated)
        {
            RefreshGrid();
            return;
        }

        try
        {
            BeginInvoke(new Action(RefreshGrid));
        }
        catch (InvalidOperationException)
        {
            _gridRefreshPending = false;
        }
    }

    private void RefreshGrid()
    {
        _gridRefreshPending = false;
        if (IsDisposed || Disposing)
            return;

        _grid.RowCount = _viewModel.References.Count;
        _grid.Invalidate();
    }

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _viewModel.References.Count)
            return;

        var reference = _viewModel.References[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => reference.Name,
            1 => reference.Kind.ToString(),
            _ => null
        };
    }

    public DataGridView DataGridView => _grid;
    public ObjectExplorerViewModel ViewModel => _viewModel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.References.CollectionChanged -= OnReferencesChanged;
        }

        base.Dispose(disposing);
    }

    public async Task RebuildAsync(string text, string? connectionName = null, CancellationToken cancellationToken = default)
    {
        _viewModel.SqlText = text ?? string.Empty;
        await _viewModel.RefreshAsync(connectionName, cancellationToken);
        // RowCount is updated via CollectionChanged handler
    }

    public void ApplyDpiMetrics()
    {
        // DataGridView owns DPI scaling through the WinForms adapter/theme pipeline.
    }
}
