using AppBase.Common;
using AppBase.Common.Enums;

namespace AppBase.Tests.Models;

public sealed class ExplorerItemSortComparerTests
{
    // ── ExplorerItemComparer ──

    [Fact]
    public void ExplorerItemComparer_same_reference_returns_zero()
    {
        var item = new ExplorerItem { Position = 5 };
        var comparer = new ExplorerItemComparer();
        Assert.Equal(0, comparer.Compare(item, item));
    }

    [Fact]
    public void ExplorerItemComparer_null_x_returns_negative()
    {
        var comparer = new ExplorerItemComparer();
        Assert.True(comparer.Compare(null, new ExplorerItem()) < 0);
    }

    [Fact]
    public void ExplorerItemComparer_null_y_returns_positive()
    {
        var comparer = new ExplorerItemComparer();
        Assert.True(comparer.Compare(new ExplorerItem(), null) > 0);
    }

    [Fact]
    public void ExplorerItemComparer_both_null_returns_zero()
    {
        var comparer = new ExplorerItemComparer();
        Assert.Equal(0, comparer.Compare(null, null));
    }

    [Fact]
    public void ExplorerItemComparer_sorts_by_position()
    {
        var comparer = new ExplorerItemComparer();
        var a = new ExplorerItem { Position = 1 };
        var b = new ExplorerItem { Position = 3 };
        var c = new ExplorerItem { Position = 2 };

        Assert.True(comparer.Compare(a, b) < 0);
        Assert.True(comparer.Compare(b, a) > 0);
        Assert.Equal(0, comparer.Compare(a, a));
        Assert.True(comparer.Compare(c, a) > 0);
        Assert.True(comparer.Compare(c, b) < 0);
    }    // ── ExplorerItemSortComparer by Database ──

    [Fact]
    public void SortByDatabase_orders_by_database_then_type_then_name()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Database, SortOrder.Ascending);
        var items = new[]
        {
            CreateItem("B_db", ExplorerItemType.Select, "zebra"),
            CreateItem("A_db", ExplorerItemType.Select, "alpha"),
            CreateItem("A_db", ExplorerItemType.From, "alpha"),
        };

        Array.Sort(items, comparer);

        Assert.Equal("A_db", items[0].Database);
        Assert.Equal(ExplorerItemType.Select, items[0].type); // Select (0) < From (1) in enum sort
        Assert.Equal("A_db", items[1].Database);
        Assert.Equal(ExplorerItemType.From, items[1].type);
        Assert.Equal("B_db", items[2].Database);
    }

    [Fact]
    public void SortByDatabase_descending_reverses_order()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Database, SortOrder.Descending);
        var items = new[]
        {
            CreateItem("A_db", ExplorerItemType.Select, "alpha"),
            CreateItem("B_db", ExplorerItemType.Select, "beta"),
        };

        Array.Sort(items, comparer);

        Assert.Equal("B_db", items[0].Database);
        Assert.Equal("A_db", items[1].Database);
    }

    // ── ExplorerItemSortComparer by Type ──

    [Fact]
    public void SortByType_orders_by_type_then_database_then_name()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Type, SortOrder.Ascending);
        var items = new[]
        {
            CreateItem("db1", ExplorerItemType.Select, "a"),
            CreateItem("db1", ExplorerItemType.From, "b"),
            CreateItem("db2", ExplorerItemType.Select, "c"),
        };

        Array.Sort(items, comparer);

        Assert.Equal(ExplorerItemType.Select, items[0].type); // Select (0) < From (1)
        Assert.Equal("db1", items[0].Database);
        Assert.Equal(ExplorerItemType.Select, items[1].type);
        Assert.Equal("db2", items[1].Database);
        Assert.Equal(ExplorerItemType.From, items[2].type);
        Assert.Equal("db1", items[2].Database);
    }

    // ── ExplorerItemSortComparer by Name ──

    [Fact]
    public void SortByName_orders_by_name_then_type_then_database()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Name, SortOrder.Ascending);
        var items = new[]
        {
            CreateItem("db1", ExplorerItemType.Select, "zebra"),
            CreateItem("db1", ExplorerItemType.Select, "alpha"),
            CreateItem("db2", ExplorerItemType.From, "alpha"),
        };

        Array.Sort(items, comparer);

        Assert.Equal("alpha", items[0].Title);
        Assert.Equal(ExplorerItemType.Select, items[0].type); // Select (0) < From (1)
        Assert.Equal("alpha", items[1].Title);
        Assert.Equal(ExplorerItemType.From, items[1].type);
        Assert.Equal("zebra", items[2].Title);
    }

    // ── ExplorerItemSortComparer by Position ──

    [Fact]
    public void SortByPosition_orders_by_position()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Position, SortOrder.Ascending);
        var items = new[]
        {
            new ExplorerItem { Position = 3, Title = "c" },
            new ExplorerItem { Position = 1, Title = "a" },
            new ExplorerItem { Position = 2, Title = "b" },
        };

        Array.Sort(items, comparer);

        Assert.Equal("a", items[0].Title);
        Assert.Equal("b", items[1].Title);
        Assert.Equal("c", items[2].Title);
    }

    // ── ExplorerItemSortComparer null handling ──

    [Fact]
    public void SortByDatabase_null_x_in_ascending_returns_negative()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Database, SortOrder.Ascending);
        Assert.True(comparer.Compare(null, CreateItem("db", ExplorerItemType.Select, "a")) < 0);
    }

    [Fact]
    public void SortByDatabase_null_y_in_ascending_returns_positive()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Database, SortOrder.Ascending);
        Assert.True(comparer.Compare(CreateItem("db", ExplorerItemType.Select, "a"), null) > 0);
    }

    [Fact]
    public void SortByDatabase_both_null_returns_zero()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Database, SortOrder.Ascending);
        Assert.Equal(0, comparer.Compare(null, null));
    }

    [Fact]
    public void SortByName_case_insensitive()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Name, SortOrder.Ascending);
        var items = new[]
        {
            CreateItem("db", ExplorerItemType.Select, "Zebra"),
            CreateItem("db", ExplorerItemType.Select, "alpha"),
        };

        Array.Sort(items, comparer);

        Assert.Equal("alpha", items[0].Title);
        Assert.Equal("Zebra", items[1].Title);
    }

    [Fact]
    public void SortByName_null_title_treated_as_empty()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Name, SortOrder.Ascending);
        var items = new[]
        {
            new ExplorerItem { Title = null, Position = 1, Database = "db", type = ExplorerItemType.Select },
            new ExplorerItem { Title = "alpha", Position = 2, Database = "db", type = ExplorerItemType.Select },
        };

        Array.Sort(items, comparer);

        Assert.Null(items[0].Title);
        Assert.Equal("alpha", items[1].Title);
    }

    [Fact]
    public void SortByName_trims_leading_whitespace_for_comparison()
    {
        var comparer = new ExplorerItemSortComparer(ExplorerItemSortBy.Name, SortOrder.Ascending);
        var items = new[]
        {
            CreateItem("db", ExplorerItemType.Select, "  alpha"),
            CreateItem("db", ExplorerItemType.Select, "beta"),
        };

        Array.Sort(items, comparer);

        Assert.Equal("  alpha", items[0].Title);
        Assert.Equal("beta", items[1].Title);
    }

    private static ExplorerItem CreateItem(string database, ExplorerItemType type, string name) =>
        new() { Database = database, type = type, Title = name, Position = 0 };
}
