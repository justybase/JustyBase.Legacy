using AppBase.Common;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppBase.Data;

public static partial class NetezzaLegacyCompletionHelpers
{

    [GeneratedRegex("([\\w\\.]+)([=<>!:]+)('?\\w+'?)$", RegexOptions.Compiled)]
    public static partial Regex RegexSpace3();

    public static Comparison<(string basicHint, string description)> SortMethodAliases(Dictionary<string, int> keyValuePairs)
    {
        return delegate ((string h, string d) a, (string h, string d) b)
        {
            int n1 = keyValuePairs[a.h] - keyValuePairs[b.h];
            if (keyValuePairs[a.h] <= 3 || keyValuePairs[b.h] <= 3)
            {
                return keyValuePairs[a.h] - keyValuePairs[b.h];
            }
            else
            {
                return a.h.CompareTo(b.h);
            }
        };
    }

    public static void SaveSnipets(
        AppBase.Common.Interfaces.IApplicationSettingsContext applicationSettingsContext,
        AppBase.Data.Core.Models.INetezzaAutocompleteState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Snipets sn = new Snipets
        {
            Keywords = state.Keywords.ToArray(),
            Snippets = state.Snippets.ToArray(),
            MonkeySnippets = state.MonkeySnippets.ToArray()
        };
        string filePath = Path.Combine(applicationSettingsContext.ConfigDirectory, "snipets.json");
        string content = JsonSerializer.Serialize(sn, MyJsonContextSnipets.Default.Snipets);
        File.WriteAllText(filePath, content);
    }
}
