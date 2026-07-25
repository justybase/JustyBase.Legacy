using AppBase.Common;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FastColoredTextBoxNS;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>Side-by-side read-only diff viewer using DiffPlex + FastColoredTextBox.</summary>
public sealed class GitDiffControl : UserControl
{
    private readonly Panel _toolbar;
    private readonly Button _btnPrev;
    private readonly Button _btnNext;
    private readonly Label _lblHunkStatus;
    private readonly SplitContainer _split;
    private readonly FastColoredTextBox _left;
    private readonly FastColoredTextBox _right;
    private readonly Label _leftHeader;
    private readonly Label _rightHeader;
    private readonly Panel _leftPanel;
    private readonly Panel _rightPanel;
    private bool _syncingScroll;
    private readonly SolidBrush _deletedBrush = new(Color.FromArgb(255, 220, 220));
    private readonly SolidBrush _insertedBrush = new(Color.FromArgb(220, 255, 220));
    private readonly SolidBrush _modifiedBrush = new(Color.FromArgb(255, 243, 200));
    private readonly SolidBrush _imaginaryBrush = new(Color.FromArgb(245, 245, 245));
    private readonly List<int> _diffLineIndices = [];
    private int _currentDiffIndex = -1;

    public GitDiffControl()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);

        _btnPrev = new Button
        {
            Text = "↑ Previous",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 6, 0),
            Enabled = false
        };
        _btnNext = new Button
        {
            Text = "↓ Next",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 8, 0),
            Enabled = false
        };
        _lblHunkStatus = new Label
        {
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "No differences",
            Margin = new Padding(0, 4, 0, 0)
        };

        _toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Padding = new Padding(6, 4, 6, 4)
        };
        _toolbar.Controls.Add(_btnPrev);
        _toolbar.Controls.Add(_btnNext);
        _toolbar.Controls.Add(_lblHunkStatus);

        _btnPrev.Click += (_, _) => GoToDifference(-1);
        _btnNext.Click += (_, _) => GoToDifference(+1);

        _leftHeader = CreateHeader("Original");
        _rightHeader = CreateHeader("Modified");
        _left = CreateEditor();
        _right = CreateEditor();

        _leftPanel = new Panel { Dock = DockStyle.Fill };
        _leftPanel.Controls.Add(_left);
        _leftPanel.Controls.Add(_leftHeader);

        _rightPanel = new Panel { Dock = DockStyle.Fill };
        _rightPanel.Controls.Add(_right);
        _rightPanel.Controls.Add(_rightHeader);

        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = Math.Max(4, DpiScale.Scale(4, DeviceDpi))
        };
        _split.Panel1.Controls.Add(_leftPanel);
        _split.Panel2.Controls.Add(_rightPanel);

        Controls.Add(_split);
        Controls.Add(_toolbar);

        _left.Scroll += (_, _) => SyncScroll(_left, _right);
        _right.Scroll += (_, _) => SyncScroll(_right, _left);
        _left.VisibleRangeChanged += (_, _) => SyncScroll(_left, _right);
        _right.VisibleRangeChanged += (_, _) => SyncScroll(_right, _left);

        Load += (_, _) => ApplyDpiMetrics();
    }

    public void LoadDiff(string title, string oldText, string newText)
    {
        _leftHeader.Text = $"Original — {title}";
        _rightHeader.Text = $"Modified — {title}";

        SideBySideDiffModel model = SideBySideDiffBuilder.Diff(oldText ?? string.Empty, newText ?? string.Empty);

        _diffLineIndices.Clear();
        _currentDiffIndex = -1;
        for (int i = 0; i < model.OldText.Lines.Count; i++)
        {
            ChangeType leftType = model.OldText.Lines[i].Type;
            ChangeType rightType = i < model.NewText.Lines.Count
                ? model.NewText.Lines[i].Type
                : ChangeType.Imaginary;
            if (IsDifference(leftType) || IsDifference(rightType))
                _diffLineIndices.Add(i);
        }

        ApplySide(_left, model.OldText.Lines, isOld: true);
        ApplySide(_right, model.NewText.Lines, isOld: false);

        UpdateNavUi();
        if (_diffLineIndices.Count > 0)
            GoToDifferenceAbsolute(0);
        else
        {
            _left.Navigate(0);
            _right.Navigate(0);
        }

        ApplyDpiMetrics();
    }

    public void ApplyDpiMetrics()
    {
        int dpi = DeviceDpi;
        int headerH = Math.Max(DpiScale.Scale(24, dpi), (int)Math.Ceiling(Font.GetHeight()) + DpiScale.Scale(8, dpi));
        int pad = DpiScale.Scale(6, dpi);
        int gap = DpiScale.Scale(6, dpi);
        int buttonH = Math.Max(DpiScale.Scale(26, dpi), (int)Math.Ceiling(Font.GetHeight()) + DpiScale.Scale(8, dpi));

        _toolbar.Padding = new Padding(pad, DpiScale.Scale(4, dpi), pad, DpiScale.Scale(4, dpi));
        foreach (Button button in new[] { _btnPrev, _btnNext })
        {
            button.MinimumSize = new Size(0, buttonH);
            button.Padding = new Padding(DpiScale.Scale(8, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(8, dpi), DpiScale.Scale(2, dpi));
            button.Margin = new Padding(0, 0, gap, 0);
        }

        _leftHeader.Height = headerH;
        _rightHeader.Height = headerH;
        _split.SplitterWidth = Math.Max(4, DpiScale.Scale(4, dpi));

        if (_split.Width > _split.Panel1MinSize + _split.Panel2MinSize + _split.SplitterWidth)
        {
            int mid = Math.Max(_split.Panel1MinSize, (_split.Width - _split.SplitterWidth) / 2);
            int max = _split.Width - _split.Panel2MinSize - _split.SplitterWidth;
            _split.SplitterDistance = Math.Clamp(mid, _split.Panel1MinSize, Math.Max(_split.Panel1MinSize, max));
        }
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyDpiMetrics();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deletedBrush.Dispose();
            _insertedBrush.Dispose();
            _modifiedBrush.Dispose();
            _imaginaryBrush.Dispose();
        }
        base.Dispose(disposing);
    }

    private void GoToDifference(int delta)
    {
        if (_diffLineIndices.Count == 0)
            return;

        int next = _currentDiffIndex < 0
            ? (delta > 0 ? 0 : _diffLineIndices.Count - 1)
            : _currentDiffIndex + delta;

        if (next < 0)
            next = _diffLineIndices.Count - 1;
        else if (next >= _diffLineIndices.Count)
            next = 0;

        GoToDifferenceAbsolute(next);
    }

    private void GoToDifferenceAbsolute(int index)
    {
        if (index < 0 || index >= _diffLineIndices.Count)
            return;

        _currentDiffIndex = index;
        int line = _diffLineIndices[index];
        _left.Navigate(line);
        _right.Navigate(line);
        try
        {
            _left.Selection = new FastColoredTextBoxNS.Range(_left, 0, line, 0, line);
            _right.Selection = new FastColoredTextBoxNS.Range(_right, 0, line, 0, line);
        }
        catch
        {
        }

        UpdateNavUi();
    }

    private void UpdateNavUi()
    {
        bool hasDiffs = _diffLineIndices.Count > 0;
        _btnPrev.Enabled = hasDiffs;
        _btnNext.Enabled = hasDiffs;
        _lblHunkStatus.Text = hasDiffs
            ? $"Difference {_currentDiffIndex + 1} of {_diffLineIndices.Count}"
            : "No differences";
    }

    private static bool IsDifference(ChangeType type) =>
        type is ChangeType.Inserted or ChangeType.Deleted or ChangeType.Modified;

    private void ApplySide(FastColoredTextBox box, List<DiffPiece> pieces, bool isOld)
    {
        var lines = new List<string>(pieces.Count);
        var brushes = new List<Brush?>(pieces.Count);

        foreach (DiffPiece piece in pieces)
        {
            lines.Add(piece.Text ?? string.Empty);
            brushes.Add(MapBrush(piece.Type, isOld));
        }

        box.BeginUpdate();
        try
        {
            box.ClearStylesBuffer();
            box.Text = lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
            for (int i = 0; i < box.LinesCount && i < brushes.Count; i++)
            {
                Line line = box[i];
                line.BackgroundBrush = brushes[i];
            }
            box.Invalidate();
        }
        finally
        {
            box.EndUpdate();
        }
    }

    private Brush? MapBrush(ChangeType type, bool isOld) =>
        type switch
        {
            ChangeType.Deleted => isOld ? _deletedBrush : _imaginaryBrush,
            ChangeType.Inserted => isOld ? _imaginaryBrush : _insertedBrush,
            ChangeType.Modified => _modifiedBrush,
            ChangeType.Imaginary => _imaginaryBrush,
            _ => null
        };

    private void SyncScroll(FastColoredTextBox source, FastColoredTextBox target)
    {
        if (_syncingScroll || source.IsDisposed || target.IsDisposed)
            return;

        _syncingScroll = true;
        try
        {
            target.VerticalScroll.Value = Math.Min(
                source.VerticalScroll.Value,
                Math.Max(target.VerticalScroll.Minimum, target.VerticalScroll.Maximum - target.ClientSize.Height));
            target.HorizontalScroll.Value = Math.Min(
                source.HorizontalScroll.Value,
                Math.Max(target.HorizontalScroll.Minimum, target.HorizontalScroll.Maximum - target.ClientSize.Width));
            target.UpdateScrollbars();
            target.Invalidate();
        }
        catch
        {
            // Scroll bounds can race during reload.
        }
        finally
        {
            _syncingScroll = false;
        }
    }

    private static Label CreateHeader(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 24,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(8, 0, 0, 0),
        AutoEllipsis = true
    };

    private static FastColoredTextBox CreateEditor()
    {
        var box = new FastColoredTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.Black,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            ShowLineNumbers = true,
            HighlightingRangeType = HighlightingRangeType.VisibleRange,
            Language = Language.Custom,
            Font = new Font("Consolas", 9.75f)
        };
        return box;
    }
}
