using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastColoredTextBoxNS.Helpers;

public sealed partial class SqlIndenting
{
    private static readonly Regex _rxJoin = RegexJoin();
    private static readonly Regex _rxInd1 = RegexInd1();
    private static readonly Regex _rxInd2 = RegexInd2();
    private static readonly Regex _rxInd3 = RegexInd3();
    private static readonly Regex _rxInd4 = RegexInd4();
    private static readonly Regex _rxInd5 = RegexInd5();
    private static readonly Regex _rxInd6 = RegexInd6();
    private static readonly Regex _rxIndAndOr = RegexIndAndOr();

    [GeneratedRegex("(\\b|\\s|\\n|\\W)join(\\b|\\s|\\n|\\W)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexJoin();

    [GeneratedRegex("\\([^\\(\\)]*\\)\\s*($|--)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd1();
    [GeneratedRegex("\\(\\s*($|--)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd2();
    [GeneratedRegex("\\)\\s*($|--)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd3();
    [GeneratedRegex("^\\s*(select|case)\\b\\s*($|--)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd4();
    [GeneratedRegex("^\\s*(from|where|group by|having|order by|limit\\s*\\d*)\\s*($|--)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd5();
    [GeneratedRegex("^\\s*(end\\b|;).*($|--)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexInd6();
    [GeneratedRegex("^\\s*(and|or|like).*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RegexIndAndOr();

    public void SqlDefaultIndenting(AutoIndentEventArgs args)
    {
        //parentheses
        //block ()
        if (_rxIndAndOr.IsMatch(args.LineText) && (_rxJoin.IsMatch(args.PrevLineText) || _rxIndAndOr.IsMatch(args.PrevLineText)))
        {
            if (_rxIndAndOr.IsMatch(args.PrevLineText))
            {
                int prevLineSpaces = args.PrevLineText.TakeWhile(System.Char.IsWhiteSpace).Count();
                int propInd = prevLineSpaces - args.AbsoluteIndentation;
                args.Shift = propInd >= 0 ? propInd : args.TabLength;
                return;
            }
            args.Shift = args.TabLength;
            return;
        }
        if (_rxInd1.IsMatch(args.LineText))
            return;
        //start of block ()
        if (_rxInd2.IsMatch(args.LineText))
        {
            args.ShiftNextLines = args.TabLength;
            return;
        }
        //end of block ()
        if (_rxInd3.IsMatch(args.LineText))
        {
            args.Shift = -args.TabLength;
            args.ShiftNextLines = -args.TabLength;
            return;
        }
        //behavior with select etc.
        //select
        if (_rxInd4.IsMatch(args.LineText))
        {
            args.ShiftNextLines = args.TabLength;
            return;
        }
        //from
        if (_rxInd5.IsMatch(args.LineText))
        {
            args.Shift = -args.TabLength;
            //args.ShiftNextLines = -args.TabLength;
            return;
        }
        if (_rxInd6.IsMatch(args.LineText))
        {
            args.Shift = -args.TabLength;
            args.ShiftNextLines = -args.TabLength;
            return;
        }


        return;
    }
}
