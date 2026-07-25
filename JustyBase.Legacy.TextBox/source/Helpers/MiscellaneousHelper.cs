using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastColoredTextBoxNS.Helpers;

public sealed partial class MiscellaneousHelper
{
    public static Regex? RxKeyWords1 { get; private set; }
    public static Regex? RxKeyWords2 { get; private set; }

    public static readonly Regex RxXlsx = XlsxRegex();

    public static void SetRegexKeyWords1(List<string> keyWords)
    {
        RxKeyWords1 = new Regex(
            @"\b(" + String.Join('|', keyWords) + @")\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(3));
    }
    public static void SetRegexKeyWords2(List<string> keyWords)
    {
        RxKeyWords2 = new Regex(
            @"\b(" + String.Join('|', keyWords) + @")\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(3));
    }

    public static void AddSqlAliases(FastColoredTextBox fctb, List<(string basicHint, string description)> collection)
    {
        string _fullTxt = null;

        if (fctb.Selection.Length >= 3)
        {
            string txt = fctb.Selection.Text;

            if (txt.Contains(' ') || txt.Contains('\n'))
            {
                //txt = Regex.Replace(txt, @"(?<prefix>([\s,\(,\)]|^){1})(?<column>[a-z0-9_]+)(?<postfix>([\s,\(,\)]|$){1})", MatchEvaluatorAliases, RegexOptions.IgnoreCase);
                //txt = Regex.Replace(txt, @"\b(?<wordBefore>[a-z0-9_]+)\s*\b(?<column>[a-z0-9_]+)\b", MatchEvaluatorAliases, RegexOptions.IgnoreCase);
                _fullTxt = txt;
                txt = Regex.Replace(txt, @"\b(?<column>[a-z0-9_]+)\b", MatchEvaluatorAliases, RegexOptions.IgnoreCase);
                _fullTxt = null;
            }
            else
            {
                var arr = collection.Where(o => o.basicHint.EndsWith("." + txt, StringComparison.OrdinalIgnoreCase)).Select(o => o.basicHint).ToArray();
                if (MiscellaneousHelper.RxKeyWords1.IsMatch(txt) || MiscellaneousHelper.RxKeyWords2.IsMatch(txt))
                {
                    MessageBox.Show($"{txt} is keyword");
                    return;
                }
                else if (arr.Length == 0)
                {
                    MessageBox.Show("No match");
                    return;
                }
                else if (arr.Length == 2)
                {
                    MessageBox.Show(String.Join('\n', arr));
                    return;
                }
                else
                {
                    txt = arr[0];
                }
            }

            fctb.InsertText(txt);
        }

        string MatchEvaluatorAliases(Match match)
        {
            if (match.Index > 0 && _fullTxt[match.Index - 1] == '.')
            {
                return match.Value;
            }

            string res = "";
            string col = match.Groups["column"].Value;
            //string wordBefore = match.Groups["wordBefore"].Value.ToUpper();
            int r = fctb.Selection.Text.Substring(0, match.Index).Count(c => c == '\n') * 2;

            if (MiscellaneousHelper.RxKeyWords1.IsMatch(col)
                || MiscellaneousHelper.RxKeyWords2.IsMatch(col)
                || fctb.Selection.Chars.ToArray()[match.Index - r /*+ wordBefore.Length + 1*/].style != StyleIndex.None
                //|| wordBefore == "WITH" || wordBefore == "AS" || wordBefore == "JOIN"
                )
            {
                return match.Value;
            }

            var arr = collection.Where(o => o.basicHint.EndsWith("." + col, StringComparison.OrdinalIgnoreCase)).Select(o => o.basicHint).ToArray();
            if (arr.Length == 0 || arr.Length == 2)
            {
                res = match.Value;
                return res;
            }
            else
            {
                res = " " + arr[0];
            }
            return res;
        }
    }

    public static bool CodeToTempTable(FastColoredTextBox fastColoredTextBox)
    {
        if (fastColoredTextBox.Selection.Length <= 3)
        {
            MessageBox.Show("Selection is too short.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        string txt = fastColoredTextBox.SelectedText;
        fastColoredTextBox.InsertText($"CREATE TEMP TABLE TABLENAME AS {Environment.NewLine}({Environment.NewLine}{txt}{Environment.NewLine}) DISTRIBUTE ON RANDOM;{Environment.NewLine}");
        return true;
    }

    public static void UpdateAdditionStyles(FastColoredTextBoxNS.Range range, FctbColors colorTheme, bool bracketFolding)
    {
        try
        {
            range.ClearStyle(colorTheme.ErrorStyle, colorTheme.WarningStyle, colorTheme.LintInfoStyle,
                colorTheme.KeyWordsStyle1, colorTheme.KeyWordsStyle2, colorTheme.BoldUnderlineStyle,
                colorTheme.NumberStyle, colorTheme.MyCommandsStyle, colorTheme.ParamStyle);
            range.SetStyle(colorTheme.NumberStyle, _rxNumericValues);

            range.SetStyle(colorTheme.BoldUnderlineStyle, _rxTableWithPodreslanie);
            range.SetStyle(colorTheme.BoldUnderlineStyle, _rxWithPodreslanie2); // , name AS (

            //SQL params highlighting
            range.SetStyle(colorTheme.ParamStyle, _rxZmienna);
            range.SetStyle(colorTheme.MyCommandsStyle, _rxSessionVariable);

            //keyword highlighting
            //// PERFORMANCE !
            range.SetStyle(colorTheme.KeyWordsStyle1, RxKeyWords1);
            range.SetStyle(colorTheme.KeyWordsStyle2, RxKeyWords2);

            //myCommandsStyle
            range.SetStyle(colorTheme.MyCommandsStyle, _rxMyCommands);
            range.SetStyle(colorTheme.MyCommandsStyle, RxXlsx);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Syntax highlighting refresh failed ({ex.GetType().Name}).");
        }
        //clear folding markers
        range.ClearFoldingMarkers();

        //set folding markers
        range.SetFoldingMarkers(@"--[ \t]*region\b", @"--[ \t]*end[ \t]{0,1}region\b", RegexOptions.IgnoreCase);//allow to collapse --region blocks
        range.SetFoldingMarkers(@"{", @"}");
        if (bracketFolding)
        {
            range.SetFoldingMarkers(@"\(", @"\)");//allow to collapse brackets block
            range.SetFoldingMarkers(@"/\*", @"\*/");//allow to collapse comment block
        }
    }

    public static bool AreQuotesBalanced(IEnumerable<FastColoredTextBoxNS.Char> chars)
    {
        bool n1 = true;
        bool n2 = true;
        foreach (var item in chars)
        {
            if (item.c == '\'')
            {
                n1 = !n1;
            }
            else if (item.c == '"')
            {
                n2 = !n2;
            }
        }
        return n1 && n2;
    }



    [GeneratedRegex(@"__xlsx (?<filepath>.*\.xlsx)(;|\s)*$", RegexOptions.IgnoreCase, "pl-PL")]
    private static partial Regex XlsxRegex();

    [GeneratedRegex("\\b\\d+[\\.]?\\d*([eE]\\-?\\d+)?[lLdDfF]?\\b|\\b0x[a-fA-F\\d]+\\b", RegexOptions.Compiled)]
    private static partial Regex RegexNumericValues();
    private static readonly Regex _rxNumericValues = RegexNumericValues();


    static readonly Regex _rxTableWithPodreslanie = RegexTableWithPodreslanie();

    [GeneratedRegex("\\b(with|create external table|create temp table|create table|drop table)\\s+(?<range>\\w+?)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexTableWithPodreslanie();

    static readonly Regex _rxWithPodreslanie2 = RegexWithPodreslanie2();

    [GeneratedRegex(",\\s*(?<range>\\w+?)\\s+as\\s+\\(", RegexOptions.Compiled)]
    private static partial Regex RegexWithPodreslanie2();

    private static readonly Regex _rxZmienna = RegexZmienna();
    [GeneratedRegex("(?<zmienna>\\$[a-zA-Z]{1}[a-zA-Z_\\d]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexZmienna();
    private static readonly Regex _rxSessionVariable = RegexSessionVariable();

    [GeneratedRegex("&[a-z]{1}[a-z_\\d]*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexSessionVariable();

    private static readonly Regex _rxMyCommands = RegexMyCommands();

    [GeneratedRegex("(\\b(__Let|__LetFor|___imp|___impOleDb|___impODBC|___impDB2|___impSQLLite|___run|___expCsv|___expXlsx)\\b|(#myCredentials\\b))", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
    private static partial Regex RegexMyCommands();



}
