using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

internal static class AnsiParser
{
    private static readonly Color[] StandardColors =
    [
        Color.FromArgb(0, 0, 0),       // Black
        Color.FromArgb(170, 0, 0),     // Red
        Color.FromArgb(0, 170, 0),     // Green
        Color.FromArgb(170, 85, 0),    // Yellow
        Color.FromArgb(0, 0, 170),     // Blue
        Color.FromArgb(170, 0, 170),   // Magenta
        Color.FromArgb(0, 170, 170),   // Cyan
        Color.FromArgb(170, 170, 170), // White
    ];

    private static readonly Color[] BrightColors =
    [
        Color.FromArgb(85, 85, 85),      // Bright Black
        Color.FromArgb(255, 85, 85),     // Bright Red
        Color.FromArgb(85, 255, 85),     // Bright Green
        Color.FromArgb(255, 255, 85),    // Bright Yellow
        Color.FromArgb(85, 85, 255),     // Bright Blue
        Color.FromArgb(255, 85, 255),    // Bright Magenta
        Color.FromArgb(85, 255, 255),    // Bright Cyan
        Color.FromArgb(255, 255, 255),   // Bright White
    ];

    public static void AppendRichText(RichTextBox box, string text)
    {
        Color fore = box.ForeColor;
        Color back = box.BackColor;
        bool bold = false;
        bool underline = false;
        bool afterCR = false;

        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];

            if (c == '\x1b')
            {
                i++;
                if (i < text.Length && text[i] == '[')
                {
                    i++;
                    int start = i;
                    while (i < text.Length && !IsCsiFinalByte(text[i]))
                        i++;
                    if (i < text.Length)
                    {
                        byte finalByte = (byte)text[i];
                        if (finalByte == (byte)'m')
                        {
                            string sgr = text.Substring(start, i - start);
                            ApplySgr(sgr, ref fore, ref back, ref bold, ref underline, box);
                        }
                        i++;
                    }
                    continue;
                }
                
                // OSC sequences: ESC ] ... (BEL = 0x07) or ESC ] ... ESC \
                if (i < text.Length && text[i] == ']')
                {
                    i++; // skip ']'
                    while (i < text.Length)
                    {
                        if (text[i] == '\x07') // BEL terminator
                        {
                            i++;
                            break;
                        }
                        if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '\\') // ST terminator
                        {
                            i += 2;
                            break;
                        }
                        i++;
                    }
                    continue;
                }
                
                continue;
            }

            if (c == '\r')
            {
                i++;
                int ahead = i;
                if (ahead < text.Length && text[ahead] == '\n')
                {
                    i = ahead + 1;
                    afterCR = false;
                    EmitText(box, "\n", fore, back, bold, underline);
                }
                else
                {
                    afterCR = true;
                }
                continue;
            }

            if (c == '\n')
            {
                i++;
                afterCR = false;
                EmitText(box, "\n", fore, back, bold, underline);
                continue;
            }

            int chunkStart = i;
            while (i < text.Length && text[i] != '\x1b' && text[i] != '\r' && text[i] != '\n')
                i++;

            string chunk = text.Substring(chunkStart, i - chunkStart);

            if (afterCR)
            {
                afterCR = false;
                ClearCurrentLine(box);
            }

            EmitText(box, chunk, fore, back, bold, underline);
        }
    }

    private static bool IsCsiFinalByte(char c)
    {
        return c >= 0x40 && c <= 0x7E;
    }

    private static void ApplySgr(string parameters, ref Color fore, ref Color back, ref bool bold, ref bool underline, RichTextBox box)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            Reset(ref fore, ref back, ref bold, ref underline, box);
            return;
        }

        string[] parts = parameters.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int code))
                continue;

            switch (code)
            {
                case 0:
                    Reset(ref fore, ref back, ref bold, ref underline, box);
                    break;
                case 1:
                    bold = true;
                    break;
                case 4:
                    underline = true;
                    break;
                case 7:
                    (fore, back) = (back, fore);
                    break;
                case 22:
                    bold = false;
                    break;
                case 24:
                    underline = false;
                    break;
                case 27:
                    (fore, back) = (back, fore);
                    break;
                case 30: case 31: case 32: case 33:
                case 34: case 35: case 36: case 37:
                    fore = StandardColors[code - 30];
                    break;
                case 38:
                    if (i + 1 < parts.Length)
                    {
                        if (parts[i + 1] == "5" && i + 2 < parts.Length)
                        {
                            i += 2;
                        }
                        else if (parts[i + 1] == "2" && i + 4 < parts.Length)
                        {
                            i += 4;
                        }
                    }
                    break;
                case 39:
                    fore = box.ForeColor;
                    break;
                case 40: case 41: case 42: case 43:
                case 44: case 45: case 46: case 47:
                    back = StandardColors[code - 40];
                    break;
                case 48:
                    if (i + 1 < parts.Length)
                    {
                        if (parts[i + 1] == "5" && i + 2 < parts.Length)
                            i += 2;
                        else if (parts[i + 1] == "2" && i + 4 < parts.Length)
                            i += 4;
                    }
                    break;
                case 49:
                    back = box.BackColor;
                    break;
                case 90: case 91: case 92: case 93:
                case 94: case 95: case 96: case 97:
                    fore = BrightColors[code - 90];
                    break;
                case 100: case 101: case 102: case 103:
                case 104: case 105: case 106: case 107:
                    back = BrightColors[code - 100];
                    break;
            }
        }
    }

    private static void Reset(ref Color fore, ref Color back, ref bool bold, ref bool underline, RichTextBox box)
    {
        fore = box.ForeColor;
        back = box.BackColor;
        bold = false;
        underline = false;
    }

    private static void ClearCurrentLine(RichTextBox box)
    {
        int textLen = box.TextLength;
        if (textLen == 0)
            return;

        int lastNewLine = box.Text.LastIndexOf('\n');
        int lineStart = lastNewLine < 0 ? 0 : lastNewLine + 1;
        int count = textLen - lineStart;
        if (count > 0)
        {
            box.Select(lineStart, count);
            box.SelectedText = "";
        }
    }

    private static void EmitText(RichTextBox box, string text, Color fore, Color back, bool bold, bool underline)
    {
        if (text.Length == 0)
            return;

        int start = box.TextLength;
        box.AppendText(text);
        box.Select(start, text.Length);
        box.SelectionColor = fore;
        box.SelectionBackColor = back;

        FontStyle style = FontStyle.Regular;
        if (bold)
            style |= FontStyle.Bold;
        if (underline)
            style |= FontStyle.Underline;

        if (style != FontStyle.Regular)
        {
            try
            {
                using var font = new Font(box.Font, style);
                box.SelectionFont = font;
            }
            catch
            {
            }
        }

        box.Select(box.TextLength, 0);
    }
}
