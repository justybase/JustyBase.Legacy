using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

public sealed class ResultsTabNaming : IResultsTabNaming
{
    private static readonly Regex ResultTabPattern = new(@"^Result\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LogTabPattern = new(@"^Log(?:\s+(\d+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static readonly ResultsTabNaming Default = new();

    /// <summary>
    /// Parses a tab title like "Result 1", "Result 42" and returns the number.
    /// Returns null if the title doesn't match the pattern.
    /// </summary>
    public static int? ParseResultNumber(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        var match = ResultTabPattern.Match(title);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
            return n;

        return null;
    }

    /// <summary>
    /// Given existing tab titles, generates the next "Result N" title.
    /// Works on strings — no WinForms dependency.
    /// </summary>
    public static string NextResultTitle(IReadOnlyList<string> existingTitles)
    {
        int max = 0;
        foreach (var title in existingTitles)
        {
            var n = ParseResultNumber(title);
            if (n.HasValue && n.Value > max)
                max = n.Value;
        }
        return $"Result {max + 1}";
    }

    /// <summary>
    /// Given existing tab titles, generates the next "Log" or "Log N" title.
    /// Works on strings — no WinForms dependency.
    /// </summary>
    public static string NextLogTitle(IReadOnlyList<string> existingTitles)
    {
        int count = 0;
        foreach (var title in existingTitles)
        {
            if (!string.IsNullOrEmpty(title) && LogTabPattern.IsMatch(title))
                count++;
        }
        return count == 0 ? "Log" : $"Log {count + 1}";
    }

    string IResultsTabNaming.NextResultTitle(IReadOnlyList<string> existingTitles)
        => NextResultTitle(existingTitles);

    string IResultsTabNaming.NextLogTitle(IReadOnlyList<string> existingTitles)
        => NextLogTitle(existingTitles);

    // ── Backward-compatible WinForms overloads ──

    public static string NextResultTitle(TabControl tc)
    {
        if (tc is null) return "Result 1";
        var titles = new List<string>(tc.TabPages.Count);
        foreach (TabPage page in tc.TabPages)
            titles.Add(page.Text ?? string.Empty);
        return NextResultTitle(titles);
    }

    public static string NextLogTitle(TabControl tc)
    {
        if (tc is null) return "Log";
        var titles = new List<string>(tc.TabPages.Count);
        foreach (TabPage page in tc.TabPages)
            titles.Add(page.Text ?? string.Empty);
        return NextLogTitle(titles);
    }
}
