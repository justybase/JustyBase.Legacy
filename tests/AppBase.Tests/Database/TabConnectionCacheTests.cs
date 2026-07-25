using AppBase.Common;
using FastColoredTextBoxNS;

namespace AppBase.Tests.Database;

public sealed class TabConnectionCacheTests
{
    [Fact]
    public void GetOrCreate_creates_new_entry_when_not_exists()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();

        var result = cache.GetOrCreate(fctb);

        Assert.NotNull(result);
        Assert.Null(result.Connection);
    }

    [Fact]
    public void GetOrCreate_returns_same_instance_on_second_call()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();

        var first = cache.GetOrCreate(fctb);
        var second = cache.GetOrCreate(fctb);

        Assert.Same(first, second);
    }

    [Fact]
    public void TryGet_returns_false_when_not_exists()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();

        bool found = cache.TryGet(fctb, out var data);

        Assert.False(found);
        Assert.Null(data);
    }

    [Fact]
    public void TryGet_returns_true_after_GetOrCreate()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();

        cache.GetOrCreate(fctb);
        bool found = cache.TryGet(fctb, out var data);

        Assert.True(found);
        Assert.NotNull(data);
    }

    [Fact]
    public void Set_replaces_existing_entry()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();
        var original = cache.GetOrCreate(fctb);
        original.ConnectionName = "original";

        var replacement = new TabConnectionData { ConnectionName = "replacement" };
        cache.Set(fctb, replacement);

        cache.TryGet(fctb, out var stored);
        Assert.Equal("replacement", stored!.ConnectionName);
    }

    [Fact]
    public void Remove_clears_entry()
    {
        var cache = new TabConnectionCache();
        var fctb = new FastColoredTextBox();

        cache.GetOrCreate(fctb);
        cache.Remove(fctb);

        Assert.False(cache.TryGet(fctb, out _));
    }

    [Fact]
    public void Default_singleton_works()
    {
        Assert.NotNull(TabConnectionCache.Default);
        Assert.IsAssignableFrom<ITabConnectionCache>(TabConnectionCache.Default);
    }

    [Fact]
    public void Different_fctb_have_different_entries()
    {
        var cache = new TabConnectionCache();
        var fctb1 = new FastColoredTextBox();
        var fctb2 = new FastColoredTextBox();

        var data1 = cache.GetOrCreate(fctb1);
        var data2 = cache.GetOrCreate(fctb2);

        Assert.NotSame(data1, data2);
    }
}
