using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Variables;
using System.Collections.ObjectModel;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Owns session-scoped and global SQL variables independently of schema and
/// configuration runtime state.
/// </summary>
public sealed class LegacySessionVariableContext :
    ISessionVariableRuntimeContext,
    ISessionVariableStore
{
    private readonly Dictionary<string, Dictionary<string, string>> _sessionVariables = [];
    private readonly Dictionary<string, string> _globalVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["&yesterday_id"] = DateTime.Today.AddDays(-1).ToString("yyyyMMdd"),
        ["&yesterday"] = $"'{DateTime.Today.AddDays(-1):yyyy-MM-dd}'",
        ["&last_working_id"] = DateTimeAddons.PreviousWorkDay(DateTime.Today).ToString("yyyyMMdd"),
        ["&last_working"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Today):yyyy-MM-dd}'",
        ["&now"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Now):yyyy-MM-dd HH:mm:ss.fff}'"
    };

    public string ActualTabTitleText { get; set; } = string.Empty;

    public event EventHandler? Changed;

    public IReadOnlyDictionary<string, string> GlobalVariables => Snapshot(_globalVariables);

    public IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey)
    {
        if (string.IsNullOrWhiteSpace(documentKey)
            || !_sessionVariables.TryGetValue(documentKey, out Dictionary<string, string>? values))
        {
            return EmptySnapshot();
        }

        return Snapshot(values);
    }

    public bool HasSessionVariables(string documentKey) =>
        !string.IsNullOrWhiteSpace(documentKey) && _sessionVariables.ContainsKey(documentKey);

    public int GetSessionVariableCount(string documentKey) =>
        !string.IsNullOrWhiteSpace(documentKey)
        && _sessionVariables.TryGetValue(documentKey, out Dictionary<string, string>? values)
            ? values.Count
            : 0;

    public void EnsureSessionVariables(string documentKey)
    {
        if (!string.IsNullOrWhiteSpace(documentKey))
            _sessionVariables.TryAdd(documentKey, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public void CopySessionVariables(string sourceDocumentKey, string destinationDocumentKey)
    {
        if (string.IsNullOrWhiteSpace(destinationDocumentKey))
            return;

        var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(sourceDocumentKey)
            && _sessionVariables.TryGetValue(sourceDocumentKey, out Dictionary<string, string>? source))
        {
            foreach ((string key, string value) in source)
                copied[key] = value;
        }

        _sessionVariables[destinationDocumentKey] = copied;
    }

    public void SetSessionVariable(string documentKey, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(documentKey) || string.IsNullOrWhiteSpace(name))
            return;

        EnsureSessionVariables(documentKey);
        _sessionVariables[documentKey][name] = value ?? string.Empty;
    }

    public void SetGlobalVariable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _globalVariables[name] = value ?? string.Empty;
    }

    public void SetSessionVariables(string documentKey, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach ((string key, string value) in values)
            SetSessionVariable(documentKey, key, value);
    }

    public void ClearGlobalVariables()
    {
        _globalVariables.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string ReplaceGlobalVariables(string query)
    {
        _globalVariables["&now"] = $"'{DateTimeAddons.PreviousWorkDay(DateTime.Now):yyyy-MM-dd HH:mm:ss.fff}'";
        foreach (var item in _globalVariables.OrderByDescending(item => item.Key.Length))
        {
            if (query.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                query = query.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
        }

        return query;
    }

    private static IReadOnlyDictionary<string, string> EmptySnapshot() =>
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private static IReadOnlyDictionary<string, string> Snapshot(Dictionary<string, string> values) =>
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase));
}
