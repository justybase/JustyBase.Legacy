using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using System.Text.RegularExpressions;

namespace AppBase.Services.Sql;

public sealed class NetezzaSqlErrorHighlighter
{
    private Regex _atributeNotFoundRegex;
    private Regex _exceptAtAchar1;
    private Regex _exceptionIncorrectType;
    private Regex _transformColumnType;
    private Regex _groomError;
    private Regex _repeatedError;
    private Regex _alreadyExistsError;
    private Regex _notExistsError;
    private Regex _permissionError;
    private Regex _functionError;
    private Regex _groupError1;
    private Regex _groupError2;
    private Regex _wrongOption;
    private Regex _wrongSet;
    private Regex _manySameAliases;
    private Regex _ambiguousError;
    private Regex _couldNotacquire;

    public sealed record HighlightMatch(string Word, bool UseRegex2, int SelectionStart);

    public bool TryGetHighlight(
        string msg,
        bool fromOleDb,
        string sqlText,
        ReadOnlySpan<char> sqlSlice,
        int selectionStart,
        out HighlightMatch match)
    {
        EnsureRegexInitialized();

        match = null;
        bool regex2 = false;
        int effectiveSelectionStart = selectionStart;

        if (!fromOleDb && msg.StartsWith("ERROR [42000] ERROR:") && msg.Contains(" ^ found \"") || fromOleDb && msg.Contains(" ^ found \""))
        {
            int m = msg.IndexOf("^ found");
            int m1 = msg.IndexOf("at char ", m);
            int m2 = msg.IndexOf(")", m1);
            string wrongText = msg[(m + 9)..(m1 - 3)];
            match = new HighlightMatch(wrongText, regex2, effectiveSelectionStart);
            return true;
        }

        if (TryRegexMatch(_wrongSet, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_atributeNotFoundRegex, msg, "name", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryExceptAtCharMatch(msg, sqlSlice, selectionStart, ref effectiveSelectionStart, out match)) return true;
        if (TryRegexMatch(_exceptionIncorrectType, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_transformColumnType, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_groomError, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_repeatedError, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_alreadyExistsError, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_notExistsError, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_functionError, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;

        if (_groupError1.IsMatch(msg))
        {
            var m = _groupError1.Match(msg);
            if (!string.IsNullOrEmpty(m.Groups["found"].Value) &&
                sqlText.Contains(m.Groups["found"].Value, StringComparison.OrdinalIgnoreCase))
            {
                match = new HighlightMatch(m.Groups["found"].Value, regex2, effectiveSelectionStart);
                return true;
            }

            if (_groupError2.IsMatch(msg))
            {
                m = _groupError2.Match(msg);
                match = new HighlightMatch(m.Groups["found"].Value, regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (TryRegexMatch(_wrongOption, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;
        if (TryRegexMatch(_manySameAliases, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;

        if (_ambiguousError.IsMatch(msg))
        {
            var m = _ambiguousError.Match(msg);
            match = new HighlightMatch(m.Groups["found"].Value, true, effectiveSelectionStart);
            return true;
        }

        if (TryRegexMatch(_couldNotacquire, msg, "found", ref regex2, ref effectiveSelectionStart, sqlSlice, selectionStart, out match)) return true;

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR:  Permission denied on "))
        {
            int m1 = msg.IndexOf('"');
            int m2 = msg.LastIndexOf('"');
            if (m1 != -1 && m2 != -1)
            {
                match = new HighlightMatch(msg[(m1 + 1)..m2], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR: ") &&
            Regex.IsMatch(msg, @"object ""(?<objectname>[a-z0-9_\.""]+)"" already exists", RegexOptions.IgnoreCase))
        {
            var r = Regex.Match(msg, @"object ""(?<objectname>[a-z0-9_\.""]+)"" already exists", RegexOptions.IgnoreCase);
            match = new HighlightMatch(r.Groups["objectname"].Value, regex2, effectiveSelectionStart);
            return true;
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR: ") &&
            Regex.IsMatch(msg, @"Schema '(?<objectname>[a-z0-9_\.""]+)' does not exist", RegexOptions.IgnoreCase))
        {
            var r = Regex.Match(msg, @"Schema '(?<objectname>[a-z0-9_\.""]+)' does not exist", RegexOptions.IgnoreCase);
            match = new HighlightMatch(r.Groups["objectname"].Value, regex2, effectiveSelectionStart);
            return true;
        }

        if (!fromOleDb && msg.StartsWith("ERROR [42S02] ERROR:"))
        {
            int m1 = msg.LastIndexOf('.');
            if (m1 != -1)
            {
                match = new HighlightMatch(msg[(m1 + 1)..], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [42S22] ERROR:"))
        {
            const string version1 = "ERROR [42S22] ERROR:  Attribute '";
            if (msg.StartsWith(version1))
            {
                int a1 = msg.IndexOf('\'', version1.Length + 1);
                match = new HighlightMatch(msg[version1.Length..a1], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR:  GROOM VERSIONS must be run on "))
        {
            int i1 = msg.IndexOf(" before");
            int i2 = "ERROR [HY000] ERROR:  GROOM VERSIONS must be run on ".Length;
            if (i1 > 0)
            {
                match = new HighlightMatch(msg[i2..i1], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR:  Attribute ") && msg.Contains(" is repeated"))
        {
            int i1 = msg.IndexOf(" is repeated");
            int i2 = "ERROR [HY000] ERROR:  Attribute ".Length;
            if (i1 > 0)
            {
                match = new HighlightMatch(msg[(i2 + 1)..(i1 - 1)], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR:  Attribute ") && msg.Contains(" must be GROUPed"))
        {
            int i1 = msg.IndexOf(" must be GROUPed");
            int i2 = "ERROR [HY000] ERROR:  Attribute ".Length;
            if (i1 > 0)
            {
                match = new HighlightMatch(msg[i2..i1], regex2, effectiveSelectionStart);
                return true;
            }
        }

        if (!fromOleDb && msg.StartsWith("ERROR [HY000] ERROR:  ") && msg.Contains(" is not a valid option name"))
        {
            int i1 = msg.IndexOf(" is not a valid option name");
            int i2 = "ERROR [HY000] ERROR:  ".Length;
            if (i1 > 0)
            {
                match = new HighlightMatch(msg[(i2 + 1)..(i1 - 1)], regex2, effectiveSelectionStart);
                return true;
            }
        }

        return false;
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
        {
            return;
        }

        string currentSqlText = fctb.TextFast;
        if (selectionStart >= currentSqlText.Length)
        {
            // The editor text can change while a query is executing.  In that
            // case the range captured before execution is no longer valid.
            return;
        }

        int availableLength = currentSqlText.Length - selectionStart;
        int safeSelectionLength = Math.Min(selectionLength, availableLength);
        ReadOnlySpan<char> sqlSlice = currentSqlText.AsSpan(selectionStart, safeSelectionLength);
        if (!TryGetHighlight(msg, fromOleDb, fctb.Text, sqlSlice, selectionStart, out HighlightMatch match))
        {
            return;
        }

        int founded = fctb.ColorizeErrorWord(errorStyle, match.SelectionStart, safeSelectionLength, match.Word, match.UseRegex2);
        if (founded != -1 && fctb.TextLength > founded)
        {
            fctb.SelectionStart = founded;
            fctb.SelectionLength = 0;
            fctb.DoSelectionVisible();
        }
    }

    private bool TryExceptAtCharMatch(
        string msg,
        ReadOnlySpan<char> sqlSlice,
        int selectionStart,
        ref int effectiveSelectionStart,
        out HighlightMatch match)
    {
        match = null;
        if (!_exceptAtAchar1.IsMatch(msg))
        {
            return false;
        }

        var m = _exceptAtAchar1.Match(msg);
        int number = int.Parse(m.Groups["charNum"].Value) - 1;
        int leadingWhiteNum = 0;
        while (leadingWhiteNum < sqlSlice.Length &&
               (sqlSlice[leadingWhiteNum] == '\r' || sqlSlice[leadingWhiteNum] == '\n' || sqlSlice[leadingWhiteNum] == ' '))
        {
            leadingWhiteNum++;
        }

        effectiveSelectionStart = selectionStart + number + leadingWhiteNum;
        match = new HighlightMatch(m.Groups["found"].Value, false, effectiveSelectionStart);
        return true;
    }

    private static bool TryRegexMatch(
        Regex regex,
        string msg,
        string groupName,
        ref bool regex2,
        ref int effectiveSelectionStart,
        ReadOnlySpan<char> sqlSlice,
        int selectionStart,
        out HighlightMatch match)
    {
        match = null;
        if (!regex.IsMatch(msg))
        {
            return false;
        }

        var m = regex.Match(msg);
        match = new HighlightMatch(m.Groups[groupName].Value, regex2, effectiveSelectionStart);
        return true;
    }

    private void EnsureRegexInitialized()
    {
        if (_atributeNotFoundRegex is not null)
        {
            return;
        }

        _atributeNotFoundRegex = new Regex(@"ERROR: Attribute '(?<name>.*)' not found");
        _exceptAtAchar1 = new Regex(@"\^ found ""(?<found>.*)"" \(at char (?<charNum>[0-9]+)\) expecting");
        _exceptionIncorrectType = new Regex(@"^ERROR: DROP (TABLE|VIEW): object ""(?<found>.*)"", incorrect type\.$");
        _transformColumnType = new Regex(@"^ERROR: transformColumnType: error reading type '(?<found>.*)'$");
        _groomError = new Regex(@"^ERROR: GROOM VERSIONS must be run on (?<found>.*) before any other GROOM operation$");
        _repeatedError = new Regex(@"^ERROR: Attribute '(?<found>.*)' is repeated. Must have an appropriate alias\.$");
        _alreadyExistsError = new Regex(@"^ERROR: CREATE TABLE: object ""(?<found>.*)"" already exists\.$");
        _notExistsError = new Regex(@"^ERROR: relation does not exist (?<db>[^.]*)\.?(?<schema>[^.]*)\.?(?<found>.*)$");
        _permissionError = new Regex(@"^ERROR: Permission denied on ""(?<found>.*)""\.$");
        _functionError = new Regex(@"^ERROR: Function '(?<found>.*)\(.*\)' does not exist");
        _groupError1 = new Regex(@"^ERROR: Attribute (?<found>.*) must be GROUPed or used in an aggregate function$");
        _groupError2 = new Regex(@"^ERROR: Attribute (?<table>[^\.]*)\.(?<found>.*) must be GROUPed or used in an aggregate function$");
        _wrongOption = new Regex(@"^ERROR: Option '(?<found>.*)' is not recognized$");
        _wrongSet = new Regex(@"^ERROR: 'SET (?<found>.*)'");
        _manySameAliases = new Regex(@"^ERROR: Table name ""(?<found>.*)"" specified more than once$");
        _ambiguousError = new Regex(@"^ERROR: Column reference ""(?<found>.*)"" is ambiguous$");
        _couldNotacquire = new Regex(@"^ERROR: DROP DATABASE: could not acquire lock for ""(?<found>.*)""$");
    }
}
