using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>
/// Compact, read-only text log for SQL execution events (Output-panel style).
/// </summary>
public sealed class SqlExecutionLogControl : UserControl, ISqlExecutionLog
{
    private readonly FastColoredTextBox _editor;
    private TextStyle? _errorStyle;
    private TextStyle? _emphasisStyle;
    private TextStyle? _codeStyle;
    private Color _errorBackColor = MyColors.LogErrorStdColor;

    public SqlExecutionLogControl()
    {
        _editor = new FastColoredTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            WordWrap = false,
            ShowLineNumbers = true,
            ShowFoldingLines = false,
            LeftBracket = '\0',
            RightBracket = '\0',
            AutoIndent = false,
            AutoIndentChars = false,
            TabLength = 4,
            BorderStyle = BorderStyle.None,
            Font = CreateMonospaceFont(9f),
            Cursor = Cursors.IBeam,
            ImeMode = ImeMode.Disable,
        };

        _editor.DelayedTextChangedInterval = 100;
        _editor.DelayedEventsInterval = 100;

        BuildContextMenu();
        Controls.Add(_editor);

        ApplyDefaultStyles();
    }

    public Control View => this;

    public FastColoredTextBox Editor => _editor;

    public void ApplyTheme(IColorTheme colorTheme)
    {
        ArgumentNullException.ThrowIfNull(colorTheme);

        var colors = colorTheme.CurrentFctbColors;
        _editor.BackColor = colors.FctbBackColor;
        _editor.ForeColor = colors.FctbForeColor;
        _editor.IndentBackColor = colors.FctbIndentBackColor;
        _editor.LineNumberColor = colors.FctbLineNumberColor;
        _editor.CaretColor = colors.FctbForeColor;
        _editor.SelectionColor = colors.FctbSelectionColor;
        _editor.DisabledColor = colors.FctbDisabledColor;
        BackColor = colors.FctbBackColor;
        ForeColor = colors.FctbForeColor;

        ApplyDefaultStyles();
        Invalidate(true);
    }

    public void SetErrorBackColor(Color color)
    {
        _errorBackColor = color.IsEmpty ? MyColors.LogErrorStdColor : color;
        ApplyDefaultStyles();
    }

    public void AppendEntry(params object?[] fields) => AppendCore(fields, LogEntryKind.Normal);

    public void AppendErrorEntry(params object?[] fields) => AppendCore(fields, LogEntryKind.Error);

    public void AppendEmphasisEntry(params object?[] fields) => AppendCore(fields, LogEntryKind.Emphasis);

    public void Clear()
    {
        if (InvokeRequired)
        {
            Invoke(Clear);
            return;
        }

        _editor.Clear();
    }

    private void AppendCore(object?[]? fields, LogEntryKind kind)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendCore(fields, kind));
            return;
        }

        (string line, string? codeLine) = FormatEntry(fields);
        Style? style = kind switch
        {
            LogEntryKind.Error => _errorStyle,
            LogEntryKind.Emphasis => _emphasisStyle,
            _ => null,
        };

        bool wasEmpty = string.IsNullOrEmpty(_editor.Text);
        string prefix = wasEmpty ? string.Empty : Environment.NewLine;

        _editor.AppendText(prefix + line, style);
        if (!string.IsNullOrEmpty(codeLine))
        {
            _editor.AppendText(Environment.NewLine + codeLine, _codeStyle);
        }

        _editor.GoEnd();
    }

    private static (string Line, string? CodeLine) FormatEntry(object?[]? fields)
    {
        if (fields is null || fields.Length == 0)
        {
            return (string.Empty, null);
        }

        string timestamp = FormatTimestamp(fields.ElementAtOrDefault(0));
        string elapsed = FormatElapsed(fields.ElementAtOrDefault(1));

        // Historical shapes:
        // 6: ts, elapsed, connection, db, info, code
        // 5: ts, elapsed, connection, db, info
        // 4: ts, elapsed, message, null/DBNull  OR  ts, elapsed, label, data
        // 3: ts, elapsed, message
        string? connection = null;
        string? db = null;
        string? info = null;
        string? code = null;

        if (fields.Length >= 6)
        {
            connection = FormatField(fields[2]);
            db = FormatField(fields[3]);
            info = FormatField(fields[4]);
            code = FormatField(fields[5]);
        }
        else if (fields.Length == 5)
        {
            connection = FormatField(fields[2]);
            db = FormatField(fields[3]);
            info = FormatField(fields[4]);
        }
        else if (fields.Length == 4)
        {
            string third = FormatField(fields[2]) ?? string.Empty;
            string fourth = FormatField(fields[3]) ?? string.Empty;
            if (string.IsNullOrEmpty(fourth))
            {
                info = third;
            }
            else
            {
                connection = third;
                info = fourth;
            }
        }
        else if (fields.Length == 3)
        {
            info = FormatField(fields[2]);
        }
        else if (fields.Length == 2)
        {
            info = FormatField(fields[1]);
            elapsed = string.Empty;
        }
        else
        {
            info = FormatField(fields[0]);
            timestamp = string.Empty;
            elapsed = string.Empty;
        }

        var sb = new StringBuilder(128);
        if (!string.IsNullOrEmpty(timestamp))
        {
            sb.Append(timestamp);
        }

        if (!string.IsNullOrEmpty(elapsed))
        {
            if (sb.Length > 0)
            {
                sb.Append("  ");
            }

            sb.Append(elapsed);
        }

        AppendSegment(sb, connection);
        AppendSegment(sb, db);
        AppendSegment(sb, info);

        string? codeLine = string.IsNullOrWhiteSpace(code) ? null : "    " + code.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        return (sb.ToString(), codeLine);
    }

    private static void AppendSegment(StringBuilder sb, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append("  ");
        }

        sb.Append(value);
    }

    private static string FormatTimestamp(object? value)
    {
        if (value is DateTime dt)
        {
            return dt.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        }

        string? text = FormatField(value);
        return text ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private static string FormatElapsed(object? value)
    {
        if (value is null || value is DBNull)
        {
            return string.Empty;
        }

        if (value is double d)
        {
            return $"+{d.ToString("0.0", CultureInfo.CurrentCulture)}s";
        }

        if (value is float f)
        {
            return $"+{f.ToString("0.0", CultureInfo.CurrentCulture)}s";
        }

        if (value is int i)
        {
            return $"+{i.ToString("0.0", CultureInfo.CurrentCulture)}s";
        }

        if (value is long l)
        {
            return $"+{l.ToString("0.0", CultureInfo.CurrentCulture)}s";
        }

        string text = value.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            return text.StartsWith('+') ? text : "+" + text;
        }

        return $"+{text}s";
    }

    private static string? FormatField(object? value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        string text = value.ToString() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private void ApplyDefaultStyles()
    {
        _errorStyle?.Dispose();
        _emphasisStyle?.Dispose();
        _codeStyle?.Dispose();

        Color errorFore = ContrastForeground(_errorBackColor);
        _errorStyle = new TextStyle(new SolidBrush(errorFore), new SolidBrush(_errorBackColor), FontStyle.Regular);
        _emphasisStyle = new TextStyle(new SolidBrush(_editor.ForeColor), null, FontStyle.Bold);

        Color muted = Color.FromArgb(
            Mix(_editor.ForeColor.R, _editor.BackColor.R, 0.45),
            Mix(_editor.ForeColor.G, _editor.BackColor.G, 0.45),
            Mix(_editor.ForeColor.B, _editor.BackColor.B, 0.45));
        _codeStyle = new TextStyle(new SolidBrush(muted), null, FontStyle.Regular);
    }

    private static int Mix(int a, int b, double amountTowardB) =>
        (int)Math.Round(a + (b - a) * amountTowardB);

    private static Color ContrastForeground(Color background)
    {
        double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
        return luminance > 0.55 ? Color.FromArgb(40, 0, 0) : Color.White;
    }

    private static Font CreateMonospaceFont(float size)
    {
        foreach (string family in new[] { "Cascadia Mono", "Consolas", "Courier New" })
        {
            try
            {
                var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
                if (string.Equals(font.FontFamily.Name, family, StringComparison.OrdinalIgnoreCase)
                    || font.Name.Contains(family, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }

                font.Dispose();
            }
            catch (ArgumentException)
            {
                // Try next family.
            }
        }

        return new Font(FontFamily.GenericMonospace, size, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy", null, (_, _) =>
        {
            if (!_editor.Selection.IsEmpty)
            {
                _editor.Copy();
            }
            else if (!string.IsNullOrEmpty(_editor.Text))
            {
                Clipboard.SetText(_editor.Text);
            }
        });
        menu.Items.Add("Select All", null, (_, _) => _editor.SelectAll());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Clear", null, (_, _) => Clear());
        _editor.ContextMenuStrip = menu;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _errorStyle?.Dispose();
            _emphasisStyle?.Dispose();
            _codeStyle?.Dispose();
            _editor.Dispose();
        }

        base.Dispose(disposing);
    }

    private enum LogEntryKind
    {
        Normal,
        Error,
        Emphasis,
    }
}
