using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS.Helpers;

public static class MiscellaneousExtensions
{
    public static (string word, int len) CurrentWord(this FastColoredTextBox fctb, int currentWordLimit)
    {
        var place = fctb.Selection.Start;
        var span = fctb.Lines[place.iLine].AsSpan();
        int pos = place.iChar;
        int l = 0;
        if (pos > span.Length)
        {
            return ("", l);
        }

        while (pos > 0 && span[pos - 1] != ' ' && span[pos - 1] != '\n' && span[pos - 1] != '\r' && span[pos - 1] != '\t' && span[pos - 1] != '(')
        {
            pos--;
            l++;
            if (l > currentWordLimit)
            {
                break;
            }
        }
        return (span.Slice(pos, l).ToString().ToUpper(), l);
    }
    public static void SelectBetweenSemicolons(this FastColoredTextBox fctb)
    {
        fctb.Selection.Start = SemicolonStart(fctb);
        fctb.Selection.End = SemicolonEnd(fctb);
        fctb.Invalidate();
    }
    private static Place SemicolonStart(FastColoredTextBox tb)
    {
        FastColoredTextBoxNS.Range range = tb.Selection.Clone();//need to clone because we will move caret
        while (range.GoLeftThroughFolded())
        {
            if (range.CharAfterStart == ';' && tb.GetStyleIndexMask(tb.GetStylesOfChar(range.Start).ToArray()) == StyleIndex.None)
            {
                range.GoRightThroughFolded();
                return range.Start;
            }
        } //while (range.GoLeftThroughFolded()); //move caret left
        return tb.Range.Start;
    }
    private static Place SemicolonEnd(FastColoredTextBox tb)
    {
        FastColoredTextBoxNS.Range range = tb.Selection.Clone();//need to clone because we will move caret
        do
        {
            if (range.CharAfterStart == ';' && tb.GetStyleIndexMask(tb.GetStylesOfChar(range.Start).ToArray()) == StyleIndex.None)
            {
                return range.Start;
            }
        } while (range.GoRightThroughFolded());//move caret right
        return tb.Range.End;
    }

    public static void GetTextCommentRanges(FctbColors colorTheme, string txt, ref string res)
    {
        FastColoredTextBox fastColored = new FastColoredTextBox();
        fastColored.Text = txt;
        string resultString = "";
        resultString = fastColored.GetTextCommentRanges(colorTheme, ref res, resultString);
    }

    public static string GetTextCommentRanges(this FastColoredTextBox fctb, FctbColors colorTheme, ref string res, string cleanSql)
    {
        var stringStyles = colorTheme.StringsStyle;
        var quotedTextStyle = colorTheme.QuotedTextStyle;
        var commentsStyle = colorTheme.CommentsStyle;
        string tempString = String.Create(fctb.TextLength, fctb, (chars, fctbInner) =>
        {
            CharType ct = CharType.NormalText;
            CharType ctPrev = CharType.NormalText;

            FastColoredTextBoxNS.Char prevChar = new FastColoredTextBoxNS.Char();

            StyleIndex layerString = FastColoredTextBoxNS.Range.ToStyleIndex(fctbInner.GetOrSetStyleLayerIndex(stringStyles));
            StyleIndex layerQuoted = FastColoredTextBoxNS.Range.ToStyleIndex(fctbInner.GetOrSetStyleLayerIndex(quotedTextStyle));
            StyleIndex layerComment = FastColoredTextBoxNS.Range.ToStyleIndex(fctbInner.GetOrSetStyleLayerIndex(commentsStyle));

            int linesCnt = fctbInner.LinesCount;
            int num = 0;
            for (int i = 0; i < linesCnt; i++)
            {
                var ll = fctbInner[i];
                int m = ll.Count;

                for (int j = 0; j < m; j++)
                {
                    var c = ll[j];
                    char cc = c.c;

                    if (ct == CharType.NormalText)
                    {
                        switch (cc)
                        {
                            case '\'':
                                ct = CharType.SqlString;
                                break;
                            case '"':
                                ct = CharType.QuotedText;
                                break;
                            case '-':
                                if (j < m - 1 && ll[j + 1].c == '-')
                                {
                                    ct = CharType.CommentOneLine;
                                }
                                break;
                            case '/':
                                if (j < m - 1 && ll[j + 1].c == '*')
                                {
                                    ct = CharType.CommentMultiLine;
                                }
                                break;
                            default:
                                break;
                        }
                        ctPrev = ct;
                    }
                    else if (ct == CharType.SqlString && cc == '\'')
                    {
                        ctPrev = CharType.SqlString;
                        ct = CharType.NormalText;
                    }
                    else if (ct == CharType.QuotedText && cc == '"')
                    {
                        ctPrev = CharType.QuotedText;
                        ct = CharType.NormalText;
                    }
                    else if (ct == CharType.CommentOneLine && j == m - 1)
                    {
                        ctPrev = CharType.CommentOneLine;
                        ct = CharType.NormalText;
                    }
                    else if (ct == CharType.CommentMultiLine && cc == '*' && j < m - 1 && ll[j + 1].c == '/')
                    {
                        ctPrev = CharType.CommentMultiLine;
                        ct = CharType.NormalText;

                        prevChar.c = cc;
                        prevChar.style = c.style;
                    }

                    if (ctPrev == CharType.SqlString)
                    {
                        c.style = layerString;
                        ll[j] = c;
                        chars[num++] = ' ';
                    }
                    else if (ctPrev == CharType.QuotedText)
                    {
                        c.style = layerQuoted;
                        ll[j] = c;
                        chars[num++] = cc;
                    }
                    else if (ctPrev == CharType.CommentOneLine)
                    {
                        c.style = layerComment;
                        ll[j] = c;
                        chars[num++] = ' ';
                    }
                    else if (ctPrev == CharType.CommentMultiLine)
                    {
                        c.style = layerComment;
                        prevChar.style = layerComment;
                        ll[j] = c;
                        chars[num++] = ' ';
                    }
                    else if (c.c == '/' && prevChar.c == '*' && prevChar.style == layerComment)
                    {
                        //StyleIndex layer = FastColoredTextBoxNS.Range.ToStyleIndex(fctb.GetOrSetStyleLayerIndex(commentsStyle));
                        c.style = layerComment;
                        ll[j] = c;
                        chars[num++] = ' ';
                    }
                    else
                    {
                        bool s1 = (c.style & layerString) == StyleIndex.None;
                        bool s2 = (c.style & layerQuoted) == StyleIndex.None;
                        bool s3 = (c.style & layerComment) == StyleIndex.None;
                        if (!s1 || !s2 || !s3)
                        {
                            //fctb.ClearStyle(stringsStyle)
                            if (!s1)
                            {
                                c.style &= ~layerString;
                            }
                            if (!s2)
                            {
                                c.style &= ~layerQuoted;
                            }
                            if (!s3)
                            {
                                c.style &= ~layerComment;
                            }
                            ll[j] = c;
                        }

                        chars[num++] = cc;
                    }

                    ctPrev = ct;
                }

                if (i < linesCnt - 1)
                {
                    chars[num++] = '\r';
                    chars[num++] = '\n';
                }
            }
        });
        if (res.Length > 0)
        {
            res = tempString;
            return cleanSql;
        }
        cleanSql = tempString;
        return cleanSql;
    }
    
    public static int ColorizeErrorWord(this FastColoredTextBox fctb, TextStyle errorStyle, int boundStart, int boundLength, string word, bool regex2 = false)
    {
        int linesCnt = fctb.LinesCount;
        word = Regex.Escape(word);

        Regex rxError;
        if (regex2)
        {
            rxError = new Regex($"[^\\.a-z]{word}(\\b|\\s|\\W|$)", RegexOptions.IgnoreCase);
        }
        else
        {
            rxError = new Regex($"(\\b|\\s|\\W|^){word}(\\b|\\s|\\W|$)", RegexOptions.IgnoreCase);
        }

        StyleIndex layerError = FastColoredTextBoxNS.Range.ToStyleIndex(fctb.GetOrSetStyleLayerIndex(errorStyle));

        int chars = 0;
        for (int i = 0; i < linesCnt; i++)
        {
            var ll = fctb[i];
            int lineCount = ll.Count;

            if (boundStart > chars + lineCount + 2 + 1)
            {
                chars += lineCount + 2;
                continue;
            }
            if (chars > boundStart + boundLength)
            {
                break;
            }
            else if (lineCount >= word.Length)
            {
                var match = rxError.Match(ll.Text);
                if (match.Success)
                {
                    for (int j = match.Index; j < match.Index + word.Length && j < lineCount; j++)
                    {
                        var c = ll[j];
                        c.c = ll[j].c;
                        c.style = layerError;
                        ll[j] = c;
                    }
                    return chars + match.Index;
                }
            }
        }

        return -1;
    }

    public static bool ContainsAny(this string source, string[] values, StringComparison comparison = StringComparison.CurrentCulture)
    {
        return values.Any(value => source.IndexOf(value, comparison) >= 0);
    }


}
