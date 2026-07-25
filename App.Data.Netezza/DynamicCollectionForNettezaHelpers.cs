using AppBase.Common;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppBase.Data;

public static partial class DynamicCollectionForNettezaHelpers
{

    public static string[] Keywords = [];

    public static string[] Snippets = [];

    public static string[] MonkeySnippets = [];

    //for group by 
    public static string ExtraSnippet;

    public static List<string> ActualColumnList = new List<string>();

    public static Dictionary<string, string[]> DatabaseArray = new Dictionary<string, string[]>();
    public static List<(string hint, string description)> CacheList1 = new List<(string, string)>(); //cache
    public static List<(string hint, string description)> CacheList2 = new List<(string, string)>();

    public static string CurrentColumn;

    public static void ResetCache()
    {
        CacheList1.Clear();
        CacheList2.Clear();
    }

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

    public static void SaveSnipets(AppBase.Common.Interfaces.IApplicationSettingsContext applicationSettingsContext)
    {
        Snipets sn = new Snipets
        {
            Keywords = Keywords,
            Snippets = Snippets,
            MonkeySnippets = MonkeySnippets
        };
        string filePath = Path.Combine(applicationSettingsContext.ConfigDirectory, "snipets.json");
        string content = JsonSerializer.Serialize(sn, MyJsonContextSnipets.Default.Snipets);
        File.WriteAllText(filePath, content);
    }
}
