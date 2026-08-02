using System.Runtime.CompilerServices;
using AppBase.Common.Interfaces;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>Attaches / detaches FCTB inline FIM controllers per editor instance.</summary>
public sealed class FimEditorHost : IDisposable
{
    private readonly FimInlineCompletionBridge _bridge;
    private readonly IApplicationSettingsContext _settings;
    private readonly ConditionalWeakTable<FastColoredTextBox, FctbInlineCompletionController> _controllers = new();
    private bool _disposed;

    public FimEditorHost(FimInlineCompletionBridge bridge, IApplicationSettingsContext settings)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Attach(FastColoredTextBox editor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(editor);

        if (_controllers.TryGetValue(editor, out _))
            return;

        var controller = new FctbInlineCompletionController(
            editor,
            (ctx, ct) => _bridge.CompleteAsync(ctx, ct),
            getDebounceMs: () =>
            {
                var cfg = _settings.Config;
                if (cfg.EmbeddedFimDebounceMs > 0)
                    return cfg.EmbeddedFimDebounceMs;
                if (cfg.EmbeddedFimDebounceSeconds > 0)
                    return FctbInlineCompletionController.DebounceMsFromSeconds(cfg.EmbeddedFimDebounceSeconds);
                return FctbInlineCompletionController.DefaultDebounceMs;
            },
            isEnabledProvider: () => _settings.Config.EnableEmbeddedFimAi,
            completionMenu: (editor.Tag as TbInfo)?.PopupMenu);
        controller.Attach();
        _controllers.Add(editor, controller);

        // Proactively preload the model in the background so the first keystroke is fast.
        if (_settings.Config.EnableEmbeddedFimAi)
        {
            _ = Task.Run(async () =>
            {
                try { await _bridge.TryPreloadAsync().ConfigureAwait(false); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FIM] Background preload failed: {ex.Message}"); }
            });
        }
    }

    public void SyncEnabled()
    {
        // ConditionalWeakTable does not enumerate; controllers sync via getDebounceMs/IsEnabled checks on schedule.
        // Re-read IsEnabled on next keystroke via bridge.IsEnabled; flip attached controllers when possible.
    }

    public void Dispose() => _disposed = true;
}
