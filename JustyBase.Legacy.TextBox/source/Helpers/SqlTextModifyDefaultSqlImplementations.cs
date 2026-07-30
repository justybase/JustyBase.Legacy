
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastColoredTextBoxNS.Helpers;

public sealed partial class SqlTextModifyDefaultSqlImplementations
{
    //controlling the "desire" for auto-suggestion
    private bool _wasPreviouslyDot  = false;
    private bool _multilineCharDeleted  =  false;

    private readonly IAutocompleteClass _autocompleteClass;
    private readonly IEditorConfig _editorConfig;

    public System.Windows.Forms.Timer TimerAutoCompletition { get; set; }
    public System.Windows.Forms.Timer TimerAutoCompletitionGeneral { get; set; }


    public SqlTextModifyDefaultSqlImplementations(IAutocompleteClass autocompleteClass, IEditorConfig editorConfig)
    {
        _autocompleteClass = autocompleteClass ?? throw new ArgumentNullException(nameof(autocompleteClass));
        _editorConfig = editorConfig ?? throw new ArgumentNullException(nameof(editorConfig));
    }

    public void TextChangingDefaultSqlImplementation(FastColoredTextBox fctb, TextChangingEventArgs e)
    {
        if (e.InsertingText == "\b" &&
            (fctb.Selection.CharBeforeStart == '\'' ||
            fctb.Selection.CharBeforeStart == '"' ||
            fctb.Selection.CharBeforeStart == '\\'
            ))
        {
            _multilineCharDeleted = true;
        }

        if (e.InsertingText == "." /*|| e.InsertingText == "@"*/)
        {
            (fctb.Tag as TbInfo).PopupMenu.MinFragmentLength = 1;
            (fctb.Tag as TbInfo).PopupMenu.AppearInterval = 50;
            _wasPreviouslyDot = true;
        }
        else if (/*e.InsertingText == "." ||*/ e.InsertingText == "@")
        {
            (fctb.Tag as TbInfo).PopupMenu.MinFragmentLength = 2;
            (fctb.Tag as TbInfo).PopupMenu.AppearInterval = 50;
            _wasPreviouslyDot = true;
        }
        else if (e.InsertingText == "&")
        {
            (fctb.Tag as TbInfo).PopupMenu.MinFragmentLength = 1;
            (fctb.Tag as TbInfo).PopupMenu.AppearInterval = 50;
            _wasPreviouslyDot = false;
        }
        else if (!_wasPreviouslyDot)
        {
            (fctb.Tag as TbInfo).PopupMenu.MinFragmentLength = 3;
            (fctb.Tag as TbInfo).PopupMenu.AppearInterval = _editorConfig.PopupMenuDefaultAppearInterval;

            if (e.InsertingText == " " || e.InsertingText == "\n" || e.InsertingText == "\r\n" || e.InsertingText == "\t")
            {
                int pos = fctb.SelectionStart;
                (var typoCandidate, var len) = fctb.CurrentWord(_editorConfig.CurrentWordLengthLimit);
                int l = len;

                if (_editorConfig.TypoCorrect && !IsCheapTypingPath(fctb))
                {
                    foreach (var correctWord in _editorConfig.TypoPatternList)
                    {
                        int dist = DamerauLevenshteinDistance(typoCandidate, correctWord);
                        if (dist <= _editorConfig.TypoLimit && dist >= 1) //default config.TypoLimit = 1 <=> 1 typo allowed
                        {
                            fctb.SelectionStart = pos - l;
                            fctb.SelectionLength = l;
                            fctb.InsertText(correctWord);
                            break;
                        }
                    }
                }

                // Quick snippets (SX -> SELECT) are O(1); keep enabled on large scripts.
                if (TryResolveQuickSnippet(typoCandidate, out string snippetText))
                {
                    fctb.SelectionStart = pos - l;
                    fctb.SelectionLength = l;

                    if (snippetText.Contains('^'))
                    {
                        string txt = snippetText;
                        int n = snippetText.IndexOf('^');

                        string c = "";
                        if (n > 0)
                        {
                            c = txt[n - 1].ToString();
                        }
                        else
                        {
                            c = " ";
                        }
                        string txt2 = (n > 0 ? txt.Substring(0, n - 1) : "") + (txt.Length > (n + 1) ? txt.Substring(n + 1) : "");

                        fctb.InsertText(txt2);
                        for (int i = 0; i < txt.Length - n - 1; i++)
                        {
                            if (!fctb.Selection.GoLeftThroughFolded())
                                break;
                        }
                        e.InsertingText = c;
                    }
                    else
                    {
                        fctb.InsertText(snippetText);
                    }
                }
            }
        }
        else
        {
            _wasPreviouslyDot = false;
        }
    }


    private static readonly string[] _multilineComments = new string[] { "/*", "*/" };
    private readonly HashSet<int> _editorsNeedingVisibleRestyle = [];
    private long _lastFullCommentScanTick;

    public string HandleSqlTextModification(TextChangedEventArgs e, FastColoredTextBox fctb, FctbColors fastColors, ref string empty
        , string cleanSqlText)
    {
        int editorId = RuntimeHelpers.GetHashCode(fctb);
        bool cheapPath = IsCheapTypingPath(fctb);
        bool needsVisibleRestyle = _editorsNeedingVisibleRestyle.Remove(editorId)
            || _multilineCharDeleted
            || e.ChangedRange.Text.ContainsAny(_multilineComments)
            || !MiscellaneousHelper.AreQuotesBalanced(e.ChangedRange.Chars);

        if (!needsVisibleRestyle)
            return cleanSqlText;

        return RecolorizeVisibleRange(fctb, fastColors, ref empty, cleanSqlText, cheapPath);
    }
    private string RecolorizeVisibleRange(FastColoredTextBox fctb, FctbColors fctbColors, ref string empty, string cleanSqlText, bool cheapPath)
    {
        Place p1 = fctb.VisibleRange.Start;
        Place p2 = fctb.VisibleRange.End;
        FastColoredTextBoxNS.Range r = new FastColoredTextBoxNS.Range(fctb, p1, p2);

        MiscellaneousHelper.UpdateAdditionStyles(r, fctbColors, _editorConfig.BracketFolding, cheapPath);

        // Full-document comment/string scan is the dominant cost on large scripts.
        // Keep it off the typing path; rebuild clean SQL only after a longer idle.
        if (!cheapPath)
        {
            using (SqlTypingPerfLocal.Measure("comment_scan", fctb.TextLength, fctb.LinesCount, "path=delayed-full"))
            {
                cleanSqlText = fctb.GetTextCommentRanges(fctbColors, ref empty, cleanSqlText);
            }
            _lastFullCommentScanTick = Environment.TickCount64;
        }
        else
        {
            // Large scripts: never run full-document comment/string rebuild while typing.
            // cleanSqlText refresh belongs to save/manual/outline refresh, not delayed idle.
            if (SqlTypingPerfLocal.Enabled)
            {
                Trace.WriteLine(
                    $"[SqlTypingPerf] op=editor.comment_scan phase=skipped chars={fctb.TextLength} lines={fctb.LinesCount} meta=path=delayed-cheap-typing");
            }
        }

        _multilineCharDeleted = false;
        return cleanSqlText;
    }

    private string _cleanSqlText = string.Empty;
    public string HandleTextChanged(FastColoredTextBox fctb, TextChangedEventArgs e, FctbColors fctbColors, ref string empty, string cleanSqlText,
    
        ref string currentColumn, bool isNetezza)
    {
        _cleanSqlText = cleanSqlText;
        bool cheapPath = IsCheapTypingPath(fctb);
        using (SqlTypingPerfLocal.Measure(
                   "handle_text_changed",
                   fctb.TextLength,
                   fctb.LinesCount,
                   cheapPath ? "cheap=1" : "cheap=0"))
        {
            MiscellaneousHelper.UpdateAdditionStyles(e.ChangedRange, fctbColors, _editorConfig.BracketFolding, cheapPath);

            // For large scripts, avoid restyling every visible line and skip the full-document
            // comment/string scan on the keystroke path — deferred to TextChangedDelayed.
            if (!cheapPath)
            {
                var rangesTmp = fctb.VisibleRange;
                int fromLine = rangesTmp.FromLine;
                int toLine = rangesTmp.ToLine;

                for (int i = fromLine; i < toLine; i++)
                {
                    if (fctb.LineInfos[i].VisibleState == VisibleState.Visible)
                    {
                        var range = new FastColoredTextBoxNS.Range(fctb, i);
                        MiscellaneousHelper.UpdateAdditionStyles(range, fctbColors, _editorConfig.BracketFolding);
                    }
                }

                using (SqlTypingPerfLocal.Measure("comment_scan", fctb.TextLength, fctb.LinesCount, "path=immediate"))
                {
                    _cleanSqlText = fctb.GetTextCommentRanges(fctbColors, ref empty, _cleanSqlText);
                }
                _lastFullCommentScanTick = Environment.TickCount64;
            }
            else
            {
                _editorsNeedingVisibleRestyle.Add(RuntimeHelpers.GetHashCode(fctb));
            }
        }

        if (isNetezza)
        {
            if (!cheapPath)
            {
                if (e.ChangedRange.Length < 200)
                {
                    string txt = e.ChangedRange.Text;

                    var m = _rx1.Match(e.ChangedRange.Text);
                    currentColumn = m.Groups["column"].Value;

                    if (!_wasPreviouslyDot)
                    {
                        bool isAftrerAs = false;
                        var sel = fctb.Selection.Start;

                        if (sel.iChar >= 2)
                        {
                            var thisLine = fctb.Lines[sel.iLine];
                            int num = sel.iChar - 1;

                            while (thisLine.Length > num && thisLine[num] != ' ' && num > 0)
                            {
                                num--;
                            }
                            if (num > thisLine.Length)
                            {
                                return _cleanSqlText;
                            }

                            for (int i = num; i >= 1; i--)
                            {
                                var c = thisLine[i];
                                if (c == ' ')
                                {
                                    continue;
                                }
                                else if ((c == 's' || c == 'S') && (thisLine[i - 1] == 'a' || thisLine[i - 1] == 'A'))
                                {
                                    if (thisLine[i - 1] == 'a' || thisLine[i - 1] == 'A')
                                    {
                                        isAftrerAs = true;
                                        break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if (isAftrerAs)
                        {
                            var tg = fctb.Tag as TbInfo;
                            if (tg is not null)
                            {
                                //https://github.com/KrzysztofDusko/Just-Data/issues/98
                                tg.PopupMenu.MinFragmentLength = 123;
                            }
                        }
                    }
                }
                else
                {
                    currentColumn = null;
                }

                if (TimerAutoCompletition == null)
                {
                    TimerAutoCompletition = new System.Windows.Forms.Timer();
                    TimerAutoCompletition.Interval = _editorConfig.GenerateToolTipTime;
                    TimerAutoCompletition.Tick += (s,e) => TimerTickMethod(s, fctb);
                }
                TimerAutoCompletition.Stop(); // Resets the timer
                TimerAutoCompletition.Start();
            }
        }
        else if (!fctb.Name.StartsWith("TXT") && !cheapPath)
        {
            if (TimerAutoCompletition == null)
            {
                TimerAutoCompletition = new System.Windows.Forms.Timer();
                TimerAutoCompletition.Interval = _editorConfig.GenerateToolTipTime;
                TimerAutoCompletition.Tick += (s, e) => OnGeneralTimerTick(s, new GeneralTickEventArgs(fctb), fctb);
            }

            TimerAutoCompletition.Stop(); // Resets the timer
            TimerAutoCompletition.Start();
        }
    
        return _cleanSqlText;
    }

    private bool IsCheapTypingPath(FastColoredTextBox fctb)
    {
        int charThreshold = _editorConfig.LargeScriptCharThreshold > 0
            ? _editorConfig.LargeScriptCharThreshold
            : 150_000;
        int lineThreshold = _editorConfig.LargeScriptLineThreshold > 0
            ? _editorConfig.LargeScriptLineThreshold
            : 500;
        return fctb.TextLength > charThreshold || fctb.LinesCount > lineThreshold;
    }

    private bool TryResolveQuickSnippet(string typoCandidate, out string snippetText)
    {
        snippetText = string.Empty;
        if (string.IsNullOrEmpty(typoCandidate))
            return false;

        if (_editorConfig.QuickSnippets.TryGetValue(typoCandidate, out snippetText))
            return true;

        return _editorConfig.QuickSnippets.TryGetValue(typoCandidate.ToUpperInvariant(), out snippetText);
    }

    private async void TimerTickMethod(object sender, FastColoredTextBox fastColoredTextBox)
    {
        if (sender is not System.Windows.Forms.Timer timer)
        {
            return;
        }
        // The timer must be stopped! We want to act only once per keystroke.
        timer.Stop();
        try
        {
            if (IsCheapTypingPath(fastColoredTextBox))
                return;

            await _autocompleteClass.AddAutocompleteForNZ(
                fastColoredTextBox.SelectionStart,
                _cleanSqlText);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

    }

    private async void OnGeneralTimerTick(object sender, GeneralTickEventArgs e, FastColoredTextBox fastColoredTextBox)
    {
        if (sender is not System.Windows.Forms.Timer timer)
            return;

        timer.Stop();
        try
        {
            await _autocompleteClass.AddAutocompleteForGeneral(fastColoredTextBox.SelectionStart, _cleanSqlText);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }


    class GeneralTickEventArgs : EventArgs
    {
        public FastColoredTextBox Fctb { get; set; }
        public GeneralTickEventArgs(FastColoredTextBox fctb)
        {
            this.Fctb = fctb;
        }
    }

    /// <summary>
    /// Returns the minimum of two integers
    /// </summary>
    private static int Minimum(int a, int b) => a < b ? a : b;

    /// <summary>
    /// Returns the minimum of three integers
    /// </summary>
    private static int Minimum(int a, int b, int c) => (a = a < b ? a : b) < c ? a : c;

    /// <summary>
    /// Calculates the Damerau-Levenshtein distance between two strings
    /// </summary>
    /// <param name="firstText">The first string to compare</param>
    /// <param name="secondText">The second string to compare</param>
    /// <returns>The edit distance between the two strings</returns>
    public static int DamerauLevenshteinDistance(string firstText, string secondText)
    {
        var n = firstText.Length + 1;
        var m = secondText.Length + 1;
        var arrayD = new int[n, m];


        for (var i = 0; i < n; i++)
        {
            arrayD[i, 0] = i;
        }

        for (var j = 0; j < m; j++)
        {
            arrayD[0, j] = j;
        }

        for (var i = 1; i < n; i++)
        {
            for (var j = 1; j < m; j++)
            {
                var cost = firstText[i - 1] == secondText[j - 1] ? 0 : 1;

                arrayD[i, j] = Minimum(arrayD[i - 1, j] + 1, // delete
                                                        arrayD[i, j - 1] + 1, // insert
                                                        arrayD[i - 1, j - 1] + cost); // replacement

                if (i > 1 && j > 1
                   && firstText[i - 1] == secondText[j - 2]
                   && firstText[i - 2] == secondText[j - 1])
                {
                    arrayD[i, j] = Minimum(arrayD[i, j],
                    arrayD[i - 2, j - 2] + cost); // permutation
                }
            }
        }

        return arrayD[n - 1, m - 1];
    }


    private static readonly Regex _rx1 = Regex1();
    [GeneratedRegex("[a-z_]+[0-9_]*\\.(?<column>[a-z0-9_]+)\\s+=\\s+[a-z_]+[0-9_]*\\.$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex Regex1();

}
