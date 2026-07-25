using CommunityToolkit.Mvvm.ComponentModel;
using JustData.Application.Schema;
using System.Collections.ObjectModel;

namespace JustData.ViewModels.Explorer;

public sealed class ExplorerNodeViewModel : ObservableObject
{
    public const int InitialChildBatchSize = 100;
    private bool _isExpanded;
    private bool _isLoading;
    private IReadOnlyList<SchemaNode> _pendingChildren = [];
    private int _nextPendingChildIndex;

    public ExplorerNodeViewModel(SchemaNode model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public SchemaNode Model { get; }
    public string Id => Model.Id;
    public string Name => Model.Name;
    public SchemaNodeKind Kind => Model.Kind;
    public SchemaPath Path => Model.Path;
    public bool HasChildren => Model.HasChildren;
    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = [];
    public bool ChildrenLoaded { get; private set; }
    public bool HasPendingChildren => _nextPendingChildIndex < _pendingChildren.Count;

    /// <summary>
    /// Raised after a UI-safe child batch has been appended. The WinForms adapter
    /// uses the event to add only the new TreeNodes instead of rebuilding a large
    /// branch on every batch.
    /// </summary>
    public event EventHandler<ExplorerChildrenAppendedEventArgs>? ChildrenAppended;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        internal set => SetProperty(ref _isLoading, value);
    }

    internal void BeginChildrenLoad(IReadOnlyList<SchemaNode> children)
    {
        Children.Clear();
        _pendingChildren = children;
        _nextPendingChildIndex = 0;
        ChildrenLoaded = false;
        OnPropertyChanged(nameof(ChildrenLoaded));
        OnPropertyChanged(nameof(HasPendingChildren));
    }

    internal void AppendNextChildrenBatch(int batchSize)
    {
        if (batchSize <= 0 || !HasPendingChildren)
            return;

        int take = Math.Min(batchSize, _pendingChildren.Count - _nextPendingChildIndex);
        var appended = new ExplorerNodeViewModel[take];
        for (int index = 0; index < take; index++)
        {
            var child = new ExplorerNodeViewModel(_pendingChildren[_nextPendingChildIndex++]);
            Children.Add(child);
            appended[index] = child;
        }

        ChildrenAppended?.Invoke(this, new ExplorerChildrenAppendedEventArgs(appended));
        OnPropertyChanged(nameof(HasPendingChildren));
    }

    internal void CompleteChildrenLoad()
    {
        _pendingChildren = [];
        _nextPendingChildIndex = 0;
        ChildrenLoaded = true;
        OnPropertyChanged(nameof(ChildrenLoaded));
        OnPropertyChanged(nameof(HasPendingChildren));
    }
}

public sealed class ExplorerChildrenAppendedEventArgs(IReadOnlyList<ExplorerNodeViewModel> children) : EventArgs
{
    public IReadOnlyList<ExplorerNodeViewModel> Children { get; } = children;
}
