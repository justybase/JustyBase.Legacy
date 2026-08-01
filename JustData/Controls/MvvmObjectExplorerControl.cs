using JustData.Application.Schema;
using JustData.ViewModels.Explorer;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>Tree-based, source-backed SQL Outline. The old flat reference API remains for callers.</summary>
public sealed class MvvmObjectExplorerControl : UserControl
{
    private readonly ObjectExplorerViewModel _viewModel;
    private readonly TreeView _tree;
    private readonly ImageList _icons;
    private readonly Dictionary<OutlineNodeKind, int> _iconByKind = [];

    public event Action<SchemaReference>? ReferenceActivated;

    public MvvmObjectExplorerControl(ObjectExplorerViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Name = "mvvmObjectExplorerControl";
        Dock = DockStyle.Fill;
        _icons = CreateIcons();
        _tree = new TreeView
        {
            Name = "outlineTreeView", Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
            HideSelection = false, ShowLines = false, ShowNodeToolTips = true,
            FullRowSelect = true, ImageList = _icons, Indent = 16,
            Font = new Font("Segoe UI", 9F), BackColor = SystemColors.Window, ForeColor = SystemColors.WindowText
        };
        _tree.NodeMouseClick += (_, e) => ActivateNode(e.Node);
        _tree.NodeMouseDoubleClick += (_, e) => ActivateNode(e.Node);
        _tree.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && _tree.SelectedNode is not null)
            {
                ActivateNode(_tree.SelectedNode); e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        Controls.Add(_tree);
        _viewModel.OutlineNodes.CollectionChanged += OnOutlineNodesChanged;
    }

    public TreeView OutlineTreeView => _tree;
    public ObjectExplorerViewModel ViewModel => _viewModel;

    // Compatibility seam for existing tests and keyboard navigation. References are still flat.
    public bool ActivateReference(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModel.References.Count) return false;
        SchemaReference reference = _viewModel.References[rowIndex];
        _viewModel.SelectedReference = reference;
        ReferenceActivated?.Invoke(reference);
        return true;
    }

    public bool SelectNodeByName(string name)
    {
        TreeNode? found = FindNode(_tree.Nodes, name);
        if (found is null) return false;
        _tree.SelectedNode = found; found.EnsureVisible(); return true;
    }

    private bool _rebuildPending;
    private void OnOutlineNodesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_rebuildPending || IsDisposed || Disposing) return;
        _rebuildPending = true;
        if (!IsHandleCreated) { RebuildTree(); return; }
        try { BeginInvoke(new Action(RebuildTree)); }
        catch (InvalidOperationException) { _rebuildPending = false; }
    }

    private void RebuildTree()
    {
        _rebuildPending = false;
        if (IsDisposed || Disposing) return;
        var expanded = _tree.Nodes.Cast<TreeNode>().SelectMany(Flatten).Where(n => n.IsExpanded)
            .Select(n => ((OutlineNode)n.Tag!).Id).ToHashSet(StringComparer.Ordinal);
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            foreach (OutlineNode node in _viewModel.OutlineNodes) _tree.Nodes.Add(CreateNode(node, expanded));
        }
        finally { _tree.EndUpdate(); }
    }

    private TreeNode CreateNode(OutlineNode model, ISet<string> expanded)
    {
        string suffix = string.IsNullOrWhiteSpace(model.Alias) ? "" : $"  ({model.Alias})";
        var node = new TreeNode(model.Name + suffix)
        {
            Tag = model, ImageIndex = Icon(model.Kind), SelectedImageIndex = Icon(model.Kind), ToolTipText =
                $"{model.Kind} · offset {model.Position}" + (model.IsIncomplete ? " · incomplete parse" : "")
        };
        foreach (OutlineNode child in model.Children) node.Nodes.Add(CreateNode(child, expanded));
        if (expanded.Contains(model.Id)) node.Expand();
        return node;
    }

    private void ActivateNode(TreeNode node)
    {
        if (node.Tag is not OutlineNode outline) return;
        SchemaReference reference = new(outline.Name, MapKind(outline.Kind), outline.Position, outline.Database, outline.Schema);
        _viewModel.SelectedReference = reference;
        ReferenceActivated?.Invoke(reference);
    }

    private static SchemaNodeKind MapKind(OutlineNodeKind kind) => kind switch
    {
        OutlineNodeKind.Table or OutlineNodeKind.TempTable => SchemaNodeKind.Table,
        OutlineNodeKind.View => SchemaNodeKind.View,
        OutlineNodeKind.Procedure => SchemaNodeKind.Procedure,
        OutlineNodeKind.Cte => SchemaNodeKind.Alias,
        _ => SchemaNodeKind.Unknown
    };

    private int Icon(OutlineNodeKind kind) => _iconByKind.TryGetValue(kind, out int index) ? index : 0;
    private static IEnumerable<TreeNode> Flatten(TreeNode node) => new[] { node }.Concat(node.Nodes.Cast<TreeNode>().SelectMany(Flatten));
    private static TreeNode? FindNode(TreeNodeCollection nodes, string name)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Text.StartsWith(name, StringComparison.OrdinalIgnoreCase)) return node;
            TreeNode? child = FindNode(node.Nodes, name); if (child is not null) return child;
        }
        return null;
    }

    private ImageList CreateIcons()
    {
        var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        foreach (OutlineNodeKind kind in Enum.GetValues<OutlineNodeKind>())
        {
            using var bitmap = new Bitmap(16, 16);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(kind == OutlineNodeKind.Warning ? Color.DarkOrange : Color.SteelBlue);
            graphics.FillEllipse(brush, 2, 2, 12, 12);
            _iconByKind[kind] = list.Images.Count; list.Images.Add((Bitmap)bitmap.Clone());
        }
        return list;
    }

    public async Task RebuildAsync(string text, string? connectionName = null, CancellationToken cancellationToken = default)
    {
        _viewModel.SqlText = text ?? string.Empty;
        await _viewModel.RefreshAsync(connectionName, cancellationToken);
    }

    public void ApplyDpiMetrics() { }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _viewModel.OutlineNodes.CollectionChanged -= OnOutlineNodesChanged; _icons.Dispose(); _tree.Dispose(); }
        base.Dispose(disposing);
    }
}
