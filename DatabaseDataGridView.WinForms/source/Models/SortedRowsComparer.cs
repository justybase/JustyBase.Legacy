namespace DatabaseDataGridView.WinForms.Models;

internal sealed class SortedRowsComparer : IComparer<object[]>
{
    readonly List<(int index, SortInfo sortInfo)> _sortInfoList;
    public SortedRowsComparer(List<(int index, SortInfo sortInfo)> sortInfoList)
    {
        _sortInfoList = sortInfoList;
    }

    public int Compare(object[]? x, object[]? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return 1;
        if (y is null) return -1;

        foreach (var (i, sortInfo) in _sortInfoList)
        {
            if (x[i] is null && y[i] is null)
                continue;
            if (x[i] is null && y[i] is not null)
                return 1;
            if (x[i] is not null && y[i] is null)
                return -1;

            int cmp = 0;

            if (x[i] is IComparable cpm1 && y[i] is IComparable cpm2)
            {
                cmp = cpm1.CompareTo(cpm2);
            }

            if (cmp != 0)
            {
                if (sortInfo == SortInfo.DESC)
                {
                    cmp = -cmp;
                }
                return cmp;
            }
        }

        return 0;
    }
}
