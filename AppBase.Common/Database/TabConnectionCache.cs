using FastColoredTextBoxNS;
using System.Runtime.CompilerServices;

namespace AppBase.Common;

public sealed class TabConnectionCache : ITabConnectionCache
{
    private readonly ConditionalWeakTable<FastColoredTextBox, TabConnectionData> _cache = new();

    public static ITabConnectionCache Default { get; } = new TabConnectionCache();

    public TabConnectionData GetOrCreate(FastColoredTextBox fctb)
    {
        return _cache.GetValue(fctb, _ => new TabConnectionData());
    }

    public bool TryGet(FastColoredTextBox fctb, out TabConnectionData? data)
    {
        return _cache.TryGetValue(fctb, out data);
    }

    public void Remove(FastColoredTextBox fctb)
    {
        _cache.Remove(fctb);
    }

    public void Set(FastColoredTextBox fctb, TabConnectionData data)
    {
        _cache.Remove(fctb);
        _cache.Add(fctb, data);
    }
}
