using FastColoredTextBoxNS;

namespace AppBase.Common;

public interface ITabConnectionCache
{
    TabConnectionData GetOrCreate(FastColoredTextBox fctb);
    bool TryGet(FastColoredTextBox fctb, out TabConnectionData? data);
    void Remove(FastColoredTextBox fctb);
    void Set(FastColoredTextBox fctb, TabConnectionData data);
}
