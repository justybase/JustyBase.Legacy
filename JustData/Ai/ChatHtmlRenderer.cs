using JustyBase.Ai.Chat;
using JustyBase.Ai.Models;
using Markdig;
using System.Text;
using System.Text.Json;

namespace JustyBaseLegacy.UI.Ai;

/// <summary>Builds the sanitized conversation HTML rendered in the chat panel WebView2.</summary>
internal static class ChatHtmlRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .Build();

    public static string Render(IReadOnlyList<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Roboto,sans-serif;font-size:13px;margin:12px;background:#fff;color:#222;}");
        sb.AppendLine(".msg{display:flex;margin:10px 0;}");
        sb.AppendLine(".user{justify-content:flex-end;}");
        sb.AppendLine(".bubble{max-width:82%;padding:8px 12px;border-radius:10px;white-space:pre-wrap;word-break:break-word;}");
        sb.AppendLine(".user .bubble{background:#dbeafe;color:#1e3a5f;}");
        sb.AppendLine(".assistant .bubble{background:#f3f4f6;color:#111;border:1px solid #e5e7eb;}");
        sb.AppendLine(".system .bubble{background:#fef3c7;color:#78350f;border:1px solid #fde68a;font-size:12px;}");
        sb.AppendLine("pre{background:#0f172a;color:#e2e8f0;padding:10px;border-radius:8px;overflow:auto;font-size:12px;}");
        sb.AppendLine("code{font-family:Consolas,monospace;}");
        sb.AppendLine("p{margin:4px 0;}");
        sb.AppendLine("table{border-collapse:collapse;margin:6px 0;}td,th{border:1px solid #cbd5e1;padding:3px 8px;}");
        sb.AppendLine(".tool-card{border:1px solid #94a3b8;border-left:4px solid #64748b;padding:8px;border-radius:6px;margin:6px 0;background:#f8fafc;}");
        sb.AppendLine(".tool-card button{margin:4px 6px 0 0;padding:4px 14px;border-radius:4px;border:1px solid #94a3b8;cursor:pointer;}");
        sb.AppendLine(".approve{background:#dcfce7;}.deny{background:#fee2e2;}");
        sb.AppendLine(".meta{color:#6b7280;font-size:11px;margin-top:2px;}");
        sb.AppendLine("</style>");
        sb.AppendLine("<script>");
        sb.AppendLine("function confirmTool(allow) { window.chrome.webview.postMessage(allow ? 'approve' : 'deny'); }");
        sb.AppendLine("</script>");
        sb.AppendLine("</head><body>");

        foreach (var message in messages)
        {
            var role = message.Role;
            if (role.Equals("tool-confirmation", StringComparison.OrdinalIgnoreCase))
            {
                RenderToolConfirmation(sb, message);
                continue;
            }

            var cssClass = role.Equals("user", StringComparison.OrdinalIgnoreCase)
                ? "user"
                : role.Equals("system", StringComparison.OrdinalIgnoreCase)
                    ? "system"
                    : "assistant";
            sb.Append("<div class='msg ").Append(cssClass).Append("'><div class='bubble'>");
            var content = ChatMarkdownSanitizer.Sanitize(message.Content ?? string.Empty);
            if (message.IsStreaming && string.IsNullOrWhiteSpace(content))
            {
                sb.Append("<em style='color:#9ca3af'>Thinking…</em>");
            }
            else
            {
                var html = Markdown.ToHtml(content, Pipeline);
                sb.Append(html);
            }

            if (message.GenerationTimeMs > 0)
            {
                sb.Append("<div class='meta'>").Append(message.GenerationTimeDisplay).Append("</div>");
            }

            sb.Append("</div></div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void RenderToolConfirmation(StringBuilder sb, ChatMessage message)
    {
        sb.Append("<div class='msg'><div class='bubble tool-card'>");
        sb.Append("<div><b>").Append(Escape(message.ToolName)).Append("</b></div>");
        sb.Append("<div style='white-space:pre-wrap'>").Append(Escape(message.ToolArgs)).Append("</div>");
        if (message.ConfirmationPending)
        {
            sb.Append("<button class='approve' onclick='confirmTool(true)'>Approve</button>");
            sb.Append("<button class='deny' onclick='confirmTool(false)'>Deny</button>");
        }
        else
        {
            sb.Append("<div class='meta'>").Append(Escape(message.Content)).Append("</div>");
        }

        sb.Append("</div></div>");
    }

    private static string Escape(string text)
        => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
}
