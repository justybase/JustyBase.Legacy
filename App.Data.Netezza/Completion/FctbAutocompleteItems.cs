using AppBase.Common;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using System.Text.RegularExpressions;

namespace AppBase.Data;

public sealed class AutocompleteItem2 : AutocompleteItem
{
    public AutocompleteItem2(string text) : base(text)
    {

    }

    public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
    {
        if (!e.Item.Text.Contains('^'))
        {
            return;
        }

        e.Tb.BeginUpdate();
        e.Tb.Selection.BeginUpdate();
        var p1 = popupMenu.Fragment.Start;
        e.Tb.Selection.Start = p1;
        while (e.Tb.Selection.CharBeforeStart != '^')
            if (!e.Tb.Selection.GoRightThroughFolded())
                break;
        if (e.Tb.Selection.CharBeforeStart == '^')
        {
            e.Tb.Selection.GoLeft(true);
            e.Tb.InsertText("");
        }

        e.Tb.Selection.EndUpdate();
        e.Tb.EndUpdate();
    }

    public override string ToString()
    {
        return MenuText ?? Text.Replace("\n", " ").Replace("^", "");
    }
}

public sealed class MonkeySnippets : AutocompleteItem
{
    readonly string text_;
    readonly string mToSpace;
    readonly string aftreSpace;
    public MonkeySnippets(string text) : base(text)
    {
        this.text_ = text;
        int n = text_.IndexOf(' ');
        if (n > text_.Length - 3)
        {
            n = text_.Length - 3;
        }
        this.mToSpace = text_.Substring(2, n + 1);
        this.aftreSpace = text_.Substring(n + 1);
    }

    public override CompareResult Compare(string fragmentText)
    {
        if (text_.StartsWith(fragmentText, StringComparison.OrdinalIgnoreCase))
            return CompareResult.VisibleAndSelected;

        if (mToSpace.Contains(fragmentText.Substring(2), StringComparison.OrdinalIgnoreCase))
            return CompareResult.Visible;

        return CompareResult.Hidden;
    }

    public override string GetTextForReplace()
    {
        return aftreSpace;
    }

    public override string ToString()
    {
        return MenuText ?? Text.Replace("\n", " ").Replace("^", "");
    }

    public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
    {
        if (!e.Item.Text.Contains('^'))
        {
            return;
        }

        e.Tb.BeginUpdate();
        e.Tb.Selection.BeginUpdate();
        var p1 = popupMenu.Fragment.Start;
        e.Tb.Selection.Start = p1;
        while (e.Tb.Selection.CharBeforeStart != '^')
            if (!e.Tb.Selection.GoRightThroughFolded())
                break;
        if (e.Tb.Selection.CharBeforeStart == '^')
        {
            e.Tb.Selection.GoLeft(true);
            e.Tb.InsertText("");
        }

        e.Tb.Selection.EndUpdate();
        e.Tb.EndUpdate();
    }

}

public sealed class MethodAutocompleteItem2 : MethodAutocompleteItem
{
    public string firstPart;
    public string lastPart;
    public MethodAutocompleteItem2(string text)
        : base(text)
    {
        var i = text.LastDot();
        if (i < 0)
            firstPart = text;
        else
        {
            firstPart = text.Substring(0, i);
            lastPart = text.Substring(i + 1);
        }
    }

    public override CompareResult Compare(string fragmentText)
    {
        var i = fragmentText.LastDot();
        if (i < 0)
        {
            if (firstPart.StartsWith(fragmentText, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(lastPart))
                return CompareResult.VisibleAndSelected;
        }
        else
        {
            var fragmentFirstPart = fragmentText.Substring(0, i);
            var fragmentLastPart = fragmentText.Substring(i + 1);


            if (!firstPart.Equals(fragmentFirstPart, StringComparison.OrdinalIgnoreCase))
                return CompareResult.Hidden;

            if (lastPart != null && lastPart.StartsWith(fragmentLastPart, StringComparison.OrdinalIgnoreCase))
                return CompareResult.VisibleAndSelected;

            if (lastPart != null && lastPart.Contains(fragmentLastPart, StringComparison.OrdinalIgnoreCase))
                return CompareResult.Visible;

        }

        return CompareResult.Hidden;
    }

    public override string GetTextForReplace()
    {
        if (lastPart == null)
            return firstPart;

        return firstPart + "." + lastPart;
    }

    public override string ToString()
    {
        if (lastPart == null)
            return firstPart;

        return lastPart;
    }

}

sealed partial class InsertSpaceSnippet : AutocompleteItem
{
    private readonly Regex _rxPattern;
    private static readonly Regex _space0 = RegexSpace0();

    public InsertSpaceSnippet(Regex rxPattern) : base("")
    {
        this._rxPattern = rxPattern;
    }

    public InsertSpaceSnippet()
        : this(_space0)
    {
    }
    public override CompareResult Compare(string fragmentText)
    {
        try
        {
            if (_rxPattern.IsMatch(fragmentText))
            {
                Text = InsertSpaces(fragmentText);
                if (Text != fragmentText)
                    return CompareResult.Visible;
            }
        }
        catch (Exception)
        {
            return CompareResult.Hidden;
        }

        return CompareResult.Hidden;
    }

    public string InsertSpaces(string fragment)
    {
        var m = _rxPattern.Match(fragment);
        if (m == null)
            return fragment;
        if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
            return fragment;
        return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
    }

    public override string ToolTipTitle
    {
        get
        {
            return Text;
        }
    }

    [GeneratedRegex("^(\\d+)([a-zA-Z_]+)(\\d*)$", RegexOptions.Compiled)]
    private static partial Regex RegexSpace0();
}
