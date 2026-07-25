using System.Text.RegularExpressions;

namespace AppBase.Common;

public static partial class StringExtension
{
    /// <summary>
    /// Splits Excel/CSV data line respecting quoted fields and escape characters
    /// </summary>
    /// <param name="line">The line to split</param>
    /// <param name="separator">The separator character</param>
    /// <param name="result">Array to store the split results</param>
    /// <param name="escapeChar">Character used for escaping quotes</param>
    public static void SplitExcelData(string line, char separator, string[] result, char escapeChar)
    {
        int n = line.Length;
        int quoteNum = 0;
        var lineSpan = line.AsSpan();

        int lastPosition = 0;
        bool startsWithQuote = false;
        int l = 0;
        for (int i = 0; i < n; i++)
        {
            if (l == result.Length)
            {
                throw new Exception("splitExcelData - row has too many separators");
            }

            if (lineSpan[i] == '"')
            {
                quoteNum++;
            }
            if (i == lastPosition)
            {
                if (lineSpan[i] == '"')
                {
                    startsWithQuote = true;
                }
                else
                {
                    startsWithQuote = false;
                }
            }
            if (lineSpan[i] == separator && (quoteNum % 2 == 0 || !startsWithQuote))
            {
                result[l++] = lineSpan.Slice(lastPosition, i - lastPosition).ToString();
                if (quoteNum % 2 == 1)
                {
                    quoteNum++;
                }
                lastPosition = i + 1;
                if (result[l - 1].StartsWith('"')) // contains and not starts ?
                {
                    if (!result[l - 1].Contains(separator))
                    {
                        result[l - 1] = result[l - 1].Replace("\"", $"{escapeChar}\"");
                    }
                    else if (!result[l - 1].EndsWith('"'))
                    {
                        result[l - 1] = result[l - 1].Replace("\"", $"{escapeChar}\"");
                    }
                }
            }
        }
        result[l] = lineSpan.Slice(lastPosition).ToString();
        if (result[l].StartsWith('"'))
        {
            if (!result[l].Contains(separator))
            {
                result[l] = result[l].Replace("\"", $"{escapeChar}\"");
            }
            else if (!result[l].EndsWith('"'))
            {
                result[l] = result[l].Replace("\"", $"{escapeChar}\"");
            }
        }
    }

    /// <summary>
    /// Removes quotes from a quoted name and handles escaped quotes
    /// </summary>
    /// <param name="word">The quoted word to process</param>
    /// <returns>The unquoted word</returns>
    public static string UnquoteName(string word)
    {
        if (word.StartsWith('"'))
        {
            word = word[1..(word.Length - 1)];
            if (!word.Contains('"'))
            {
                word = word.Replace("\"\"", "\"");
            }
        }
        return word;
    }
    private static Regex _rxProperSqlName = new Regex(@"^[A-Z0-9_]*$");
    /// <summary>
    /// Adds quotes around a name if it contains special characters or doesn't match SQL naming rules
    /// </summary>
    /// <param name="word">The word to potentially quote</param>
    /// <returns>The quoted word if needed, otherwise the original word</returns>
    public static string QuoteNameIfNeeded(string word)
    {
        if (!_rxProperSqlName.IsMatch(word))
        {
            if (word.Contains('"'))
            {
                word = $"\"{word.Replace("\"", "\"\"")}\"";
            }
            else
            {
                word = $"\"{word}\"";
            }
        }
        return word;
    }

    /// <summary>
    /// Determines whether the beginning of this string instance matches any of the specified strings
    /// </summary>
    /// <param name="source">The string to check</param>
    /// <param name="values">Array of strings to compare</param>
    /// <param name="comparison">String comparison type</param>
    /// <returns>True if source starts with any of the values</returns>
    public static bool StartsWithAny(this string source, string[] values, StringComparison comparison = StringComparison.CurrentCulture)
    {
        return values.Any(value => source.StartsWith(value, comparison));
    }
    /// <summary>
    /// Determines whether the end of this string instance matches any of the specified strings
    /// </summary>
    /// <param name="source">The string to check</param>
    /// <param name="values">Array of strings to compare</param>
    /// <param name="comparison">String comparison type</param>
    /// <returns>True if source ends with any of the values</returns>
    public static bool EndsWithAny(this string source, string[] values, StringComparison comparison = StringComparison.CurrentCulture)
    {
        return values.Any(value => source.EndsWith(value, comparison));
    }
    /// <summary>
    /// Determines whether this string instance contains any of the specified strings
    /// </summary>
    /// <param name="source">The string to check</param>
    /// <param name="values">Array of strings to search for</param>
    /// <param name="comparison">String comparison type</param>
    /// <returns>True if source contains any of the values</returns>
    public static bool ContainsAny(this string source, string[] values, StringComparison comparison = StringComparison.CurrentCulture)
    {
        return values.Any(value => source.IndexOf(value, comparison) >= 0);
    }

    /// <summary>
    /// Finds the position of the last dot (.) that is not inside quoted text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <returns>The position of the last unquoted dot, or -1 if not found</returns>
    public static int LastDot(this string text)
    {
        int position = -1;
        int length = text.Length;

        bool insideQuotes = false;

        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == '.' && !insideQuotes)
            {
                position = i;
            }
        }
        return position;
    }

    /// <summary>
    /// Finds the position of the first dot (.) that is not inside quoted text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <returns>The position of the first unquoted dot, or -1 if not found</returns>
    public static int FirstDot(this string text)
    {
        int position = -1;
        int length = text.Length;

        bool insideQuotes = false;

        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == '.' && !insideQuotes)
            {
                position = i;
                break;
            }
        }
        return position;
    }

    /// <summary>
    /// Counts the number of dots (.) that are not inside quoted text
    /// </summary>
    /// <param name="text">The text to count dots in</param>
    /// <returns>The number of unquoted dots</returns>
    public static int DotCounter(this string text)
    {
        int dotCount = 0;
        int length = text.Length;

        bool insideQuotes = false;

        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == '.' && !insideQuotes)
            {
                dotCount++;
            }
        }
        return dotCount;
    }

    /// <summary>
    /// Finds the position of the last dot, space, newline, or carriage return that is not inside quoted text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <returns>The position of the last unquoted separator character, or -1 if not found</returns>
    public static int LastDotSpaceOrNewline(this string text)
    {
        int position = -1;
        int length = text.Length;

        bool insideQuotes = false;

        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                insideQuotes = !insideQuotes;
            }
            else if ((c == '.' || c == ' ' || c == '\n' || c == '\r') && !insideQuotes)
            {
                position = i;
            }
        }
        return position;
    }

    /// <summary>
    /// Calculates the balance of left parentheses over right parentheses that are not inside quoted text
    /// </summary>
    /// <param name="text">The text to analyze</param>
    /// <param name="length">The length to analyze, or -1 for full string</param>
    /// <returns>The balance of left parentheses (positive means more left than right)</returns>
    public static int LeftParenthesesBalance(this string text, int length = -1)
    {
        int balance = 0;
        if (length == -1)
        {
            length = text.Length;
        }

        bool insideQuotes = false;

        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == '(' && !insideQuotes)
            {
                balance++;
            }
            else if (c == ')' && !insideQuotes)
            {
                balance--;
            }

        }

        return balance;
    }

    /// <summary>
    /// Splits SQL text by separator, respecting parentheses and quoted strings
    /// </summary>
    /// <param name="sqlText">The SQL text to split</param>
    /// <param name="separator">The separator character (default: comma)</param>
    /// <returns>Array of split parts</returns>
    public static string[] SqlSplit(this string sqlText, char separator = ',')
    {
        List<string> parts = new List<string>();

        int parenthesesCount = 0;
        int singleQuoteCount = 0;
        int doubleQuoteCount = 0;

        int lastPosition = -1;
        int length = sqlText.Length;
        char c = (char)0;
        for (int i = 0; i < length; i++)
        {
            c = sqlText[i];
            if (c == separator && parenthesesCount == 0 && singleQuoteCount == 0 && doubleQuoteCount == 0)
            {
                parts.Add(sqlText.Substring(lastPosition + 1, i - lastPosition - 1));
                lastPosition = i;
            }
            else if (c == '(')
            {
                parenthesesCount++;
            }
            else if (c == ')')
            {
                parenthesesCount--;
            }
            else if (c == '\'')
            {
                singleQuoteCount = 1 - singleQuoteCount;
            }
            else if (c == '\"')
            {
                doubleQuoteCount = 1 - doubleQuoteCount;
            }
        }

        parts.Add(sqlText.Substring(lastPosition + 1));

        return parts.ToArray();
    }

    /// <summary>
    /// Splits SQL text by separator for SQL splitting operations, respecting parentheses and quoted strings
    /// </summary>
    /// <param name="sqlText">The SQL text to split</param>
    /// <param name="separator">The separator character (default: comma)</param>
    /// <returns>Array of split parts</returns>
    public static string[] SqlSplitAdvanced(this string sqlText, char separator = ',')
    {
        List<string> parts = new List<string>();

        int parenthesesCount = 0;
        int singleQuoteCount = 0;
        int doubleQuoteCount = 0;

        int lastPosition = -1;
        int length = sqlText.Length;
        char c = (char)0;
        for (int i = 0; i < length; i++)
        {
            c = sqlText[i];
            if (c == separator && parenthesesCount == 0 && singleQuoteCount == 0 && doubleQuoteCount == 0)
            {
                parts.Add(sqlText.Substring(lastPosition + 1, i - lastPosition - 1));
                lastPosition = i;
            }
            else if (c == '(')
            {
                parenthesesCount++;
            }
            else if (c == ')')
            {
                parenthesesCount--;
            }
            else if (c == '\'')
            {
                singleQuoteCount = 1 - singleQuoteCount;
            }
            else if (c == '\"')
            {
                doubleQuoteCount = 1 - doubleQuoteCount;
            }
            else if (c == '-' && i < length - 1 && sqlText[i + 1] == '-')
            {
                while (i < length && sqlText[i] != '\n')
                {
                    ++i;
                }
            }
            else if (c == '/' && i < length - 1 && sqlText[i + 1] == '*')
            {
                while (i < length - 1 && !(sqlText[i] == '*' && sqlText[i + 1] == '/'))
                {
                    ++i;
                }
            }
        }

        parts.Add(sqlText.Substring(lastPosition + 1));

        return parts.ToArray();
    }

    public static bool IsAllComments(this string txt)
    {
        int n = txt.Length;
        for (int i = 0; i < n; i++)
        {
            char c1 = txt[i];
            if (char.IsWhiteSpace(c1))
            {
                continue;
            }
            if (c1 != '-' && c1 != '/')
            {
                return false;
            }
            //if (c1 == '-' && i < n - 1)
            //{

            //}
            char c2 = txt[i + 1];
            if (i < n - 1 && c1 == '-' && c2 == '-')
            {
                do
                {
                    i++;
                } while (i < n && txt[i] != '\n');
            }
            else if (i < n - 1 && c1 == '/' && c2 == '*')
            {
                do
                {
                    i++;
                } while (i < n - 1 && (txt[i] != '*' || txt[i + 1] != '/'));
                i++;
            }
        }

        return true;
    }

    public static string[] Sqlparts(string sql)
    {
        int maxLen = 32_000;
        if (sql.Length < maxLen)
        {
            return new string[] { "", sql };
        }

        List<string> ll = new List<string>();
        ll.Add("");
        int m = (sql.Length - 1) / 32_000;

        for (int i = 0; i < m + 1; i++)
        {
            int start = i * 32000;
            int end = 32000 * (i + 1);
            if (end > sql.Length)
            {
                end = sql.Length;
            }

            ll.Add(sql[start..end]);
        }
        return ll.ToArray();
    }

    /// <summary>
    /// Extracts the last word from a string, handling SQL column expressions like "KOL AS K1", "KOL.K1", "KOL K1" -> "K1"
    /// </summary>
    /// <param name="text">The text to extract the last word from</param>
    /// <returns>The last word after dot, space, or newline</returns>
    public static string LastWord(this string text)
    {
        string result = text.Trim();
        result = result.Substring(result.LastDotSpaceOrNewline() + 1);
        return result;
    }

    static readonly Regex _rxAZ09 = RegexAZ09();
    static readonly Regex _rx2 = Regex2();
    static readonly Regex _rx3 = Regex3();
    /// <summary>
    /// Normalizes a name by removing special characters and ensuring it's a valid identifier
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <param name="reservedKeywords">List of reserved keywords to avoid</param>
    /// <returns>A normalized name safe for use as an identifier</returns>
    public static string NormalizeName(this string text, List<string> reservedKeywords)
    {
        string result = _rx2.Replace(_rxAZ09.Replace(
            text.Trim().ToUpper()
            .Replace('Ą', 'A')
            .Replace('Ć', 'C')
            .Replace('Ę', 'E')
            .Replace('Ł', 'L')
            .Replace('Ń', 'N')
            .Replace('Ó', 'O')
            .Replace('Ś', 'S')
            .Replace('Ż', 'Z')
            .Replace('Ź', 'Z')
            , "_"), "");

        if (result.Length >= 129)
        {
            result = result[0..126];
        }
        if (_rx3.IsMatch(result))
        {
            result = $"K{result}";
        }

        if (reservedKeywords.Contains(result.ToLower()))
        {
            result += '_';
        }

        return result;
    }

    /// <summary>
    /// Cuts a numeric string to specified precision after decimal point
    /// </summary>
    /// <param name="numericText">The numeric text to cut</param>
    /// <param name="precision">The number of decimal places to keep (default: 8)</param>
    /// <returns>The truncated numeric string</returns>
    public static string CutToLongNumeric(this string numericText, int precision = 8)
    {
        if (!numericText.Contains('.'))
        {
            return numericText;
        }
        else
        {
            int dotIndex = numericText.IndexOf('.');
            int length = numericText.Length;
            if (length - dotIndex - 1 <= 8)
            {
                return numericText;
            }
            else
            {
                string integerPart = numericText.Substring(0, dotIndex);
                string decimalPart = numericText.Substring(dotIndex + 1, precision);
                return $"{integerPart}.{decimalPart}";
            }
        }
    }

    private static StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Removes duplicate names from an array by appending numbers to duplicates
    /// </summary>
    /// <param name="list">The array of names to process</param>
    public static void RemoveDuplicates(string[] list)
    {
        Dictionary<string, (int count, int index)> dictionary = new Dictionary<string, (int, int)>(_comparer);

        for (int i = 0; i < list.Length; i++)
        {
            if (dictionary.ContainsKey(list[i]))
            {
                dictionary[list[i]] = (dictionary[list[i]].count + 1, dictionary[list[i]].index);
            }
            else
            {
                dictionary[list[i]] = (1, 0);
            }
        }

        for (int i = 0; i < list.Length; i++)
        {
            if (dictionary[list[i]].count > 1)
            {
                dictionary[list[i]] = (dictionary[list[i]].count, dictionary[list[i]].index + 1);
                list[i] = list[i] + "_" + dictionary[list[i]].index.ToString();
            }
        }
    }

    /// <summary>
    /// Checks if a string is a valid name (contains only uppercase letters, digits, and underscores)
    /// </summary>
    /// <param name="word">The word to validate</param>
    /// <returns>True if the word is a valid name, false otherwise</returns>
    public static bool IsGoodName(this string word)
    {
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            if (char.IsLower(c) || !char.IsLetter(c) && !char.IsDigit(c) && c != '_')
            {
                return false;
            }
        }
        return true;

    }

    private static Random _random = new Random();

    /// <summary>
    /// Generates a random name with timestamp and random letters
    /// </summary>
    /// <param name="startName">The prefix for the name (default: "export_")</param>
    /// <param name="randomLength">The length of random letters to append (default: 10)</param>
    /// <returns>A unique random name</returns>
    public static string RandomName(string startName = "export_", int randomLength = 10)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (string.IsNullOrEmpty(startName))
        {
            startName = "ABCDE_";
        }

        return startName + DateTime.Now.ToString("yyMMdd_HHmm") + new string(Enumerable.Repeat(letters, randomLength).Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Regex pattern to match non-alphanumeric characters (excluding underscore)
    /// </summary>
    [GeneratedRegex("[^a-zA-Z0-9_]", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex RegexAZ09();

    /// <summary>
    /// Regex pattern to match leading underscores
    /// </summary>
    [GeneratedRegex("^_*", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex Regex2();

    /// <summary>
    /// Regex pattern to match strings that don't start with a letter
    /// </summary>
    [GeneratedRegex("^[^a-zA-Z]", RegexOptions.Compiled)]
    private static partial Regex Regex3();
}

