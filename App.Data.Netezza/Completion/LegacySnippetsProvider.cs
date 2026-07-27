using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Models;
using FastColoredTextBoxNS;
using JustData.Application.Variables;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppBase.Data.Completion;

/// <summary>
/// User snippets, monkey directives, @@ expansion, session/global variables — WinForms-only layer.
/// </summary>
public sealed class LegacySnippetsProvider
{
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private readonly ISessionVariableStore _sessionVariableStore;
    private readonly INetezzaAutocompleteState _state;
    private readonly Regex _space3 = DynamicCollectionForNettezaHelpers.RegexSpace3();
    private readonly string _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public LegacySnippetsProvider(
        IApplicationSettingsContext applicationSettingsContext,
        ISessionVariableStore sessionVariableStore,
        INetezzaAutocompleteState state)
    {
        _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
        _sessionVariableStore = sessionVariableStore ?? throw new ArgumentNullException(nameof(sessionVariableStore));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        EnsureSnippetsLoaded();
    }

    public static void EnsureSnippetsLoaded(
        IApplicationSettingsContext applicationSettingsContext,
        INetezzaAutocompleteState state)
    {
        if (applicationSettingsContext is null || state is null
            || state.Keywords.Count > 0
            || state.Snippets.Count > 0
            || state.MonkeySnippets.Count > 0)
            return;

        string filepath = Path.Combine(applicationSettingsContext.ConfigDirectory, "snipets.json");
        string content = File.ReadAllText(filepath);
        var sn = JsonSerializer.Deserialize(content, MyJsonContextSnipets.Default.Snipets);
        if (sn is null)
            throw new NullReferenceException(nameof(sn));

        state.ReplaceSnippets(sn.Keywords, sn.Snippets, sn.MonkeySnippets);
    }

    private void EnsureSnippetsLoaded() => EnsureSnippetsLoaded(_applicationSettingsContext, _state);

    public IEnumerable<AutocompleteItem> YieldPreambleItems(string documentKey)
    {
        yield return CompletionItemAppearance.Apply(
            new AutocompleteItem("declare"), CompletionIconKind.Keyword, "Keyword");

        foreach (var item in _sessionVariableStore.GlobalVariables)
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item.Key), CompletionIconKind.Variable, "Variable");

        var sess = _sessionVariableStore.GetSessionVariables(documentKey);
        if (sess is not null)
        {
            foreach (var item in sess.Keys)
                yield return CompletionItemAppearance.Apply(
                    new MethodAutocompleteItem2(item), CompletionIconKind.Variable, "Variable");
        }

        yield return CompletionItemAppearance.Apply(
            new InsertSpaceSnippet(), CompletionIconKind.Snippet, "Snippet");
        yield return CompletionItemAppearance.Apply(
            new InsertSpaceSnippet(_space3), CompletionIconKind.Snippet, "Snippet");
    }

    /// <returns>True when caller should stop yielding SQL/engine items (@@ path).</returns>
    public bool TryYieldAtPrefixItems(string fragmentText, List<(string basicHint, string description)> aliasHints, out IEnumerable<AutocompleteItem> items)
    {
        if (fragmentText.StartsWith("@@"))
        {
            items = YieldAtAliasExpansion(aliasHints);
            return true;
        }

        if (fragmentText.StartsWith('.'))
        {
            items = YieldDotMonkeySnippets();
            return false;
        }

        items = Array.Empty<AutocompleteItem>();
        return false;
    }

    public IEnumerable<AutocompleteItem> YieldKeywordsAndSnippets(string fragmentText)
    {
        if (fragmentText.Contains('.'))
            yield break;

        foreach (var item in _state.Snippets)
        {
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem2(item), CompletionIconKind.Snippet, "Snippet");
        }

        foreach (var item in _state.Keywords)
        {
            yield return CompletionItemAppearance.Apply(
                new AutocompleteItem(item), CompletionIconKind.Keyword, "Keyword");
        }
    }

    private IEnumerable<AutocompleteItem> YieldAtAliasExpansion(List<(string basicHint, string description)> aliasHints)
    {
        if (_state.ExtraSnippet is { Length: >= 6 } extraSnippet)
            yield return CompletionItemAppearance.Apply(
                new MonkeySnippets(extraSnippet),
                CompletionIconKind.Snippet,
                "Snippet");

        foreach (var item in _state.MonkeySnippets)
            yield return CompletionItemAppearance.Apply(
                new MonkeySnippets(item), CompletionIconKind.Snippet, "Snippet");

        foreach (var (basicHint, _) in aliasHints)
        {
            if (basicHint.Contains('.'))
                continue;

            var sb = new StringBuilder();
            sb.Append($"@@{basicHint} \n");
            sb.Append(string.Join("\n    , ",
                aliasHints
                    .Where(arg => arg.basicHint.StartsWith(basicHint + '.', StringComparison.OrdinalIgnoreCase))
                    .Select(arg =>
                    {
                        string h1 = arg.description;
                        if (h1 is not null)
                        {
                            int cr = h1.IndexOf('\r');
                            if (cr >= 0) h1 = h1[..cr];
                            else
                            {
                                int nl = h1.IndexOf('\n');
                                if (nl >= 0) h1 = h1[..nl];
                            }
                            if (h1.Length > 50) h1 = h1[..50];
                        }

                        return $"{arg.basicHint} --{h1}";
                    })));

            yield return CompletionItemAppearance.Apply(
                new MonkeySnippets(sb.ToString()), CompletionIconKind.Snippet, "Snippet");
        }
    }

    private IEnumerable<AutocompleteItem> YieldDotMonkeySnippets()
    {
        yield return Snippet(@$".ImportXlsxTxtCsv ___imp: {_desktop}\test_import.xlsx/SheetName -> tableName;");
        yield return Snippet(@$".ImportOleDB ___impOleDb: Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_desktop}\Database.accdb;/tableSource -> tableDest;");
        yield return Snippet(@$".ImportODBC ___impODBC: connectionString -> -> tableDest;");
        yield return Snippet(@$".ImportDB2 ___impDB2: connectionString -> -> tableDest;");
        yield return Snippet(@$".Python ___run: python -> -u {_desktop}\file.py arg1 arg2;");
        yield return Snippet(@$".CsvExport ___expCsv: SELECT * FROM SAMPLE_DATA LIMIT 123 -> {_desktop}\sampleX.csv;");
        yield return Snippet(@$".XlsxExport ___expXlsx: SELECT * FROM SAMPLE_DATA LIMIT 123 -> {_desktop}\sampleX.xlsx;");
        yield return Snippet(@$".sleep ___sleep 2000;");
        yield return Snippet(@$".maxRows ___maxRows 10000;");
        yield return Snippet(@$".echo ___echo message;");
        yield return Snippet(@$".echoFile ___echoFile filepath:message;");
        yield return Snippet(@$".notify ___window iconify;");
        yield return Snippet(@$".restore ___window restore;");
        yield return Snippet(@$".convert ___convert(1250:FILE.TXT,UTF8:FILE2.TXT)");
        yield return Snippet(@$".BLOB BLOB
INSERT INTO TABLENAME VALUES(?)
PATHS
C:\image.jpg
;");
    }

    private static AutocompleteItem Snippet(string text)
    {
        return CompletionItemAppearance.Apply(
            new MonkeySnippets(text), CompletionIconKind.Snippet, "Snippet");
    }
}
