using AppBase.Common;

namespace AppBase.Services.Sql;

/// <summary>
/// Small, dependency-free SQL text helpers used by the legacy editor preprocessor.
/// This is deliberately not a SQL parser; it only tracks quoted SQL literals and identifiers.
/// </summary>
public sealed class SqlTextScanner : ISqlTextScanner
{
    /// <summary>
    /// Default singleton instance. Test code and DI consumers should prefer
    /// working with <see cref="ISqlTextScanner"/> over this static field.
    /// </summary>
    public static readonly SqlTextScanner Default = new();

    public static bool IsInsideQuotedLiteral(string sql, int position) =>
        Default.DoIsInsideQuotedLiteral(sql, position);

    public static bool IsInsideComment(string sql, int position) =>
        Default.DoIsInsideComment(sql, position);

    // ── Instance methods (backed by static logic) ──

    public bool DoIsInsideQuotedLiteral(string sql, int position) =>
        IsInsideQuotedLiteralCore(sql, position);

    public bool DoIsInsideComment(string sql, int position) =>
        IsInsideCommentCore(sql, position);

    // ── Explicit interface implementations ──

    bool ISqlTextScanner.IsInsideQuotedLiteral(string sql, int position) =>
        DoIsInsideQuotedLiteral(sql, position);

    bool ISqlTextScanner.IsInsideComment(string sql, int position) =>
        DoIsInsideComment(sql, position);

    // ── Core logic (pure, testable) ──

    private static bool IsInsideQuotedLiteralCore(string sql, int position)
    {
        if (string.IsNullOrEmpty(sql) || position <= 0)
        {
            return false;
        }

        position = Math.Min(position, sql.Length);
        char quote = '\0';

        for (int i = 0; i < position; i++)
        {
            char current = sql[i];

            if (quote == '\0')
            {
                if (current is '\'' or '"')
                {
                    quote = current;
                }

                continue;
            }

            if (current != quote)
            {
                continue;
            }

            // SQL escapes a quote in a literal by doubling it: '' or "".
            if (i + 1 < position && sql[i + 1] == quote)
            {
                i++;
                continue;
            }

            quote = '\0';
        }

        return quote != '\0';
    }

    private static bool IsInsideCommentCore(string sql, int position)
    {
        if (string.IsNullOrEmpty(sql) || position <= 0)
        {
            return false;
        }

        position = Math.Min(position, sql.Length);
        char quote = '\0';
        bool lineComment = false;
        int blockCommentDepth = 0;

        for (int index = 0; index < position; index++)
        {
            char current = sql[index];
            char next = index + 1 < position ? sql[index + 1] : '\0';

            if (lineComment)
            {
                if (current is '\r' or '\n')
                {
                    lineComment = false;
                }
                continue;
            }

            if (blockCommentDepth > 0)
            {
                if (current == '/' && next == '*')
                {
                    blockCommentDepth++;
                    index++;
                }
                else if (current == '*' && next == '/')
                {
                    blockCommentDepth--;
                    index++;
                }
                continue;
            }

            if (quote != '\0')
            {
                if (current == quote)
                {
                    if (next == quote)
                    {
                        index++;
                    }
                    else
                    {
                        quote = '\0';
                    }
                }
                continue;
            }

            if (current is '\'' or '"')
            {
                quote = current;
            }
            else if (current == '-' && next == '-')
            {
                lineComment = true;
                index++;
            }
            else if (current == '/' && next == '*')
            {
                blockCommentDepth = 1;
                index++;
            }
        }

        return lineComment || blockCommentDepth > 0;
    }
}
