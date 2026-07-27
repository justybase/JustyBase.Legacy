namespace AppBase.Data.Core.Models;

/// <summary>
/// Per-application Netezza editor state used by the legacy authoring adapter.
/// Per-application replacement for the former process-wide snippet/keyword store.
/// </summary>
public interface INetezzaAutocompleteState
{
    IReadOnlyList<string> Keywords { get; }
    IReadOnlyList<string> Snippets { get; }
    IReadOnlyList<string> MonkeySnippets { get; }
    IReadOnlyList<string> ActualColumnList { get; }
    string? ExtraSnippet { get; }
    string? CurrentColumn { get; set; }

    void ReplaceSnippets(IEnumerable<string>? keywords, IEnumerable<string>? snippets, IEnumerable<string>? monkeySnippets);
    void ReplaceActualColumns(IEnumerable<string>? columns);
    void AddMonkeySnippet(string snippet);
}

public sealed class NetezzaAutocompleteState : INetezzaAutocompleteState
{
    private readonly object _sync = new();
    private string[] _keywords = [];
    private string[] _snippets = [];
    private string[] _monkeySnippets = [];
    private string[] _actualColumnList = [];
    private string? _extraSnippet;
    private string? _currentColumn;

    public IReadOnlyList<string> Keywords { get { lock (_sync) return _keywords.ToArray(); } }
    public IReadOnlyList<string> Snippets { get { lock (_sync) return _snippets.ToArray(); } }
    public IReadOnlyList<string> MonkeySnippets { get { lock (_sync) return _monkeySnippets.ToArray(); } }
    public IReadOnlyList<string> ActualColumnList { get { lock (_sync) return _actualColumnList.ToArray(); } }
    public string? ExtraSnippet { get { lock (_sync) return _extraSnippet; } }
    public string? CurrentColumn
    {
        get { lock (_sync) return _currentColumn; }
        set { lock (_sync) _currentColumn = value; }
    }

    public void ReplaceSnippets(IEnumerable<string>? keywords, IEnumerable<string>? snippets, IEnumerable<string>? monkeySnippets)
    {
        lock (_sync)
        {
            _keywords = (keywords ?? []).ToArray();
            _snippets = (snippets ?? []).ToArray();
            _monkeySnippets = (monkeySnippets ?? []).ToArray();
        }
    }

    public void ReplaceActualColumns(IEnumerable<string>? columns)
    {
        lock (_sync)
        {
            _actualColumnList = (columns ?? []).ToArray();
            _extraSnippet = _actualColumnList.Length == 0
                ? null
                : "@@xx " + string.Join(",", _actualColumnList);
        }
    }

    public void AddMonkeySnippet(string snippet)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        lock (_sync)
            _monkeySnippets = [.. _monkeySnippets, snippet];
    }

}
