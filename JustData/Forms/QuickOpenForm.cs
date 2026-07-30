using AppBase.Common;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.UI.QuickOpen;

namespace JustyBaseLegacy.UI.Forms;

internal sealed class QuickOpenForm : Form
{
    private const int DebounceMs = 350;
    private const int LogicalMaxWidth = 720;
    private const int LogicalMaxHeight = 460;
    private const int LogicalMinWidth = 480;
    private const int LogicalMinHeight = 280;

    private readonly IColorTheme _theme;
    private readonly QuickOpenSearchService _searchService;
    private readonly IReadOnlyList<QuickOpenCandidate> _candidates;
    private readonly TimeSpan _contentTimeout;

    private readonly TextBox _searchBox;
    private readonly ListBox _results;
    private readonly Panel _chrome;
    private readonly Panel _body;
    private readonly Label _hintLabel;

    private CancellationTokenSource? _contentCts;
    private System.Windows.Forms.Timer? _debounceTimer;
    private IReadOnlyList<QuickOpenListEntry> _entries = [];
    private IReadOnlyList<QuickOpenHit> _nameHits = [];
    private IReadOnlyList<QuickOpenHit> _contentHits = [];
    private int _selectedSelectableIndex = -1;
    private bool _activationReady;
    private bool _applyingDpi;

    private readonly Color _matchHighlight;
    private readonly Color _mutedFore;
    private readonly Color _selectedBack;
    private readonly Color _headerFore;
    private readonly Color _border;

    private Font? _searchFont;
    private Font? _resultsFont;
    private Font? _hintFont;
    private Font? _headerFont;
    private Font? _pathFont;
    private Font? _snippetFont;
    private int _appliedDpi = -1;

    public QuickOpenHit? SelectedHit { get; private set; }

    public QuickOpenForm(
        IColorTheme theme,
        QuickOpenSearchService searchService,
        IReadOnlyList<QuickOpenCandidate> candidates,
        TimeSpan contentTimeout)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        _contentTimeout = contentTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(10) : contentTimeout;

        bool dark = _theme.IsDark(_theme.MainBack);
        BackColor = dark ? Color.FromArgb(30, 30, 30) : _theme.MainBack;
        ForeColor = _theme.MainFore;
        _matchHighlight = dark ? Color.FromArgb(78, 201, 176) : Color.FromArgb(0, 120, 140);
        _mutedFore = dark ? Color.FromArgb(140, 140, 140) : Color.FromArgb(110, 110, 110);
        _selectedBack = dark ? Color.FromArgb(9, 71, 113) : Color.FromArgb(0, 120, 215);
        _headerFore = dark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(90, 90, 90);
        _border = DarkChromeHelper.SoftBorder(BackColor, dark);

        // Manual DpiScale via DeviceDpi — avoid AutoScaleMode.Dpi double-scaling
        // of sizes already computed with DpiScale.Scale.
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        Padding = new Padding(1);

        // Point-sized fonts are DPI-independent; create once and never dispose while controls use them.
        _searchFont = new Font("Segoe UI", 12f, FontStyle.Regular);
        _resultsFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        _hintFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        _headerFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        _pathFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        _snippetFont = new Font("Consolas", 8.5f, FontStyle.Regular);

        _chrome = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
        };

        _searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Font = _searchFont,
            TabIndex = 0,
        };

        _hintLabel = new Label
        {
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _mutedFore,
            Font = _hintFont,
            Text = "↑↓ navigate  ·  Enter open  ·  Esc close",
        };

        _results = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawVariable,
            IntegralHeight = false,
            Font = _resultsFont,
            BackColor = BackColor,
            ForeColor = ForeColor,
            TabStop = false,
        };
        Font = _resultsFont;

        _body = new Panel { Dock = DockStyle.Fill };
        _body.Controls.Add(_results);
        _body.Controls.Add(_hintLabel);

        _chrome.Controls.Add(_body);
        _chrome.Controls.Add(_searchBox);
        Controls.Add(_chrome);

        DarkChromeHelper.ApplyTextBox(_searchBox, _theme.TextBoxBackColor, _theme.TextBoxForeColor, _border);

        _searchBox.TextChanged += SearchBox_TextChanged;
        _searchBox.KeyDown += SearchBox_KeyDown;
        _results.MeasureItem += Results_MeasureItem;
        _results.DrawItem += Results_DrawItem;
        _results.MouseClick += Results_MouseClick;
        _results.MouseDoubleClick += Results_MouseDoubleClick;
        KeyDown += QuickOpenForm_KeyDown;
        Deactivate += (_, _) =>
        {
            if (!_activationReady || DialogResult != DialogResult.None)
                return;
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Paint += (_, e) =>
        {
            using var pen = new Pen(_border, Math.Max(1f, DpiScale.Factor(DeviceDpi)));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Layout only — do not recreate/dispose fonts while child handles are being created.
        BeginInvoke(new Action(() => ApplyDpiMetrics()));
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyDpiMetrics(force: true);
        _results.Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyDpiMetrics();
        ApplyFilter(_searchBox.Text);
        _searchBox.Focus();
        _searchBox.SelectAll();
        BeginInvoke(() => _activationReady = true);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        CancelContentSearch();
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        // Detach owned fonts from controls before disposing them.
        _searchBox.Font = SystemFonts.DefaultFont;
        _hintLabel.Font = SystemFonts.DefaultFont;
        _results.Font = SystemFonts.DefaultFont;
        Font = SystemFonts.DefaultFont;
        DisposeFonts();
        base.OnFormClosed(e);
    }

    public void PositionOver(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        int dpi = owner.DeviceDpi > 0 ? owner.DeviceDpi : DeviceDpi;
        Rectangle host = owner.RectangleToScreen(owner.ClientRectangle);
        var screen = Screen.FromControl(owner).WorkingArea;

        int maxW = DpiScale.Scale(LogicalMaxWidth, dpi);
        int maxH = DpiScale.Scale(LogicalMaxHeight, dpi);
        int minW = DpiScale.Scale(LogicalMinWidth, dpi);
        int minH = DpiScale.Scale(LogicalMinHeight, dpi);

        int width = Math.Min(maxW, Math.Max(minW, (int)(host.Width * 0.55)));
        int height = Math.Min(maxH, Math.Max(minH, (int)(host.Height * 0.55)));
        Width = width;
        Height = height;
        MinimumSize = new Size(minW, minH);

        int x = host.Left + (host.Width - width) / 2;
        int y = host.Top + (host.Height - height) / 2;

        // Keep fully visible on the monitor that hosts the editor window.
        x = Math.Min(Math.Max(x, screen.Left), screen.Right - width);
        y = Math.Min(Math.Max(y, screen.Top), screen.Bottom - height);
        Location = new Point(x, y);
    }

    private void ApplyDpiMetrics(bool force = false)
    {
        if (_applyingDpi)
            return;

        int dpi = DeviceDpi > 0 ? DeviceDpi : DpiScale.DefaultDpi;
        if (!force && dpi == _appliedDpi)
            return;

        _applyingDpi = true;
        try
        {
            int pad = DpiScale.Scale(10, dpi);
            int searchH = Math.Max(
                DpiScale.Scale(32, dpi),
                (int)Math.Ceiling(_searchFont!.GetHeight()) + DpiScale.Scale(12, dpi));
            int hintH = Math.Max(
                DpiScale.Scale(22, dpi),
                (int)Math.Ceiling(_hintFont!.GetHeight()) + DpiScale.Scale(6, dpi));
            int bodyTopPad = DpiScale.Scale(8, dpi);

            Padding = new Padding(Math.Max(1, DpiScale.Scale(1, dpi)));
            _chrome.Padding = new Padding(pad, pad, pad, DpiScale.Scale(8, dpi));
            _body.Padding = new Padding(0, bodyTopPad, 0, 0);

            _searchBox.Height = searchH;
            _hintLabel.Height = hintH;
            MinimumSize = DpiScale.Scale(new Size(LogicalMinWidth, LogicalMinHeight), dpi);

            if (_results.Items.Count > 0)
            {
                int selected = _results.SelectedIndex;
                _results.BeginUpdate();
                var items = _results.Items.Cast<object>().ToArray();
                _results.Items.Clear();
                foreach (var item in items)
                    _results.Items.Add(item);
                if (selected >= 0 && selected < _results.Items.Count)
                    _results.SelectedIndex = selected;
                _results.EndUpdate();
            }

            _appliedDpi = dpi;
        }
        finally
        {
            _applyingDpi = false;
        }
    }

    private void DisposeFonts()
    {
        _searchFont?.Dispose();
        _resultsFont?.Dispose();
        _hintFont?.Dispose();
        _headerFont?.Dispose();
        _pathFont?.Dispose();
        _snippetFont?.Dispose();
        _searchFont = null;
        _resultsFont = null;
        _hintFont = null;
        _headerFont = null;
        _pathFont = null;
        _snippetFont = null;
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        ApplyFilter(_searchBox.Text);
        ScheduleContentSearch(_searchBox.Text);
    }

    private void ApplyFilter(string query)
    {
        _nameHits = _searchService.SearchByName(_candidates, query);
        RebuildList();
    }

    private void ScheduleContentSearch(string query)
    {
        CancelContentSearch();
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        string trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            _contentHits = [];
            RebuildList();
            return;
        }

        _debounceTimer = new System.Windows.Forms.Timer { Interval = DebounceMs };
        _debounceTimer.Tick += async (_, _) =>
        {
            _debounceTimer.Stop();
            await RunContentSearchAsync(trimmed).ConfigureAwait(true);
        };
        _debounceTimer.Start();
    }

    private async Task RunContentSearchAsync(string query)
    {
        CancelContentSearch();
        var cts = new CancellationTokenSource();
        _contentCts = cts;
        try
        {
            var hits = await _searchService.SearchByContentAsync(
                _candidates,
                query,
                _contentTimeout,
                cts.Token).ConfigureAwait(true);

            if (cts.IsCancellationRequested)
                return;

            if (!string.Equals(_searchBox.Text.Trim(), query, StringComparison.Ordinal))
                return;

            _contentHits = hits;
            RebuildList();
        }
        catch (OperationCanceledException)
        {
            // expected on debounce / close
        }
    }

    private void CancelContentSearch()
    {
        try
        {
            _contentCts?.Cancel();
            _contentCts?.Dispose();
        }
        catch
        {
            // ignore dispose races
        }
        finally
        {
            _contentCts = null;
        }
    }

    private void RebuildList()
    {
        _entries = QuickOpenSearchService.BuildList(_nameHits, _contentHits);
        int previousSelectable = _selectedSelectableIndex;

        _results.BeginUpdate();
        _results.Items.Clear();
        foreach (var entry in _entries)
            _results.Items.Add(entry);
        _results.EndUpdate();

        int selectableCount = CountSelectable();
        if (selectableCount == 0)
        {
            _selectedSelectableIndex = -1;
            _results.SelectedIndex = -1;
            _hintLabel.Text = _candidates.Count == 0
                ? "No SQL files in Files / Git / open editors"
                : "No matches";
            return;
        }

        _selectedSelectableIndex = Math.Clamp(previousSelectable < 0 ? 0 : previousSelectable, 0, selectableCount - 1);
        SyncListSelectionFromSelectable();
        _hintLabel.Text = $"{selectableCount} results  ·  ↑↓ navigate  ·  Enter open  ·  Esc close";
    }

    private int CountSelectable() => _entries.Count(e => !e.IsHeader && e.Hit is not null);

    private int EntryIndexFromSelectable(int selectableIndex)
    {
        int seen = -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].IsHeader || _entries[i].Hit is null)
                continue;
            seen++;
            if (seen == selectableIndex)
                return i;
        }
        return -1;
    }

    private void SyncListSelectionFromSelectable()
    {
        int entryIndex = EntryIndexFromSelectable(_selectedSelectableIndex);
        if (entryIndex >= 0 && entryIndex < _results.Items.Count)
            _results.SelectedIndex = entryIndex;
    }

    private QuickOpenHit? GetSelectedHit()
    {
        int entryIndex = EntryIndexFromSelectable(_selectedSelectableIndex);
        if (entryIndex < 0 || entryIndex >= _entries.Count)
            return null;
        return _entries[entryIndex].Hit;
    }

    private void MoveSelection(int delta)
    {
        int count = CountSelectable();
        if (count == 0)
            return;

        if (_selectedSelectableIndex < 0)
            _selectedSelectableIndex = 0;
        else
            _selectedSelectableIndex = (_selectedSelectableIndex + delta + count) % count;

        SyncListSelectionFromSelectable();
        int entryIndex = EntryIndexFromSelectable(_selectedSelectableIndex);
        if (entryIndex >= 0)
            _results.TopIndex = Math.Max(0, entryIndex - 2);
    }

    private void AcceptSelection()
    {
        var hit = GetSelectedHit();
        if (hit is null)
            return;

        SelectedHit = hit;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void QuickOpenForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down)
        {
            MoveSelection(1);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode is Keys.Enter or Keys.Return)
        {
            AcceptSelection();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Results_MouseClick(object? sender, MouseEventArgs e)
    {
        int index = _results.IndexFromPoint(e.Location);
        if (index < 0 || index >= _entries.Count || _entries[index].IsHeader || _entries[index].Hit is null)
            return;

        int selectable = -1;
        for (int i = 0; i <= index; i++)
        {
            if (!_entries[i].IsHeader && _entries[i].Hit is not null)
                selectable++;
        }

        _selectedSelectableIndex = selectable;
        SyncListSelectionFromSelectable();
    }

    private void Results_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        Results_MouseClick(sender, e);
        AcceptSelection();
    }

    private void Results_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _entries.Count)
            return;

        int dpi = DeviceDpi;
        var entry = _entries[e.Index];
        if (entry.IsHeader)
        {
            e.ItemHeight = Math.Max(
                DpiScale.Scale(22, dpi),
                (int)Math.Ceiling((_headerFont ?? Font).GetHeight()) + DpiScale.Scale(6, dpi));
            return;
        }

        bool content = entry.Hit?.Kind == QuickOpenHitKind.Content;
        e.ItemHeight = content
            ? Math.Max(
                DpiScale.Scale(44, dpi),
                (int)Math.Ceiling((_resultsFont ?? Font).GetHeight())
                    + (int)Math.Ceiling((_snippetFont ?? Font).GetHeight())
                    + DpiScale.Scale(12, dpi))
            : Math.Max(
                DpiScale.Scale(28, dpi),
                (int)Math.Ceiling((_resultsFont ?? Font).GetHeight()) + DpiScale.Scale(10, dpi));
    }

    private void Results_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _entries.Count)
            return;

        int dpi = DeviceDpi;
        int padX = DpiScale.Scale(10, dpi);
        int padY = DpiScale.Scale(4, dpi);
        int lineH = Math.Max(
            DpiScale.Scale(20, dpi),
            (int)Math.Ceiling((_resultsFont ?? Font).GetHeight()) + DpiScale.Scale(4, dpi));
        int headerPadX = DpiScale.Scale(8, dpi);

        var entry = _entries[e.Index];
        using (var back = new SolidBrush(BackColor))
            e.Graphics.FillRectangle(back, e.Bounds);

        if (entry.IsHeader)
        {
            TextRenderer.DrawText(
                e.Graphics,
                entry.HeaderText ?? string.Empty,
                _headerFont ?? Font,
                new Rectangle(e.Bounds.X + headerPadX, e.Bounds.Y, e.Bounds.Width - headerPadX * 2, e.Bounds.Height),
                _headerFore,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            return;
        }

        var hit = entry.Hit!;
        bool selected = e.Index == _results.SelectedIndex;
        Color rowBack = selected ? _selectedBack : BackColor;
        using (var brush = new SolidBrush(rowBack))
            e.Graphics.FillRectangle(brush, e.Bounds);

        Color nameColor = selected ? Color.White : ForeColor;
        Color pathColor = selected ? Color.FromArgb(200, 220, 230) : _mutedFore;
        Color matchColor = selected ? Color.FromArgb(120, 230, 210) : _matchHighlight;

        int left = e.Bounds.X + padX;
        int top = e.Bounds.Y + padY;
        int nameWidth = Math.Max(DpiScale.Scale(80, dpi), e.Bounds.Width / 2);
        int pathGap = DpiScale.Scale(4, dpi);
        int rightPad = DpiScale.Scale(24, dpi);

        DrawHighlightedText(
            e.Graphics,
            hit.DisplayName,
            hit.Query,
            _resultsFont ?? Font,
            new Rectangle(left, top, nameWidth, lineH),
            nameColor,
            matchColor,
            dpi);

        string pathText = hit.Kind == QuickOpenHitKind.Content && hit.LineNumber is int line
            ? $"{hit.DisplayPath}  :{line}"
            : hit.DisplayPath;

        TextRenderer.DrawText(
            e.Graphics,
            pathText,
            _pathFont ?? Font,
            new Rectangle(left + nameWidth + pathGap, top, e.Bounds.Width - nameWidth - rightPad, lineH),
            pathColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if (hit.Kind == QuickOpenHitKind.Content && !string.IsNullOrWhiteSpace(hit.Snippet))
        {
            int snippetH = Math.Max(
                DpiScale.Scale(18, dpi),
                (int)Math.Ceiling((_snippetFont ?? Font).GetHeight()) + DpiScale.Scale(2, dpi));
            TextRenderer.DrawText(
                e.Graphics,
                hit.Snippet,
                _snippetFont ?? Font,
                new Rectangle(left, top + lineH - DpiScale.Scale(2, dpi), e.Bounds.Width - DpiScale.Scale(20, dpi), snippetH),
                pathColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    private static void DrawHighlightedText(
        Graphics g,
        string text,
        string? query,
        Font font,
        Rectangle bounds,
        Color normal,
        Color highlight,
        int dpi)
    {
        if (string.IsNullOrEmpty(query))
        {
            TextRenderer.DrawText(
                g,
                text,
                font,
                bounds,
                normal,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            return;
        }

        int index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            TextRenderer.DrawText(
                g,
                text,
                font,
                bounds,
                normal,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            return;
        }

        string before = text[..index];
        string match = text.Substring(index, query.Length);
        string after = text[(index + query.Length)..];

        int x = bounds.X;
        int y = bounds.Y;
        int h = bounds.Height;
        // TextRenderer.MeasureText with NoPadding still over-reports; compensate by DPI.
        int paddingFudge = DpiScale.Scale(8, dpi);

        void DrawPart(string part, Color color)
        {
            if (part.Length == 0)
                return;
            var size = TextRenderer.MeasureText(part, font, new Size(int.MaxValue, h), TextFormatFlags.NoPadding);
            int drawWidth = Math.Max(1, size.Width - paddingFudge);
            TextRenderer.DrawText(
                g,
                part,
                font,
                new Rectangle(x, y, size.Width, h),
                color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            x += drawWidth;
        }

        DrawPart(before, normal);
        DrawPart(match, highlight);
        DrawPart(after, normal);
    }
}
