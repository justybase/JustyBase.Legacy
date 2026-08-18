using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using JustyBase.Ai.Chat;
using JustyBase.Ai.Embedded.Abstractions;
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
    private readonly object? _chatBootstrap;
    private static readonly ToolTip toolTip = new();
    private bool _loading;

    private sealed record ComboOption(string Id, string Name);

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
    private Button? _embeddedDownload;
    private Button? _embeddedCancel;
    private ProgressBar? _embeddedProgress;
    private Label? _embeddedStatus;
    private CancellationTokenSource? _embeddedDownloadCts;
    private bool _isDisposing;

    public AiChatPreferencesPanel(
        IApplicationSettingsContext settings,
        EmbeddedChatModelCatalog? chatCatalog = null,
        object? chatBootstrap = null)
    {
        _settings = settings;
        _chatCatalog = chatCatalog;
        _chatBootstrap = chatBootstrap;
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

        AddSectionHeader(root, "General — defaults for new chat sessions");
        _enableChat = AddCheckBox(root, "Enable AI Chat", "Master switch for the AI Chat panel.", "aiChatEnable");
        _backend = AddCombo(root, "Backend", "Where AI requests are sent: Codex (ChatGPT), GitHub Copilot, OpenAI Compatible or Embedded (local). You can also switch instantly in the AI Chat panel header.", "aiChatBackend");
        _backend.Items.AddRange(new object[]
        {
            new ComboOption("codex", "Codex (ChatGPT)"),
            new ComboOption("copilot", "GitHub Copilot"),
            new ComboOption("openai-compatible", "OpenAI Compatible"),
            new ComboOption("embedded", "Embedded (local)")
        });
        _backend.DisplayMember = "Name";
        _backend.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
                return;

            // Picking "Embedded (local)" as the backend is the same decision as enabling it.
            if (string.Equals(GetSelectedId(_backend), "embedded", StringComparison.OrdinalIgnoreCase))
            {
                _enableEmbedded!.Checked = true;
            }
        };
        _endpoint = AddTextBox(root, "OpenAI-compatible endpoint", "Base URL, e.g. http://localhost:1234/v1", "aiChatEndpoint");
        _apiKey = AddTextBox(root, "API key (optional)", "Bearer token for the OpenAI-compatible endpoint", "aiChatApiKey");
        _apiKey.UseSystemPasswordChar = true;
        _defaultModel = AddTextBox(root, "Default model", "Model used for new chats with Codex / Copilot / OpenAI-compatible. The local model is chosen in the Embedded section below.", "aiChatDefaultModel");
        _defaultMode = AddCombo(root, "Default mode", "Expert / SQL Fix / Simple — prompt style for new chats.", "aiChatDefaultMode");
        _defaultMode.Items.AddRange(new object[]
        {
            new ComboOption("expert", "Expert"),
            new ComboOption("sqlfix", "SQL Fix"),
            new ComboOption("simple", "Simple")
        });
        _defaultMode.DisplayMember = "Name";
        _reasoningEffort = AddTextBox(root, "Reasoning effort (Codex / Copilot / Embedded)", "low / medium / high — for Codex, Copilot and the Embedded model (Qwen3-style thinking).", "aiChatReasoningEffort");
        _autoConnect = AddCheckBox(root, "Auto-connect on start", "Connect to the selected backend when the app starts.", "aiChatAutoConnect");
        _historyLimit = AddNumeric(root, "History limit (messages)", 1, 100, "aiChatHistoryLimit");
        _temperature = AddNumeric(root, "Temperature", 0, 20, "aiChatTemperature");
        _temperature.DecimalPlaces = 1;
        _temperature.Increment = 0.1m;
        _maxTokens = AddNumeric(root, "Max tokens", 64, 32768, "aiChatMaxTokens");
        _timeoutMs = AddNumeric(root, "Request timeout (ms)", 1000, 600000, "aiChatTimeoutMs");
        _timeoutMs.Increment = 1000;
        _maxRetries = AddNumeric(root, "Max retries", 0, 5, "aiChatMaxRetries");
        _promptOverride = AddMultiline(root, "System prompt override", "Optional text prepended to the mode prompt.", "aiChatPromptOverride");

        AddSectionHeader(root, "Embedded AI (local) — used when Backend = Embedded (local)");
        _enableEmbedded = AddCheckBox(root, "Enable Embedded AI (Chat)",
            "Master switch for the bundled llama.cpp model. Enabling it also selects Backend = Embedded (local); disabling it falls back to Codex.",
            "aiChatEnableEmbedded");
        _enableEmbedded.CheckedChanged += (_, _) =>
        {
            if (_loading)
                return;

            if (_enableEmbedded.Checked)
            {
                SelectId(_backend!, "embedded");
            }
            else if (string.Equals(GetSelectedId(_backend!), "embedded", StringComparison.OrdinalIgnoreCase))
            {
                SelectId(_backend!, "codex");
            }
        };
        _embeddedModel = AddCombo(root, "Embedded model", "GGUF chat model id — downloaded on first use when the Embedded backend connects.", "aiChatEmbeddedModel");
        _embeddedModel.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
                return;

            UpdateEmbeddedDownloadStatus();
        };
        _embeddedGpuLayers = AddNumeric(root, "Embedded GPU layers", 0, 999, "aiChatEmbeddedGpuLayers");
        _embeddedCtxSize = AddNumeric(root, "Embedded context size", 512, 131072, "aiChatEmbeddedContextSize");
        _embeddedCtxSize.Increment = 512;
        _preferVulkan = AddCheckBox(root, "Prefer Vulkan for llama-server", "Use the Vulkan llama.cpp build when available.", "aiChatPreferVulkan");
        AddEmbeddedDownloadControls(root);

        AddNote(root, "Changes to Backend take effect on the next start. To switch right away, open the AI Chat panel (Settings → Dock windows → AI Chat) and pick the backend in its header.");

        Controls.Add(root);
    }

    public void LoadFromConfig(IApplicationConfig? source = null)
    {
        _loading = true;
        try
        {
            var config = source ?? _settings.Config;
            _enableChat!.Checked = config.EnableAiChat;
            SelectId(_backend!, config.AiChatBackendId);
            if (_backend.SelectedIndex < 0 && _backend.Items.Count > 0)
            {
                _backend.SelectedIndex = 0;
            }
            _endpoint!.Text = config.AiChatOpenAiCompatibleEndpoint;
            _apiKey!.Text = config.AiChatOpenAiCompatibleApiKey ?? string.Empty;
            _defaultModel!.Text = config.AiChatDefaultModel;
            SelectId(_defaultMode!, config.AiChatDefaultMode);
            if (_defaultMode.SelectedIndex < 0 && _defaultMode.Items.Count > 0)
            {
                _defaultMode.SelectedIndex = 0;
            }
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

            object? selectedModel = _embeddedModel.Items
                .OfType<ModelDescriptor>()
                .FirstOrDefault(model => string.Equals(model.Id, config.EmbeddedChatModelId, StringComparison.OrdinalIgnoreCase));
            _embeddedModel.SelectedItem = selectedModel ?? config.EmbeddedChatModelId;
            UpdateEmbeddedDownloadStatus();
        }
        finally
        {
            _loading = false;
        }
    }

    public void SaveFromUi() => ApplyCurrentSettingsTo(_settings.Config);

    /// <summary>Copies the controls into the caller's transactional config buffer.</summary>
    public void ApplyCurrentSettingsTo(IApplicationConfig config)
    {
        config.EnableAiChat = _enableChat!.Checked;
        config.AiChatBackendId = GetSelectedId(_backend!) ?? config.AiChatBackendId;
        config.AiChatOpenAiCompatibleEndpoint = _endpoint!.Text.Trim();
        config.AiChatOpenAiCompatibleApiKey = string.IsNullOrWhiteSpace(_apiKey!.Text) ? null : _apiKey.Text.Trim();
        config.AiChatDefaultModel = _defaultModel!.Text.Trim();
        config.AiChatDefaultMode = GetSelectedId(_defaultMode!) ?? "expert";
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
        config.EmbeddedChatAcceptedLicenseModelIds = config.EmbeddedChatAcceptedLicenseModelIds?.ToList() ?? [];

    }

    private void AddEmbeddedDownloadControls(TableLayoutPanel root)
    {
        root.Controls.Add(new Label { Text = "Model actions", AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(4, 2, 4, 2)
        };

        _embeddedDownload = new Button
        {
            Text = "Download / prepare",
            AutoSize = true,
            Name = "aiChatEmbeddedDownload",
            AccessibleName = "aiChatEmbeddedDownload"
        };
        _embeddedDownload.Click += async (_, _) => await DownloadEmbeddedModelAsync();
        actions.Controls.Add(_embeddedDownload);

        _embeddedCancel = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            Name = "aiChatEmbeddedCancel",
            AccessibleName = "aiChatEmbeddedCancel",
            Enabled = false
        };
        _embeddedCancel.Click += (_, _) => _embeddedDownloadCts?.Cancel();
        actions.Controls.Add(_embeddedCancel);

        _embeddedProgress = new ProgressBar
        {
            Width = 220,
            Height = 22,
            Minimum = 0,
            Maximum = 100,
            Visible = _chatBootstrap is not null
        };
        actions.Controls.Add(_embeddedProgress);

        _embeddedStatus = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(640, 0),
            Margin = new Padding(4, 6, 4, 2)
        };
        actions.Controls.Add(_embeddedStatus);
        root.Controls.Add(actions);

        if (_chatBootstrap is null)
        {
            _embeddedDownload.Enabled = false;
            _embeddedStatus.Text = "Embedded model download service is unavailable.";
        }
    }

    private async Task DownloadEmbeddedModelAsync()
    {
        if (_chatBootstrap is null || _embeddedModel is null || _embeddedDownload is null || _embeddedCancel is null)
            return;

        var model = _embeddedModel.SelectedItem as ModelDescriptor
            ?? _chatCatalog?.Models.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, _embeddedModel.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? (ModelDescriptor)((dynamic)_chatBootstrap).CurrentModel;
        if (model.RequiresLicenseAcceptance
            && !_settings.Config.EmbeddedChatAcceptedLicenseModelIds.Contains(model.Id, StringComparer.OrdinalIgnoreCase))
        {
            var result = MessageBox.Show(
                this,
                $"{model.LicenseSummary}\n\nAccept {model.LicenseName} to download {model.DisplayName}?",
                "License acceptance required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                _embeddedStatus!.Text = "Download cancelled: license was not accepted.";
                return;
            }

            _settings.Config.EmbeddedChatAcceptedLicenseModelIds.Add(model.Id);
        }

        _embeddedDownloadCts?.Cancel();
        _embeddedDownloadCts?.Dispose();
        _embeddedDownloadCts = new CancellationTokenSource();
        _embeddedDownload.Enabled = false;
        _embeddedCancel.Enabled = true;
        _embeddedProgress!.Value = 0;
        _embeddedStatus!.Text = $"Preparing {model.DisplayName}…";

        try
        {
            var progress = new Progress<FimModelProgress>(update =>
            {
                if (!CanUpdateDownloadUi)
                    return;
                _embeddedProgress!.Value = Math.Clamp((int)Math.Round(update.Fraction * 100), 0, 100);
                _embeddedStatus!.Text = update.Message;
            });
            await ((dynamic)_chatBootstrap).EnsureModelAsync(model.Id, progress, _embeddedDownloadCts.Token).ConfigureAwait(true);
            if (CanUpdateDownloadUi)
            {
                _embeddedProgress.Value = 100;
                _embeddedStatus.Text = ((dynamic)_chatBootstrap).SelectedModelDiskStatus;
            }
        }
        catch (OperationCanceledException)
        {
            if (CanUpdateDownloadUi)
                _embeddedStatus.Text = "Model download cancelled.";
        }
        catch (Exception ex)
        {
            if (CanUpdateDownloadUi)
                _embeddedStatus.Text = $"Model download failed: {ex.Message}";
        }
        finally
        {
            if (CanUpdateDownloadUi)
            {
                _embeddedDownload.Enabled = true;
                _embeddedCancel.Enabled = false;
            }
            _embeddedDownloadCts?.Dispose();
            _embeddedDownloadCts = null;
        }
    }

    private bool CanUpdateDownloadUi
        => !_isDisposing && !IsDisposed && !Disposing;

    private void UpdateEmbeddedDownloadStatus()
    {
        if (CanUpdateDownloadUi && _embeddedStatus is not null && _chatBootstrap is not null)
            _embeddedStatus.Text = ((dynamic)_chatBootstrap).SelectedModelDiskStatus;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposing = true;
            _embeddedDownloadCts?.Cancel();
            _embeddedDownloadCts?.Dispose();
            _embeddedDownloadCts = null;
        }

        base.Dispose(disposing);
    }

    private static string? GetSelectedId(ComboBox combo)
        => combo.SelectedItem is ComboOption option ? option.Id : combo.SelectedItem as string;

    private static void SelectId(ComboBox combo, string? id)
    {
        var option = combo.Items
            .OfType<ComboOption>()
            .FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        combo.SelectedItem = option;
    }

    private static CheckBox AddCheckBox(TableLayoutPanel root, string label, string tooltip, string automationId)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var checkBox = new CheckBox
        {
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 2),
            Name = automationId,
            AccessibleName = automationId
        };
        toolTip.SetToolTip(checkBox, tooltip);
        root.Controls.Add(checkBox);
        return checkBox;
    }

    private static ComboBox AddCombo(TableLayoutPanel root, string label, string tooltip, string automationId)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Name = automationId,
            AccessibleName = automationId
        };
        toolTip.SetToolTip(combo, tooltip);
        root.Controls.Add(combo);
        return combo;
    }

    private static TextBox AddTextBox(TableLayoutPanel root, string label, string tooltip, string automationId)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var textBox = new TextBox
        {
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Name = automationId,
            AccessibleName = automationId
        };
        toolTip.SetToolTip(textBox, tooltip);
        root.Controls.Add(textBox);
        return textBox;
    }

    private static TextBox AddMultiline(TableLayoutPanel root, string label, string tooltip, string automationId)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var textBox = new TextBox
        {
            Multiline = true,
            Height = 70,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Name = automationId,
            AccessibleName = automationId
        };
        toolTip.SetToolTip(textBox, tooltip);
        root.Controls.Add(textBox);
        return textBox;
    }

    private static NumericUpDown AddNumeric(TableLayoutPanel root, string label, int min, int max, string automationId)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 8, 4, 2) });
        var numeric = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Margin = new Padding(4, 4, 4, 2),
            Anchor = AnchorStyles.Left,
            Name = automationId,
            AccessibleName = automationId
        };
        root.Controls.Add(numeric);
        return numeric;
    }

    private static void AddSectionHeader(TableLayoutPanel root, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(FontFamily.GenericSansSerif, 9.5F, FontStyle.Bold),
            Margin = new Padding(4, 16, 4, 6)
        };
        root.Controls.Add(label, 0, root.RowCount);
        root.SetColumnSpan(label, 2);
    }

    private static void AddNote(TableLayoutPanel root, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(label, 0, root.RowCount);
        root.SetColumnSpan(label, 2);
    }
}
