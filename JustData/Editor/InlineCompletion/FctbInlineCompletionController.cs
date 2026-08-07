using FastColoredTextBoxNS;
using TextChangedEventArgs = FastColoredTextBoxNS.TextChangedEventArgs;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>
/// Debounced inline AI completion (ghost text + Tab accept) for FastColoredTextBox.
/// </summary>
public sealed class FctbInlineCompletionController : IDisposable
{
    public const int DefaultDebounceMs = 600;
    public const int MinDebounceMs = 250;
    public const int MaxDebounceMs = 3000;

    public static IReadOnlyList<int> AllowedDebounceMs { get; } = [250, 400, 600, 1000, 2000, 3000];

    private readonly FastColoredTextBox _editor;
    private readonly AutocompleteMenu? _completionMenu;
    private readonly Func<InlineCompletionContext, CancellationToken, Task<string?>> _completeAsync;
    private readonly Func<int>? _getDebounceMs;
    private readonly Func<bool>? _isEnabledProvider;
    private readonly int _debounceMs;
    private CancellationTokenSource? _debounceCts;
    private string? _ghostText;
    private int _ghostOffset = -1;
    private bool _attached;
    private bool _disposed;
    private CompletionSelectionSnapshot? _completionSelection;
    private bool _completionAcceptancePending;
    private bool _completionContinuationActive;
    private bool _completionTabInFlight;
    private string? _completionContinuationCandidate;
    private string? _pendingCompletionContinuation;
    private int _ghostCompletionPrefixLength;

    public FctbInlineCompletionController(
        FastColoredTextBox editor,
        Func<InlineCompletionContext, CancellationToken, Task<string?>> completeAsync,
        int debounceMs = DefaultDebounceMs,
        Func<int>? getDebounceMs = null,
        Func<bool>? isEnabledProvider = null,
        AutocompleteMenu? completionMenu = null)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _completeAsync = completeAsync ?? throw new ArgumentNullException(nameof(completeAsync));
        _getDebounceMs = getDebounceMs;
        _isEnabledProvider = isEnabledProvider;
        _completionMenu = completionMenu;
        _debounceMs = SnapDebounceMs(debounceMs);
    }

    /// <summary>Fallback when no provider is supplied (used by tests).</summary>
    public bool IsEnabled { get; set; } = true;

    private bool IsEffectivelyEnabled => (_isEnabledProvider?.Invoke() ?? IsEnabled) && !_disposed;

    public bool HasGhostText => !string.IsNullOrEmpty(_ghostText) && _ghostOffset >= 0;

    private int ResolveDebounceMs() => SnapDebounceMs(_getDebounceMs?.Invoke() ?? _debounceMs);

    public static int SnapDebounceMs(int debounceMs)
    {
        if (debounceMs <= 0)
            return DefaultDebounceMs;

        var clamped = Math.Clamp(debounceMs, MinDebounceMs, MaxDebounceMs);
        var best = AllowedDebounceMs[0];
        var bestDist = Math.Abs(best - clamped);
        foreach (var option in AllowedDebounceMs)
        {
            var dist = Math.Abs(option - clamped);
            if (dist < bestDist)
            {
                best = option;
                bestDist = dist;
            }
        }

        return best;
    }

    public static int DebounceMsFromSeconds(int seconds) =>
        SnapDebounceMs(Math.Clamp(seconds, 1, 15) * 1000);

    public void Attach()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_attached)
            return;

        _editor.TextChanged += OnTextChanged;
        _editor.SelectionChanged += OnSelectionChanged;
        _editor.PreviewKeyDown += OnPreviewKeyDown;
        _editor.KeyDown += OnKeyDown;
        _editor.PaintLine += OnPaintLine;
        if (_completionMenu is not null)
        {
            _completionMenu.Items.FocussedItemIndexChanged += OnCompletionSelectionChanged;
            _completionMenu.VisibleChanged += OnCompletionMenuVisibleChanged;
            _completionMenu.Selecting += OnCompletionSelecting;
            _completionMenu.Selected += OnCompletionSelected;
        }
        _attached = true;
    }

    public void Detach()
    {
        if (!_attached)
            return;

        CancelPending();
        ClearGhostText();
        _editor.TextChanged -= OnTextChanged;
        _editor.SelectionChanged -= OnSelectionChanged;
        _editor.PreviewKeyDown -= OnPreviewKeyDown;
        _editor.KeyDown -= OnKeyDown;
        _editor.PaintLine -= OnPaintLine;
        if (_completionMenu is not null)
        {
            _completionMenu.Items.FocussedItemIndexChanged -= OnCompletionSelectionChanged;
            _completionMenu.VisibleChanged -= OnCompletionMenuVisibleChanged;
            _completionMenu.Selecting -= OnCompletionSelecting;
            _completionMenu.Selected -= OnCompletionSelected;
        }
        _attached = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Detach();
        _debounceCts?.Dispose();
        _debounceCts = null;
        _disposed = true;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_completionAcceptancePending && _completionSelection is not null)
        {
            if (!IsCompletionInserted(_completionSelection))
            {
                // AutocompleteMenu replaces a selected fragment as two
                // commands (clear, then insert). Ignore the intermediate
                // clear notification and keep waiting for the inserted item.
                ClearGhostText();
                return;
            }

            var ghost = HasGhostText ? _ghostText ?? string.Empty : string.Empty;
            var continuation = _completionContinuationCandidate
                ?? (_ghostCompletionPrefixLength >= ghost.Length
                    ? string.Empty
                    : ghost[_ghostCompletionPrefixLength..]);

            _completionAcceptancePending = false;
            _completionContinuationCandidate = null;
            _completionSelection = null;
            _completionContinuationActive = !string.IsNullOrEmpty(continuation);
            CancelPending();
            ClearGhostText();
            if (!string.IsNullOrEmpty(continuation))
                QueueCompletionContinuation(continuation);

            return;
        }

        _completionContinuationCandidate = null;
        _pendingCompletionContinuation = null;
        _completionContinuationActive = false;
        if (HasGhostText)
            ClearGhostText();
        Schedule();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_completionAcceptancePending)
            return;

        if (_pendingCompletionContinuation is not null)
        {
            ApplyPendingCompletionContinuation();
            return;
        }

        // Navigation (mouse click, arrow keys, Home/End) must never start a completion.
        // Only clear a stale ghost text when the caret left its anchor; typing is handled
        // by OnTextChanged, and selection changes by OnCompletionSelectionChanged.
        if (HasGhostText && _ghostOffset != _editor.SelectionStart)
            ClearGhostText();
    }

    private void OnPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Tab
            && e.Modifiers == Keys.None
            && _completionMenu?.Visible == true)
        {
            _completionContinuationCandidate = CaptureCompletionContinuation();
            _completionAcceptancePending = _completionSelection is not null
                && _completionContinuationCandidate is not null;
            _completionTabInFlight = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_completionTabInFlight && e.KeyCode == Keys.Tab && e.Modifiers == Keys.None)
        {
            _completionTabInFlight = false;
            return;
        }

        if (_completionAcceptancePending && e.KeyCode == Keys.Tab && e.Modifiers == Keys.None)
            return;

        if (e.KeyCode == Keys.Tab
            && e.Modifiers == Keys.None
            && _completionMenu?.Visible != true
            && HasGhostText
            && !string.IsNullOrEmpty(_ghostText))
        {
            AcceptGhostText();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Escape && HasGhostText)
        {
            ClearGhostText();
            e.Handled = true;
            return;
        }

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            if (_completionMenu?.Visible != true && HasGhostText)
                ClearGhostText();
        }
    }

    private void OnCompletionSelectionChanged(object? sender, EventArgs e)
    {
        if (_completionMenu?.Visible != true)
            return;

        _completionAcceptancePending = false;
        _completionContinuationActive = false;
        _completionContinuationCandidate = null;
        _pendingCompletionContinuation = null;
        _completionSelection = CreateCompletionSelection();
        CancelPending();
        ClearGhostText();

        if (_completionSelection is not null)
        {
            var visibleSeed = GetVisibleCompletionText(_completionSelection);
            _ghostCompletionPrefixLength = visibleSeed.Length;
            if (!string.IsNullOrEmpty(visibleSeed))
                SetGhostText(_editor.SelectionStart, visibleSeed);
        }

        Schedule();
    }

    private void OnCompletionMenuVisibleChanged(object? sender, EventArgs e)
    {
        if (_completionMenu?.Visible == true)
        {
            OnCompletionSelectionChanged(sender, e);
            return;
        }

        if (_completionAcceptancePending)
            return;

        if (_completionContinuationActive)
        {
            _completionContinuationActive = false;
            return;
        }

        _completionContinuationCandidate = null;
        _pendingCompletionContinuation = null;
        _completionSelection = null;
        CancelPending();
        ClearGhostText();
    }

    private void OnCompletionSelecting(object? sender, SelectingEventArgs e)
    {
        if (_completionMenu?.Visible != true)
            return;

        _completionSelection ??= CreateCompletionSelection(e.Item);
        if (_completionSelection is not null)
        {
            // AutocompleteMenu raises Selecting immediately before replacing
            // the fragment. Capture the current continuation before the
            // editor starts raising TextChanged/SelectionChanged events.
            _completionContinuationCandidate = CaptureCompletionContinuation();
            _completionAcceptancePending = _completionContinuationCandidate is not null;
        }
    }

    private void OnCompletionSelected(object? sender, SelectedEventArgs e)
    {
        if (!_completionAcceptancePending)
            return;

        // TextChanged performs the hand-off after the selected item is
        // inserted. Keep the candidate here because event ordering differs
        // between FastColoredTextBox versions.
    }

    private CompletionSelectionSnapshot? CreateCompletionSelection(AutocompleteItem? item = null)
    {
        try
        {
            item ??= _completionMenu?.Items.FocussedItem;
            if (item is null || _completionMenu is null)
                return null;

            var end = Math.Clamp(_editor.SelectionStart, 0, _editor.TextLength);
            var fragmentText = _completionMenu.Fragment?.Text ?? string.Empty;
            var start = Math.Clamp(end - fragmentText.Length, 0, end);
            return new CompletionSelectionSnapshot(item.GetTextForReplace(), start, end);
        }
        catch (ArgumentOutOfRangeException)
        {
            // The editor can update its lines between the autocomplete menu
            // event and reading Fragment.Text. The completion is optional;
            // discard this stale selection instead of terminating the UI.
            return null;
        }
    }

    private string GetVisibleCompletionText(CompletionSelectionSnapshot selection)
    {
        var start = Math.Clamp(selection.ReplacementStartOffset, 0, _editor.TextLength);
        var end = Math.Clamp(selection.ReplacementEndOffset, start, _editor.TextLength);
        var typed = _editor.Text.Substring(start, Math.Min(end, _editor.SelectionStart) - start);
        return selection.InsertText.StartsWith(typed, StringComparison.OrdinalIgnoreCase)
            ? selection.InsertText[typed.Length..]
            : selection.InsertText;
    }

    private void OnPaintLine(object? sender, PaintLineEventArgs e)
    {
        if (!HasGhostText || string.IsNullOrEmpty(_ghostText))
            return;

        try
        {
            Place place = _editor.PositionToPlace(_ghostOffset);
            if (e.LineIndex != place.iLine)
                return;

            Point pt = _editor.PlaceToPoint(place);
            using var brush = new SolidBrush(Color.FromArgb(150, 110, 110, 110));
            e.Graphics.DrawString(_ghostText, _editor.Font, brush, pt);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            // Ignore paint races while the document mutates.
        }
    }

    private void AcceptGhostText()
    {
        if (!HasGhostText || string.IsNullOrEmpty(_ghostText))
            return;

        string text = _ghostText;
        int offset = _ghostOffset;
        ClearGhostText();
        CancelPending();
        _editor.SelectionStart = offset;
        _editor.SelectionLength = 0;
        _editor.InsertText(text);
    }

    private void Schedule()
    {
        if (!IsEffectivelyEnabled)
            return;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = RunDebouncedAsync(token);
    }

    private async Task RunDebouncedAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(ResolveDebounceMs(), token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;

            if (!IsEffectivelyEnabled)
                return;

            string documentText = string.Empty;
            int caret = 0;
            CompletionSelectionSnapshot? completionSelection = null;
            if (_editor.IsDisposed || !_editor.IsHandleCreated)
                return;

            await InvokeOnUiAsync(() =>
            {
                documentText = _editor.Text;
                caret = _editor.SelectionStart;
                completionSelection = _completionSelection;
            }).ConfigureAwait(false);

            if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(documentText))
                return;

            var suggestion = await _completeAsync(
                new InlineCompletionContext(documentText, caret, completionSelection),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested || string.IsNullOrEmpty(suggestion))
                return;

            await InvokeOnUiAsync(() =>
            {
                if (token.IsCancellationRequested
                    || _editor.SelectionStart != caret
                    || !Equals(_completionSelection, completionSelection))
                    return;

                var continuation = NormalizeContinuation(suggestion, completionSelection);
                var visibleSeed = completionSelection is null
                    ? string.Empty
                    : GetVisibleCompletionText(completionSelection);
                _ghostCompletionPrefixLength = visibleSeed.Length;
                SetGhostText(caret, visibleSeed + continuation);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FIM] inline completion failed: {ex}");
        }
#pragma warning restore CA1031
    }

    private Task InvokeOnUiAsync(Action action)
    {
        if (_editor.IsDisposed)
            return Task.CompletedTask;

        if (!_editor.InvokeRequired)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        _editor.BeginInvoke(() =>
        {
            try
            {
                action();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private void CancelPending()
    {
        try
        {
            _debounceCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    private void ClearGhostText()
    {
        if (!HasGhostText)
        {
            _ghostCompletionPrefixLength = 0;
            return;
        }

        _ghostText = null;
        _ghostOffset = -1;
        _ghostCompletionPrefixLength = 0;
        if (!_editor.IsDisposed && _editor.IsHandleCreated)
            _editor.Invalidate();
    }

    private void SetGhostText(int offset, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            ClearGhostText();
            return;
        }

        _ghostOffset = offset;
        _ghostText = text;
        _editor.Invalidate();
    }

    private string? CaptureCompletionContinuation()
    {
        if (!HasGhostText || string.IsNullOrEmpty(_ghostText))
            return null;

        return _ghostCompletionPrefixLength >= _ghostText.Length
            ? null
            : _ghostText[_ghostCompletionPrefixLength..];
    }

    private bool IsCompletionInserted(CompletionSelectionSnapshot selection)
    {
        var start = Math.Clamp(selection.ReplacementStartOffset, 0, _editor.TextLength);
        var insertText = selection.InsertText ?? string.Empty;
        var end = start + insertText.Length;
        if (_editor.SelectionStart < end || end > _editor.TextLength)
            return false;

        return string.Equals(
            _editor.Text.Substring(start, insertText.Length),
            insertText,
            StringComparison.Ordinal);
    }

    private void QueueCompletionContinuation(string continuation)
    {
        _pendingCompletionContinuation = continuation;
        if (_editor.IsDisposed || !_editor.IsHandleCreated)
        {
            ApplyPendingCompletionContinuation();
            return;
        }

        _editor.BeginInvoke((Action)ApplyPendingCompletionContinuation);
    }

    private void ApplyPendingCompletionContinuation()
    {
        var continuation = _pendingCompletionContinuation;
        _pendingCompletionContinuation = null;
        if (string.IsNullOrEmpty(continuation) || _editor.IsDisposed)
            return;

        SetGhostText(_editor.SelectionStart, continuation);
    }

    private static string NormalizeContinuation(string suggestion, CompletionSelectionSnapshot? selection)
    {
        if (selection is null || string.IsNullOrEmpty(suggestion))
            return suggestion;

        var selectedText = selection.InsertText;
        return suggestion.StartsWith(selectedText, StringComparison.OrdinalIgnoreCase)
            ? suggestion[selectedText.Length..]
            : suggestion;
    }
}
