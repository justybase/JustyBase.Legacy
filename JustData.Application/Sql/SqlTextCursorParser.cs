namespace JustData.Application.Sql;

/// <summary>
/// Finds top-level SQL clauses for editor-context analysis.
///
/// This parser intentionally remains small and provider-neutral. It tracks
/// parentheses only; provider-specific completion and editor interaction stay
/// outside the application layer.
/// </summary>
public static class SqlTextCursorParser
{
    public static string BetweenSemicolons(int position, string sqlText)
    {
        ArgumentNullException.ThrowIfNull(sqlText);

        bool quoteBalance = true;
        bool doubleQuoteBalance = true;
        int length = sqlText.Length;

        if (position >= length)
            position = length - 1;
        int start = position > 0 ? position - 1 : position;

        if (position == -1)
            return string.Empty;

        while (start > 0 && start < length)
        {
            char c = sqlText[start];
            if (c == ';' && quoteBalance && doubleQuoteBalance)
            {
                start++;
                break;
            }
            else if (c == '\'')
                quoteBalance = !quoteBalance;
            else if (c == '"')
                doubleQuoteBalance = !doubleQuoteBalance;
            start--;
        }

        quoteBalance = true;
        doubleQuoteBalance = true;
        int end = position;
        while (end < length)
        {
            char c = sqlText[end];
            if (c == ';' && quoteBalance && doubleQuoteBalance)
                break;
            else if (c == '\'')
                quoteBalance = !quoteBalance;
            else if (c == '"')
                doubleQuoteBalance = !doubleQuoteBalance;
            end++;
        }

        return end > length || end < start
            ? sqlText[start..length]
            : sqlText[start..end];
    }

    public static string BetweenParenthesesOrBrackets(int position, string sqlText)
    {
        ArgumentNullException.ThrowIfNull(sqlText);

        bool quoteBalance = true;
        bool doubleQuoteBalance = true;
        int bracketBalance = 0;
        int start = position > 0 ? position - 1 : position;
        int length = sqlText.Length;

        if (position >= length)
            position = length - 1;
        if (position == -1)
            return string.Empty;

        while (start > 0 && start < length)
        {
            char c = sqlText[start];
            if ((c == ';' || c == '(' && ++bracketBalance == 1) && quoteBalance && doubleQuoteBalance)
            {
                start++;
                break;
            }
            else if (c == '\'')
                quoteBalance = !quoteBalance;
            else if (c == '"')
                doubleQuoteBalance = !doubleQuoteBalance;
            else if (c == ')')
                bracketBalance--;
            start--;
        }

        quoteBalance = true;
        doubleQuoteBalance = true;
        bracketBalance = 0;
        int end = position;
        while (end < length)
        {
            char c = sqlText[end];
            if ((c == ';' || c == ')' && --bracketBalance == -1) && quoteBalance && doubleQuoteBalance)
                break;
            else if (c == '\'')
                quoteBalance = !quoteBalance;
            else if (c == '"')
                doubleQuoteBalance = !doubleQuoteBalance;
            else if (c == '(')
                bracketBalance++;
            end++;
        }

        if (end >= length)
            end = length - 1;
        if (start >= length)
            start = length - 1;
        if (end == length - 1)
            end++;

        return sqlText[start..end];
    }

    public static int FindClosingBracket(string sqlFragment, int start = 0)
    {
        ArgumentNullException.ThrowIfNull(sqlFragment);

        int bracketBalance = 0;
        for (int index = start; index < sqlFragment.Length; index++)
        {
            if (sqlFragment[index] == '(')
                bracketBalance++;
            else if (sqlFragment[index] == ')')
                bracketBalance--;

            if (bracketBalance == -1)
                return index;
        }

        return -1;
    }

    public static int LastSelect(ref string inner, bool doTrim = true)
    {
        ArgumentNullException.ThrowIfNull(inner);

        if (doTrim && inner.Length > 0
            && (char.IsWhiteSpace(inner[0]) || char.IsWhiteSpace(inner[^1])))
        {
            inner = inner.Trim();
        }

        int n = inner.Length;
        int selectIndex = -1;
        int bracketBalance = 0;
        int m = n - 5;

        for (int i = 0; i < m; i++)
        {
            char c = inner[i];
            if (bracketBalance == 0
                && (c == 's' || c == 'S')
                && (inner[i + 1] == 'e' || inner[i + 1] == 'E')
                && (inner[i + 2] == 'l' || inner[i + 2] == 'L')
                && (inner[i + 3] == 'e' || inner[i + 3] == 'E')
                && (inner[i + 4] == 'c' || inner[i + 4] == 'C')
                && (inner[i + 5] == 't' || inner[i + 5] == 'T')
                && (i == 0 || inner[i - 1] is ' ' or '\n' or '\r' or '(' or ')' or '\t')
                && (i + 5 == n - 1 || inner[i + 6] is ' ' or '\n' or '\r' or '(' or ')' or '\t'))
            {
                selectIndex = i > 0 ? i - 1 : 0;
            }
            else if (c == '(')
            {
                bracketBalance++;
            }
            else if (c == ')')
            {
                bracketBalance--;
            }
        }

        return selectIndex;
    }

    public static int FirstFrom(string afterSelect)
    {
        ArgumentNullException.ThrowIfNull(afterSelect);

        int n = afterSelect.Length;
        int bracketBalance = 0;
        for (int i = 1; i < n - 3; i++)
        {
            if (bracketBalance == 0
                && (afterSelect[i] == 'f' || afterSelect[i] == 'F')
                && (afterSelect[i + 1] == 'r' || afterSelect[i + 1] == 'R')
                && (afterSelect[i + 2] == 'o' || afterSelect[i + 2] == 'O')
                && (afterSelect[i + 3] == 'm' || afterSelect[i + 3] == 'M')
                && (i == 0 || afterSelect[i - 1] is ' ' or '\n' or '\r' or '(' or ')' or '\t')
                && (i + 3 == n - 1 || afterSelect[i + 4] is ' ' or '\n' or '\r' or '(' or ')' or '\t'))
            {
                return i > 0 ? i - 1 : 0;
            }

            if (afterSelect[i] == '(')
                bracketBalance++;
            else if (afterSelect[i] == ')')
                bracketBalance--;
        }

        return -1;
    }

    public static int FirstWhereGroupLimit(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        int n = text.Length;
        int bracketBalance = 0;
        for (int i = 0; i < n - 4; i++)
        {
            bool atTokenBoundary = i == 0 || text[i - 1] is ' ' or '\n' or '\r' or '(' or ')' or '\t';
            if (bracketBalance == 0
                && atTokenBoundary
                && (IsKeyword(text, i, "where")
                    || IsKeyword(text, i, "limit")
                    || IsGroupBy(text, i)))
            {
                return i > 0 ? i - 1 : 0;
            }

            if (text[i] == '(')
                bracketBalance++;
            else if (text[i] == ')')
                bracketBalance--;
        }

        return -1;
    }

    private static bool IsKeyword(string text, int start, string keyword)
    {
        int end = start + keyword.Length;
        return end <= text.Length
            && text.AsSpan(start, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase)
            && (end == text.Length || text[end] is ' ' or '\n' or '\r' or '(' or ')' or '\t');
    }

    private static bool IsGroupBy(string text, int start)
    {
        const string keyword = "group by";
        int end = start + keyword.Length;
        return end <= text.Length
            && text.AsSpan(start, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase)
            && (end == text.Length || text[end] is ' ' or '\n' or '\r' or '(' or ')' or '\t');
    }
}
