using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBase.Netezza;

namespace AppBase.Services.Sql;

public sealed class NetezzaSqlErrorHighlighter
{
    public sealed record HighlightMatch(string Word, bool UseRegex2, int SelectionStart);

    public bool TryGetHighlight(
        string msg,
        bool fromOleDb,
        string sqlText,
        ReadOnlySpan<char> sqlSlice,
        int selectionStart,
        out HighlightMatch match)
    {
        match = null!;
        if (!NetezzaErrorLocator.TryLocate(msg, fromOleDb, sqlSlice, out var location))
            return false;

        int effectiveSelectionStart = selectionStart + (location.CharIndexInSlice ?? 0);
        match = new HighlightMatch(location.Word, location.UseRegexWordSearch, effectiveSelectionStart);
        return true;
    }

    public void Highlight(
        string msg,
        FastColoredTextBox fctb,
        TextStyle errorStyle,
        int selectionStart,
        int selectionLength,
        bool fromOleDb = false)
    {
        if (selectionStart < 0 || selectionLength <= 0)
            return;

        string currentSqlText = fctb.TextFast;
        if (selectionStart >= currentSqlText.Length)
            return;

        int availableLength = currentSqlText.Length - selectionStart;
        int safeSelectionLength = Math.Min(selectionLength, availableLength);
        ReadOnlySpan<char> sqlSlice = currentSqlText.AsSpan(selectionStart, safeSelectionLength);
        if (!TryGetHighlight(msg, fromOleDb, fctb.Text, sqlSlice, selectionStart, out HighlightMatch match))
            return;

        int founded = fctb.ColorizeErrorWord(errorStyle, match.SelectionStart, safeSelectionLength, match.Word, match.UseRegex2);
        if (founded != -1 && fctb.TextLength > founded)
        {
            fctb.SelectionStart = founded;
            fctb.SelectionLength = 0;
            fctb.DoSelectionVisible();
        }
    }
}
