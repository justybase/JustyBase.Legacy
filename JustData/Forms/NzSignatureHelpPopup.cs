using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Authoring;

namespace JustyBaseLegacy.UI.Forms;

public sealed class NzSignatureHelpPopup : Form
{
    private readonly RichTextBox _signatureBox;
    private readonly Label _documentationLabel;
    private readonly Label _overloadCountLabel;
    private SqlSignatureHelpInfo _currentHelp;
    private int _currentSignatureIndex;

    private const int PopupPadding = 8;
    private const int MaxWidth = 600;
    private static readonly Font _measureFont = new("Consolas", 10f, FontStyle.Regular);

    public NzSignatureHelpPopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.FromArgb(241, 241, 241);

        _overloadCountLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(140, 140, 140),
            Font = new Font("Consolas", 9f, FontStyle.Regular),
            Location = new Point(PopupPadding, PopupPadding)
        };

        _signatureBox = new RichTextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 10f, FontStyle.Regular),
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.None,
            Location = new Point(PopupPadding, PopupPadding + 22),
            Width = 100,
            Height = 22
        };

        _documentationLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(170, 170, 170),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            MaximumSize = new Size(MaxWidth - PopupPadding * 2, 0),
            Location = new Point(PopupPadding, PopupPadding + 48)
        };

        Controls.Add(_overloadCountLabel);
        Controls.Add(_signatureBox);
        Controls.Add(_documentationLabel);

        KeyPreview = true;
        KeyDown += OnKeyDown;
        Deactivate += (_, _) => Hide();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_currentHelp?.Signatures.Length > 1)
        {
            if (e.KeyCode == Keys.Up)
            {
                SelectPreviousOverload();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                SelectNextOverload();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }

        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    public void Show(FastColoredTextBox editor, SqlSignatureHelpInfo help)
    {
        _currentHelp = help;
        _currentSignatureIndex = Math.Clamp(help.ActiveSignature, 0, help.Signatures.Length - 1);

        var caretPos = editor.Selection.Start;
        var caretScreen = editor.PointToScreen(editor.PlaceToPoint(caretPos));

        Render();

        var screen = Screen.FromControl(editor).WorkingArea;
        int x = Math.Max(screen.Left, Math.Min(caretScreen.X, screen.Right - Width));
        int y = caretScreen.Y + 20;
        if (y + Height > screen.Bottom)
            y = caretScreen.Y - Height - 4;

        Location = new Point(x, y);
        Show(editor);
        BringToFront();
    }

    public void Show(Control owner)
    {
        if (!Visible)
            base.Show(owner);
    }

    public void Update(SqlSignatureHelpInfo help)
    {
        _currentHelp = help;
        _currentSignatureIndex = Math.Clamp(help.ActiveSignature, 0, help.Signatures.Length - 1);
        Render();
    }

    private void Render()
    {
        if (_currentHelp is null || _currentSignatureIndex >= _currentHelp.Signatures.Length)
        {
            Hide();
            return;
        }

        var sig = _currentHelp.Signatures[_currentSignatureIndex];

        int overloadCount = _currentHelp.Signatures.Length;
        if (overloadCount > 1)
        {
            _overloadCountLabel.Text = $"{_currentSignatureIndex + 1}/{overloadCount}";
            _overloadCountLabel.Visible = true;
        }
        else
        {
            _overloadCountLabel.Visible = false;
        }

        RenderSignature(sig);

        if (!string.IsNullOrWhiteSpace(sig.Documentation))
        {
            _documentationLabel.Text = sig.Documentation;
            _documentationLabel.Visible = true;
        }
        else
        {
            _documentationLabel.Visible = false;
        }

        Width = CalculateWidth(sig);
        Height = CalculateHeight();

        using var g = CreateGraphics();
        int sigTextWidth = (int)g.MeasureString(_signatureBox.Text, _signatureBox.Font).Width + PopupPadding * 2;
        int desiredWidth = Math.Min(sigTextWidth, MaxWidth) + PopupPadding * 2;
        Width = Math.Max(Width, desiredWidth);
        _signatureBox.Width = Width - PopupPadding * 2;
        _documentationLabel.MaximumSize = new Size(Width - PopupPadding * 2, 0);
    }

    private void RenderSignature(SqlSignatureInfo sig)
    {
        _signatureBox.Clear();
        _signatureBox.Text = sig.Label;

        int activeParam = _currentHelp.ActiveParameter;
        if (activeParam >= 0 && activeParam < sig.Parameters.Length)
        {
            string paramLabel = sig.Parameters[activeParam].Label;
            int idx = sig.Label.IndexOf(paramLabel, StringComparison.Ordinal);
            if (idx >= 0)
            {
                _signatureBox.Select(idx, paramLabel.Length);
                _signatureBox.SelectionFont = new Font(_signatureBox.Font, FontStyle.Bold);
                _signatureBox.SelectionColor = Color.FromArgb(100, 200, 255);
                _signatureBox.Select(0, 0);
            }
        }
    }

    private static int CalculateWidth(SqlSignatureInfo sig)
    {
        int labelWidth = TextRenderer.MeasureText(sig.Label, _measureFont).Width;
        return Math.Min(labelWidth + PopupPadding * 2 + 16, MaxWidth + PopupPadding * 2);
    }

    private int CalculateHeight()
    {
        int h = PopupPadding;
        if (_overloadCountLabel.Visible)
            h += 22;
        h += 22;

        if (_documentationLabel.Visible)
        {
            int docHeight = TextRenderer.MeasureText(
                _documentationLabel.Text,
                _documentationLabel.Font,
                new Size(Width - PopupPadding * 2, int.MaxValue),
                TextFormatFlags.WordBreak).Height;
            h += docHeight + 4;
        }

        return h + PopupPadding;
    }

    public void SelectNextOverload()
    {
        if (_currentHelp is null || _currentHelp.Signatures.Length <= 1)
            return;
        _currentSignatureIndex = (_currentSignatureIndex + 1) % _currentHelp.Signatures.Length;
        Render();
    }

    public void SelectPreviousOverload()
    {
        if (_currentHelp is null || _currentHelp.Signatures.Length <= 1)
            return;
        _currentSignatureIndex = (_currentSignatureIndex - 1 + _currentHelp.Signatures.Length) % _currentHelp.Signatures.Length;
        Render();
    }

    public int CurrentParameterCount =>
        _currentHelp?.Signatures.Length > 0
            ? _currentHelp.Signatures[_currentSignatureIndex].Parameters.Length
            : 0;
}
