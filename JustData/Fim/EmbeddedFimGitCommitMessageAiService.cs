using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using JustData.Application.Git;
using JustyBase.Ai.Fim.LlamaSharp;
using System.Text;

namespace JustyBaseLegacy.UI.Fim;

/// <summary>
/// Uses the same embedded Qwen2.5-Coder GGUF host as FIM, but with plain text completion
/// (no FIM tokens). Few-shot continuation for commit messages.
/// </summary>
public sealed class EmbeddedFimGitCommitMessageAiService : IGitCommitMessageAiService
{
    private static readonly string[] AntiPrompts =
    [
        "<|endoftext|>",
        "<|fim_prefix|>",
        "<|fim_suffix|>",
        "<|fim_middle|>",
        "\nChanges:",
        "\n### ",
        "\nExample ",
        "\ndiff --git ",
    ];

    private static readonly string[] RejectPhrases =
    [
        "imperative subject",
        "optional short body",
        "markdown fences",
        "output only the commit",
        "write a concise git commit",
        "max ~72",
        "max 72",
    ];

    private readonly LlamaSharpModelHost _host;
    private readonly IApplicationSettingsContext _settings;

    public EmbeddedFimGitCommitMessageAiService(LlamaSharpModelHost host, IApplicationSettingsContext settings)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public bool IsAvailable => _settings.Config.EnableEmbeddedFimAi;

    public async Task<string?> GenerateAsync(string changeContext, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(changeContext))
            return null;

        string prompt = BuildCompletionPrompt(changeContext);
        string raw = await _host.InferAsync(
            prompt,
            AntiPrompts,
            maxTokens: 96,
            temperature: 0.2f,
            topP: 0.9f,
            cancellationToken).ConfigureAwait(false);

        string cleaned = CleanMessage(raw);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string BuildCompletionPrompt(string changeContext)
    {
        var sb = new StringBuilder(changeContext.Length + 900);
        sb.AppendLine("### Example 1");
        sb.AppendLine("Changes:");
        sb.AppendLine(" M Services/Auth/LoginService.cs");
        sb.AppendLine("diff --git a/Services/Auth/LoginService.cs b/Services/Auth/LoginService.cs");
        sb.AppendLine("@@");
        sb.AppendLine("- return password == stored;");
        sb.AppendLine("+ return SecureEquals(password, stored);");
        sb.AppendLine();
        sb.AppendLine("Commit message:");
        sb.AppendLine("Harden login password comparison");
        sb.AppendLine();
        sb.AppendLine("### Example 2");
        sb.AppendLine("Changes:");
        sb.AppendLine("A  Views/Tools/GitView.axaml");
        sb.AppendLine("M  ViewModels/Tools/GitViewModel.cs");
        sb.AppendLine();
        sb.AppendLine("Commit message:");
        sb.AppendLine("Add Git panel commit history tree");
        sb.AppendLine();
        sb.AppendLine("### Example 3");
        sb.AppendLine("Changes:");
        sb.AppendLine(changeContext.Trim());
        sb.AppendLine();
        sb.Append("Commit message:");
        sb.AppendLine();
        return sb.ToString();
    }

    internal static string CleanMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNl = text.IndexOf('\n', StringComparison.Ordinal);
            if (firstNl >= 0)
                text = text[(firstNl + 1)..];
            int fence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
                text = text[..fence];
            text = text.Trim();
        }

        string[] prefixes =
        [
            "Commit message:",
            "Commit Message:",
            "Message:",
            "Subject:",
        ];
        foreach (string prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                text = text[prefix.Length..].TrimStart();
        }

        if ((text.StartsWith('"') && text.EndsWith('"')) || (text.StartsWith('\'') && text.EndsWith('\'')))
            text = text[1..^1].Trim();

        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var kept = new List<string>(Math.Min(lines.Length, 6));
        foreach (string line in lines)
        {
            string trimmed = line.TrimEnd();
            if (trimmed.StartsWith("### ", StringComparison.Ordinal)
                || trimmed.StartsWith("Changes:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("diff --git ", StringComparison.Ordinal)
                || trimmed.StartsWith("Example ", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (kept.Count >= 6)
                break;

            kept.Add(trimmed);
        }

        string result = string.Join(Environment.NewLine, kept).Trim();
        if (LooksLikeInstructionEcho(result))
            return string.Empty;

        return result;
    }

    private static bool LooksLikeInstructionEcho(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        string upper = text.ToUpperInvariant();
        foreach (string phrase in RejectPhrases)
        {
            if (upper.Contains(phrase.ToUpperInvariant(), StringComparison.Ordinal))
                return true;
        }

        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int bulletish = 0;
        foreach (string line in lines)
        {
            string t = line.TrimStart();
            if (t.StartsWith("- ", StringComparison.Ordinal)
                || t.StartsWith("* ", StringComparison.Ordinal)
                || (t.Length > 2 && char.IsDigit(t[0]) && t[1] == '.'))
            {
                bulletish++;
            }
        }

        return bulletish >= 2;
    }
}
