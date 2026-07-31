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
    private readonly Func<InlineCompletionContext, CancellationToken, Task<string?>> _completeAsync;
    private readonly Func<int>? _getDebounceMs;
    private readonly Func<bool>? _isEnabledProvider;
    private readonly int _debounceMs;
    private CancellationTokenSource? _debounceCts;
    private string? _ghostText;
    private int _ghostOffset = -1;
    private bool _attached;
    private bool _disposed;

    public FctbInlineCompletionController(
        FastColoredTextBox editor,
        Func<InlineCompletionContext, CancellationToken, Task<string?>> completeAsync,
        int debounceMs = DefaultDebounceMs,
        Func<int>? getDebounceMs = null,
        Func<bool>? isEnabledProvider = null)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _completeAsync = completeAsync ?? throw new ArgumentNullException(nameof(completeAsync));
        _getDebounceMs = getDebounceMs;
        _isEnabledProvider = isEnabledProvider;
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
        _editor.KeyDown += OnKeyDown;
        _editor.PaintLine += OnPaintLine;
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
        _editor.KeyDown -= OnKeyDown;
        _editor.PaintLine -= OnPaintLine;
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
        if (HasGhostText)
            ClearGhostText();
        Schedule();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (HasGhostText && _ghostOffset != _editor.SelectionStart)
            ClearGhostText();
        Schedule();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab
            && e.Modifiers == Keys.None
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
            if (HasGhostText)
                ClearGhostText();
        }
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
            if (_editor.IsDisposed || !_editor.IsHandleCreated)
                return;

            await InvokeOnUiAsync(() =>
            {
                documentText = _editor.Text;
                caret = _editor.SelectionStart;
            }).ConfigureAwait(false);

            if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(documentText))
                return;

            var suggestion = await _completeAsync(
                new InlineCompletionContext(documentText, caret),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested || string.IsNullOrEmpty(suggestion))
                return;

            await InvokeOnUiAsync(() =>
            {
                if (token.IsCancellationRequested || _editor.SelectionStart != caret)
                    return;

                _ghostOffset = caret;
                _ghostText = suggestion;
                _editor.Invalidate();
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
            return;

        _ghostText = null;
        _ghostOffset = -1;
        if (!_editor.IsDisposed && _editor.IsHandleCreated)
            _editor.Invalidate();
    }
}
