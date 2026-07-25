using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Variables;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Owns session-scoped and global SQL variables independently of schema and
/// configuration runtime state.
/// </summary>
public sealed class LegacySessionVariableContext :
    ISessionVariableRuntimeContext,
    ISessionVariableStore
{
    public Dictionary<string, Dictionary<string, string>> SessionVariables { get; } = [];
    public Dictionary<string, string> GlobalVariables { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["&yesterday_id"] = DateTime.Today.AddDays(-1).ToString("yyyyMMdd"),
        ["&yesterday"] = $"'{DateTime.Today.AddDays(-1):yyyy-MM-dd}'",
        ["&last_working_id"] = DateTimeAddons.PreviousWorkDay(DateTime.Today).ToString("yyyyMMdd"),
        ["&last_working"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Today):yyyy-MM-dd}'",
        ["&now"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Now):yyyy-MM-dd HH:mm:ss.fff}'"
    };

    public string ActualTabTitleText { get; set; } = string.Empty;

    public event EventHandler? Changed;

    IReadOnlyDictionary<string, string> ISessionVariableStore.GlobalVariables => GlobalVariables;

    public IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey)
    {
        if (string.IsNullOrWhiteSpace(documentKey)
            || !SessionVariables.TryGetValue(documentKey, out Dictionary<string, string>? values))
        {
            return new Dictionary<string, string>();
        }

        return values;
    }

    public void ClearGlobalVariables()
    {
        GlobalVariables.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string ReplaceGlobalVariables(string query)
    {
        GlobalVariables["&now"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Now):yyyy-MM-dd HH:mm:ss.fff}'";
        foreach (var item in GlobalVariables.OrderByDescending(item => item.Key.Length))
        {
            if (query.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                query = query.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
        }

        return query;
    }
}
