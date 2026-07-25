using AppBase.Common.Enums;

namespace AppBase.Common;

public class ExplorerItem
{
    public string? Title;
    public int Position;
    public ExplorerItemType type;
    public string? Database { get; set; }
    public string? Schema { get; set; }
}


public class ExplorerItemComparer : IComparer<ExplorerItem>
{
    public int Compare(ExplorerItem? x, ExplorerItem? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        return x.Position.CompareTo(y.Position);
    }
}

/// <summary>
/// Compares ExplorerItems for sorting by Database, Type, and Name
/// </summary>
public class ExplorerItemSortComparer : IComparer<ExplorerItem>
{
    private readonly SortOrder _order;
    private readonly ExplorerItemSortBy _sortBy;

    public ExplorerItemSortComparer(ExplorerItemSortBy sortBy = ExplorerItemSortBy.Database, SortOrder order = SortOrder.Ascending)
    {
        _sortBy = sortBy;
        _order = order;
    }

    public int Compare(ExplorerItem? x, ExplorerItem? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return _order == SortOrder.Ascending ? -1 : 1;
        if (y == null) return _order == SortOrder.Ascending ? 1 : -1;

        int result = 0;

        switch (_sortBy)
        {
            case ExplorerItemSortBy.Database:
                result = CompareByDatabase(x, y);
                break;
            case ExplorerItemSortBy.Type:
                result = CompareByType(x, y);
                break;
            case ExplorerItemSortBy.Name:
                result = CompareByName(x, y);
                break;
            case ExplorerItemSortBy.Position:
                result = x.Position.CompareTo(y.Position);
                break;
            default:
                result = CompareByDefault(x, y);
                break;
        }

        return _order == SortOrder.Ascending ? result : -result;
    }

    /// <summary>
    /// Default sort: Database → Type → Name
    /// </summary>
    private int CompareByDefault(ExplorerItem x, ExplorerItem y)
    {
        // Sort by Database first
        int dbCompare = string.Compare(x.Database ?? "", y.Database ?? "", StringComparison.OrdinalIgnoreCase);
        if (dbCompare != 0) return dbCompare;

        // Then by Type
        int typeCompare = x.type.CompareTo(y.type);
        if (typeCompare != 0) return typeCompare;

        // Finally by Name
        return string.Compare(x.Title?.TrimStart() ?? "", y.Title?.TrimStart() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private int CompareByDatabase(ExplorerItem x, ExplorerItem y)
    {
        int dbCompare = string.Compare(x.Database ?? "", y.Database ?? "", StringComparison.OrdinalIgnoreCase);
        if (dbCompare != 0) return dbCompare;

        int typeCompare = x.type.CompareTo(y.type);
        if (typeCompare != 0) return typeCompare;

        return string.Compare(x.Title?.TrimStart() ?? "", y.Title?.TrimStart() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private int CompareByType(ExplorerItem x, ExplorerItem y)
    {
        int typeCompare = x.type.CompareTo(y.type);
        if (typeCompare != 0) return typeCompare;

        int dbCompare = string.Compare(x.Database ?? "", y.Database ?? "", StringComparison.OrdinalIgnoreCase);
        if (dbCompare != 0) return dbCompare;

        return string.Compare(x.Title?.TrimStart() ?? "", y.Title?.TrimStart() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private int CompareByName(ExplorerItem x, ExplorerItem y)
    {
        int nameCompare = string.Compare(x.Title?.TrimStart() ?? "", y.Title?.TrimStart() ?? "", StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0) return nameCompare;

        int typeCompare = x.type.CompareTo(y.type);
        if (typeCompare != 0) return typeCompare;

        return string.Compare(x.Database ?? "", y.Database ?? "", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Determines the primary sort criteria for ExplorerItems
/// </summary>
public enum ExplorerItemSortBy
{
    /// <summary>Sort by Database, then Type, then Name (default)</summary>
    Database,
    /// <summary>Sort by Type, then Database, then Name</summary>
    Type,
    /// <summary>Sort by Name, then Type, then Database</summary>
    Name,
    /// <summary>Sort by Position (original order)</summary>
    Position
}

