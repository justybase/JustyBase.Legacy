namespace DatabaseDataGridView.WinForms;

public class ListViewVirtualModeManager
{
    private readonly ListView _listView;
    private readonly List<object> _valuesInFilter;
    private ListViewItem[]? _myCache;
    private int _firstItem;

    public ListViewVirtualModeManager(ListView listView, List<object> valuesInFilter)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        _valuesInFilter = valuesInFilter ?? throw new ArgumentNullException(nameof(valuesInFilter));
    }

    public void Attach()
    {
        _listView.VirtualListSize = _valuesInFilter.Count;
        _listView.RetrieveVirtualItem += RetrieveVirtualItem;
        _listView.CacheVirtualItems += CacheVirtualItems;
        _listView.SearchForVirtualItem += SearchForVirtualItem;
    }

    private void RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
    {
        if (_myCache != null && e.ItemIndex >= _firstItem && e.ItemIndex < _firstItem + _myCache.Length)
        {
            e.Item = _myCache[e.ItemIndex - _firstItem];
        }
        else
        {
            var itemX = new ListViewItem(_valuesInFilter[e.ItemIndex].ToString() ?? string.Empty)
            {
                Tag = _valuesInFilter[e.ItemIndex]
            };
            e.Item = itemX;
        }
    }

    private void CacheVirtualItems(object? sender, CacheVirtualItemsEventArgs e)
    {
        if (_myCache != null && e.StartIndex >= _firstItem && e.EndIndex <= _firstItem + _myCache.Length)
        {
            return;
        }

        _firstItem = e.StartIndex;
        int length = e.EndIndex - e.StartIndex + 1;
        _myCache = new ListViewItem[length];

        for (int i = 0; i < length; i++)
        {
            var itemX = new ListViewItem(_valuesInFilter[i + _firstItem].ToString() ?? string.Empty)
            {
                Tag = _valuesInFilter[i + _firstItem]
            };
            _myCache[i] = itemX;
        }
    }

    private void SearchForVirtualItem(object? sender, SearchForVirtualItemEventArgs e)
    {
        string toSearch = e.Text ?? string.Empty;
        for (int i = e.StartIndex; i < _valuesInFilter.Count; i++)
        {
            if (_valuesInFilter[i].ToString()?.Contains(toSearch, StringComparison.OrdinalIgnoreCase) == true)
            {
                e.Index = i;
                break;
            }
        }
    }
}
