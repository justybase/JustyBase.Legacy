using JustyBase.Ai.Embedded.Abstractions;
using JustyBase.Ai.Embedded.Prompting;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>
/// Bridges editor inline-completion requests to an <see cref="ICompletionProvider"/>.
/// </summary>
public sealed class FimInlineCompletionBridge
{
    private readonly ICompletionProvider _provider;
    private readonly Func<FimPromptBudget> _getBudget;
    private readonly SemaphoreSlim _startGate = new(1, 1);

    public FimInlineCompletionBridge(
        ICompletionProvider provider,
        Func<bool> isEnabled,
        Func<FimPromptBudget>? getBudget = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        IsEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        _getBudget = getBudget ?? (() => FimPromptBudget.MediumDefault);
    }

    public Func<bool> IsEnabled { get; }

    /// <summary>
    /// Best-effort background preload: starts the llama-server only when the model is
    /// already on disk (never triggers a download from the editor hot path).
    /// </summary>
    public async Task<bool> TryPreloadAsync(CancellationToken cancellationToken = default)
    {
        if (_provider.IsReady)
        {
            return true;
        }

        if (!_provider.IsAvailable)
        {
            return false;
        }

        try
        {
            await _startGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (!_provider.IsReady)
                {
                    await _provider.EnsureReadyAsync(progress: null, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                _startGate.Release();
            }
        }
        catch
        {
            return false;
        }

        return _provider.IsReady;
    }

    public async Task<string?> CompleteAsync(InlineCompletionContext context, CancellationToken cancellationToken)
    {
        if (!IsEnabled())
            return null;

        try
        {
            var budget = _getBudget();
            var (promptText, promptCaret) = BuildPromptDocument(context);
            var (prefix, suffix) = FimContextExtractor.Extract(
                promptText,
                promptCaret,
                budget.MaxPromptTokens,
                budget.PrefixPercentage,
                budget.SuffixPercentage);
            if (string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(suffix))
                return null;

            // Start the server on demand when the model is on disk but the backend is not
            // running (app restart, crash, or a fresh install where Prepare was skipped).
            // A single gate serializes concurrent keystrokes during a slow start; a failed
            // start surfaces as "no completion" instead of throwing on every keystroke.
            if (!_provider.IsReady)
            {
                if (!_provider.IsAvailable)
                {
                    return null;
                }

                try
                {
                    await _startGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                    try
                    {
                        if (!_provider.IsReady)
                        {
                            await _provider.EnsureReadyAsync(progress: null, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        _startGate.Release();
                    }
                }
                catch
                {
                    return null;
                }

                if (!_provider.IsReady)
                {
                    return null;
                }
            }

            var maxTokens = FimContextExtractor.ClampMaxTokens(budget.MaxGenerationTokens);
            var suggestion = await _provider.CompleteAsync(
                new CompletionRequest(
                    prefix ?? string.Empty,
                    suffix ?? string.Empty,
                    MaxTokens: maxTokens),
                cancellationToken).ConfigureAwait(false);

            return suggestion?.Text;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            System.Diagnostics.Debug.WriteLine($"[FIM] CompleteAsync failed: {ex}");
            return null;
        }
    }

    private static (string Text, int CaretOffset) BuildPromptDocument(InlineCompletionContext context)
    {
        var selection = context.CompletionSelection;
        if (selection is null)
            return (context.DocumentText, context.CaretOffset);

        var documentText = context.DocumentText;
        var caret = Math.Clamp(context.CaretOffset, 0, documentText.Length);
        var start = Math.Clamp(selection.ReplacementStartOffset, 0, caret);
        var virtualText = string.Concat(documentText[..start], selection.InsertText, documentText[caret..]);
        return (virtualText, start + selection.InsertText.Length);
    }
}

public readonly record struct FimPromptBudget(
    int MaxPromptTokens,
    double PrefixPercentage,
    double SuffixPercentage,
    int MaxGenerationTokens)
{
    public static FimPromptBudget MediumDefault { get; } = new(1536, 0.65, 0.35, 50);
}

public sealed record CompletionSelectionSnapshot(
    string InsertText,
    int ReplacementStartOffset,
    int ReplacementEndOffset);

public readonly record struct InlineCompletionContext(
    string DocumentText,
    int CaretOffset,
    CompletionSelectionSnapshot? CompletionSelection = null);
