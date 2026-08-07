using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using JustyBase.Ai.Embedded.Download;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>
/// Preferences tab for the shared AI chat pipeline: backend, OpenAI-compatible
/// endpoint/key, defaults, request tuning and the embedded llama.cpp chat model.
/// </summary>
public sealed class AiChatPreferencesPanel : UserControl
{
    private readonly IApplicationSettingsContext _settings;
    private readonly EmbeddedChatModelCatalog? _chatCatalog;
    private static readonly ToolTip toolTip = new();

    private CheckBox? _enableChat;
    private ComboBox? _backend;
    private TextBox? _endpoint;
    private TextBox? _apiKey;
    private TextBox? _defaultModel;
    private ComboBox? _defaultMode;
    private TextBox? _reasoningEffort;
    private CheckBox? _autoConnect;
    private NumericUpDown? _historyLimit;
    private NumericUpDown? _temperature;
    private NumericUpDown? _maxTokens;
    private NumericUpDown? _timeoutMs;
    private NumericUpDown? _maxRetries;
    private TextBox? _promptOverride;
    private CheckBox? _enableEmbedded;
    private ComboBox? _embeddedModel;
    private NumericUpDown? _embeddedGpuLayers;
    private NumericUpDown? _embeddedCtxSize;
    private CheckBox? _preferVulkan;

    public AiChatPreferencesPanel(
        IApplicationSettingsContext settings,
        EmbeddedChatModelCatalog? chatCatalog = null)
    {
        _settings = settings;
        _chatCatalog = chatCatalog;
        AutoScroll = true;
        BuildLayout();
        LoadFromConfig();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(4),
            ColumnStyles =
            {
                new ColumnStyle(SizeType.AutoSize),
                new ColumnStyle(SizeType.Percent, 100)
            }
        };

        _enableChat = AddCheckBox(root, "Enable AI Chat", "Master switch for the AI Chat panel.");
        _backend = AddCombo(root, "Backend", "Codex (ChatGPT) / OpenAI-compatible / Embedded (local)");
        _backend.Items.AddRange(new object[] { "codex", "openai-compatible", "embedded" });
        _endpoint = AddTextBox(root, "OpenAI-compatible endpoint", "Base URL, e.g. http://localhost:1234/v1");
        _apiKey = AddTextBox(root, "API key (optional)", "Bearer token for the OpenAI-compatible endpoint");
        _apiKey.UseSystemPasswordChar = true;
        _defaultModel = AddTextBox(root, "Default model", "Model used for new chats");
        _defaultMode = AddCombo(root, "Default mode", "Expert / SQL Fix / Simple");
        _defaultMode.Items.AddRange(new object[] { "expert", "sqlfix", "simple" });
        _reasoningEffort = AddTextBox(root, "Reasoning effort (Codex)", "low / medium / high");
        _autoConnect = AddCheckBox(root, "Auto-connect on start", "Connect to the selected backend when the app starts.");
        _historyLimit = AddNumeric(root, "History limit (messages)", 1, 100);
        _temperature = AddNumeric(root, "Temperature", 0, 20);
        _temperature.DecimalPlaces = 1;
        _temperature.Increment = 0.1m;
        _maxTokens = AddNumeric(root, "Max tokens", 64, 32768);
        _timeoutMs = AddNumeric(root, "Request timeout (ms)", 1000, 600000);
        _timeoutMs.Increment = 1000;
        _maxRetries = AddNumeric(root, "Max retries", 0, 5);
        _promptOverride = AddMultiline(root, "System prompt override", "Optional text prepended to the mode prompt.");

        _enableEmbedded = AddCheckBox(root, "Enable Embedded AI (Chat)",
            "Bundled llama.cpp llama-server hosting a local GGUF chat model.");
        _embeddedModel = AddCombo(root, "Embedded model", "GGUF chat model id");
        _embeddedGpuLayers = AddNumeric(root, "Embedded GPU layers", 0, 999);
        _embeddedCtxSize = AddNumeric(root, "Embedded context size", 512, 131072);
        _embeddedCtxSize.Increment = 512;
        _preferVulkan = AddCheckBox(root, "Prefer Vulkan for llama-server", "Use the Vulkan llama.cpp build when available.");

        Controls.Add(root);
    }

    public void LoadFromConfig()
    {
        var config = _settings.Config;
        _enableChat!.Checked = config.EnableAiChat;
        _backend!.SelectedItem = config.AiChatBackendId;
        _endpoint!.Text = config.AiChatOpenAiCompatibleEndpoint;
        _apiKey!.Text = config.AiChatOpenAiCompatibleApiKey ?? string.Empty;
        _defaultModel!.Text = config.AiChatDefaultModel;
        _defaultMode!.SelectedItem = config.AiChatDefaultMode;
        _reasoningEffort!.Text = config.AiChatDefaultReasoningEffort;
        _autoConnect!.Checked = config.AiChatAutoConnect;
        _historyLimit!.Value = Math.Clamp(config.AiChatHistoryLimit, 1, 100);
        _temperature!.Value = Math.Clamp((decimal)config.AiChatTemperature, 0, 2);
        _maxTokens!.Value = Math.Clamp(config.AiChatMaxTokens, 64, 32768);
        _timeoutMs!.Value = Math.Clamp(config.AiChatRequestTimeoutMs, 1000, 600000);
        _maxRetries!.Value = Math.Clamp(config.AiChatMaxRetries, 0, 5);
        _promptOverride!.Text = config.AiChatSystemPromptOverride;
        _enableEmbedded!.Checked = config.EnableEmbeddedChatAi;
        _embeddedGpuLayers!.Value = Math.Clamp(config.EmbeddedChatGpuLayers, 0, 999);
        _embeddedCtxSize!.Value = Math.Clamp(config.EmbeddedChatCtxSize, 512, 131072);
        _preferVulkan!.Checked = config.LlamaServerPreferVulkan;

        _embeddedModel!.Items.Clear();
        if (_chatCatalog is not null)
        {
            foreach (var model in _chatCatalog.Models)
            {
                _embeddedModel.Items.Add(model);
            }

            _embeddedModel.DisplayMember = "DisplayName";
        }

        _embeddedModel.Items.Add(config.EmbeddedChatModelId);
        _embeddedModel.Text = config.EmbeddedChatModelId;
    }

    public void SaveFromUi()
    {
        var config = _settings.Config;
        config.EnableAiChat = _enableChat!.Checked;
        config.AiChatBackendId = _backend!.SelectedItem as string ?? config.AiChatBackendId;
        config.AiChatOpenAiCompatibleEndpoint = _endpoint!.Text.Trim();
        config.AiChatOpenAiCompatibleApiKey = string.IsNullOrWhiteSpace(_apiKey!.Text) ? null : _apiKey.Text.Trim();
        config.AiChatDefaultModel = _defaultModel!.Text.Trim();
        config.AiChatDefaultMode = _defaultMode!.SelectedItem as string ?? "expert";
        config.AiChatDefaultReasoningEffort = _reasoningEffort!.Text.Trim();
        config.AiChatAutoConnect = _autoConnect!.Checked;
        config.AiChatHistoryLimit = (int)_historyLimit!.Value;
        config.AiChatTemperature = (double)_temperature!.Value;
        config.AiChatMaxTokens = (int)_maxTokens!.Value;
        config.AiChatRequestTimeoutMs = (int)_timeoutMs!.Value;
        config.AiChatMaxRetries = (int)_maxRetries!.Value;
        config.AiChatSystemPromptOverride = _promptOverride!.Text;
        config.EnableEmbeddedChatAi = _enableEmbedded!.Checked;
        config.EmbeddedChatModelId = _embeddedModel!.SelectedItem is ModelDescriptor descriptor
            ? descriptor.Id
            : _embeddedModel.Text.Trim();
        config.EmbeddedChatGpuLayers = (int)_embeddedGpuLayers!.Value;
        config.EmbeddedChatCtxSize = (int)_embeddedCtxSize!.Value;
        config.LlamaServerPreferVulkan = _preferVulkan!.Checked;

        (_settings as IApplicationSettingsPersistence)?.SaveConfig();
    }

    private static CheckBox AddCheckBox(TableLayoutPanel root, string label, string tooltip)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var checkBox = new CheckBox { AutoSize = true, Margin = new Padding(4, 6, 4, 2) };
        toolTip.SetToolTip(checkBox, tooltip);
        root.Controls.Add(checkBox);
        return checkBox;
    }

    private static ComboBox AddCombo(TableLayoutPanel root, string label, string tooltip)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        toolTip.SetToolTip(combo, tooltip);
        root.Controls.Add(combo);
        return combo;
    }

    private static TextBox AddTextBox(TableLayoutPanel root, string label, string tooltip)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var textBox = new TextBox { Margin = new Padding(4, 4, 4, 2), Anchor = AnchorStyles.Left | AnchorStyles.Right };
        toolTip.SetToolTip(textBox, tooltip);
        root.Controls.Add(textBox);
        return textBox;
    }

    private static TextBox AddMultiline(TableLayoutPanel root, string label, string tooltip)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var textBox = new TextBox
        {
            Multiline = true,
            Height = 70,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        toolTip.SetToolTip(textBox, tooltip);
        root.Controls.Add(textBox);
        return textBox;
    }

    private static NumericUpDown AddNumeric(TableLayoutPanel root, string label, int min, int max)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var numeric = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left
        };
        root.Controls.Add(numeric);
        return numeric;
    }
}
