using AppBase.Common.Interfaces;
using AppBase.Common.Configuration;
using JustyBase.Ai.Embedded.Abstractions;
using JustyBase.Ai.Embedded.Download;
using JustyBase.Ai.Embedded.Prompting;
using JustyBase.Ai.Embedded.Server;
using JustyBaseLegacy.UI.Fim;
using JustyBaseLegacy.UI.Configuration;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>Preferences UI for embedded Fill-in-the-Middle (local GGUF).</summary>
public sealed class EmbeddedFimPreferencesPanel : UserControl
{
    private readonly IApplicationSettingsContext _settings;
    private readonly IModelCatalog _catalog;
    private readonly IFimModelBootstrapService _bootstrap;
    private readonly CheckBox _chkEnable = new();
    private readonly ComboBox _cmbPreset = new();
    private readonly ComboBox _cmbModel = new();
    private readonly NumericUpDown _nudDebounce = new();
    private readonly NumericUpDown _nudMaxTokens = new();
    private readonly NumericUpDown _nudPromptTokens = new();
    private readonly NumericUpDown _nudGpuLayers = new();
    private readonly NumericUpDown _nudCtxSize = new();
    private readonly CheckBox _chkVulkan = new();
    private readonly Button _btnDownload = new();
    private readonly Button _btnDelete = new();
    private readonly Button _btnSpeed = new();
    private readonly Label _lblStatus = new();
    private bool _suppress;

    public EmbeddedFimPreferencesPanel(
        IApplicationSettingsContext settings,
        IModelCatalog catalog,
        IFimModelBootstrapService bootstrap)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));

        Dock = DockStyle.Fill;
        AutoScroll = true;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(4)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        void AddRow(string label, Control control)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            layout.Controls.Add(control, 1, row);
            row++;
        }

        _chkEnable.Text = "Enable Fill-in-the-Middle";
        _chkEnable.AutoSize = true;
        AddRow("Enable", _chkEnable);

        _cmbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbPreset.Items.AddRange(["Small", "Medium", "Large", "Custom"]);
        AddRow("Preset", _cmbPreset);

        _cmbModel.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var model in _catalog.Models)
            _cmbModel.Items.Add(model.Id);
        AddRow("Model", _cmbModel);

        _nudDebounce.Minimum = 250;
        _nudDebounce.Maximum = 3000;
        _nudDebounce.Increment = 50;
        AddRow("Suggestion delay (ms)", _nudDebounce);

        _nudMaxTokens.Minimum = 20;
        _nudMaxTokens.Maximum = 200;
        AddRow("Max generation tokens", _nudMaxTokens);

        _nudPromptTokens.Minimum = 256;
        _nudPromptTokens.Maximum = 8192;
        _nudPromptTokens.Increment = 128;
        AddRow("Max prompt tokens", _nudPromptTokens);

        _chkVulkan.Text = "Prefer Vulkan GPU";
        _chkVulkan.AutoSize = true;
        AddRow("GPU", _chkVulkan);

        _nudGpuLayers.Minimum = 0;
        _nudGpuLayers.Maximum = 999;
        AddRow("GPU layers", _nudGpuLayers);

        _nudCtxSize.Minimum = 512;
        _nudCtxSize.Maximum = 131072;
        _nudCtxSize.Increment = 512;
        AddRow("Context size (tokens)", _nudCtxSize);

        var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
        _btnDownload.Text = "Download / prepare";
        _btnDownload.AutoSize = true;
        _btnDelete.Text = "Delete model";
        _btnDelete.AutoSize = true;
        _btnSpeed.Text = "Speed test";
        _btnSpeed.AutoSize = true;
        actions.Controls.Add(_btnDownload);
        actions.Controls.Add(_btnDelete);
        actions.Controls.Add(_btnSpeed);
        AddRow("Actions", actions);

        _lblStatus.AutoSize = true;
        _lblStatus.MaximumSize = new Size(560, 0);
        AddRow("Status", _lblStatus);

        Controls.Add(layout);

        _chkEnable.CheckedChanged += (_, _) => SaveFromUi();
        _cmbPreset.SelectedIndexChanged += (_, _) =>
        {
            if (_suppress) return;
            ApplyPreset(_cmbPreset.SelectedItem?.ToString());
            SaveFromUi();
        };
        _cmbModel.SelectedIndexChanged += (_, _) => SaveFromUi();
        _nudDebounce.ValueChanged += (_, _) => SaveFromUi();
        _nudMaxTokens.ValueChanged += (_, _) => SaveFromUi();
        _nudPromptTokens.ValueChanged += (_, _) => SaveFromUi();
        _nudGpuLayers.ValueChanged += (_, _) => SaveFromUi();
        _chkVulkan.CheckedChanged += (_, _) => SaveFromUi();

        _btnDownload.Click += async (_, _) => await DownloadAsync();
        _btnDelete.Click += async (_, _) => await DeleteAsync();
        _btnSpeed.Click += async (_, _) => await SpeedAsync();

        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        _suppress = true;
        try
        {
            var c = _settings.Config;
            _chkEnable.Checked = c.EnableEmbeddedFimAi;
            _cmbPreset.SelectedItem = string.IsNullOrWhiteSpace(c.EmbeddedFimPreset) ? "Medium" : c.EmbeddedFimPreset;
            if (_cmbModel.Items.Contains(c.EmbeddedFimModelId))
                _cmbModel.SelectedItem = c.EmbeddedFimModelId;
            else if (_cmbModel.Items.Count > 0)
                _cmbModel.SelectedIndex = 0;
            _nudDebounce.Value = Math.Clamp(c.EmbeddedFimDebounceMs > 0 ? c.EmbeddedFimDebounceMs : 600, 250, 3000);
            _nudMaxTokens.Value = Math.Clamp(c.EmbeddedFimMaxTokens, 20, 200);
            _nudPromptTokens.Value = Math.Clamp(c.EmbeddedFimMaxPromptTokens, 256, 8192);
            _nudGpuLayers.Value = Math.Clamp(c.EmbeddedFimGpuLayers < 0 ? 99 : c.EmbeddedFimGpuLayers, 0, 999);
            _nudCtxSize.Value = Math.Clamp(c.EmbeddedFimCtxSize, 512, 131072);
            _chkVulkan.Checked = c.LlamaServerPreferVulkan;
            _lblStatus.Text = _bootstrap.SelectedModelDiskStatus + Environment.NewLine + "Models: " + _bootstrap.ModelsDirectory;
        }
        finally
        {
            _suppress = false;
        }
    }

    private void SaveFromUi()
    {
        if (_suppress)
            return;

        var c = _settings.Config;
        c.EnableEmbeddedFimAi = _chkEnable.Checked;
        c.EmbeddedFimPreset = _cmbPreset.SelectedItem?.ToString() ?? "Medium";
        c.EmbeddedFimModelId = _cmbModel.SelectedItem?.ToString() ?? c.EmbeddedFimModelId;
        c.EmbeddedFimDebounceMs = (int)_nudDebounce.Value;
        c.EmbeddedFimMaxTokens = (int)_nudMaxTokens.Value;
        c.EmbeddedFimMaxPromptTokens = (int)_nudPromptTokens.Value;
        c.EmbeddedFimGpuLayers = (int)_nudGpuLayers.Value;
        c.EmbeddedFimCtxSize = (int)_nudCtxSize.Value;
        c.LlamaServerPreferVulkan = _chkVulkan.Checked;

        if (string.Equals(c.EmbeddedFimPreset, "Small", StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.EmbeddedFimPreset, "Medium", StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.EmbeddedFimPreset, "Large", StringComparison.OrdinalIgnoreCase))
        {
            var preset = FimPresets.Get(c.EmbeddedFimPreset);
            c.EmbeddedFimPrefixPercentage = preset.PrefixPercentage;
            c.EmbeddedFimSuffixPercentage = preset.SuffixPercentage;
        }
    }

    /// <summary>
    /// Copies the current panel state into the Preferences transaction buffer.
    /// The panel keeps using the live config for model download/runtime setup,
    /// while Preferences persists the copied values together with its draft.
    /// </summary>
    public void ApplyCurrentSettingsTo(IApplicationConfig target)
    {
        ArgumentNullException.ThrowIfNull(target);
        SaveFromUi();
        LegacyApplicationSettingsMapper.CopyEmbeddedFimSettings(_settings.Config, target);
    }

    private void ApplyPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || string.Equals(preset, "Custom", StringComparison.OrdinalIgnoreCase))
            return;

        var def = FimPresets.Get(preset);
        _suppress = true;
        try
        {
            _nudPromptTokens.Value = Math.Clamp(def.MaxPromptTokens, 256, 8192);
            _nudMaxTokens.Value = Math.Clamp(def.MaxGenerationTokens, 20, 200);
            if (_cmbModel.Items.Contains(def.ModelId))
                _cmbModel.SelectedItem = def.ModelId;
        }
        finally
        {
            _suppress = false;
        }
    }

    private async Task DownloadAsync()
    {
        SaveFromUi();
        var model = _catalog.Resolve(_settings.Config.EmbeddedFimModelId);
        if (model.RequiresLicenseAcceptance
            && !_settings.Config.EmbeddedFimAcceptedLicenseModelIds.Contains(model.Id, StringComparer.OrdinalIgnoreCase))
        {
            var result = MessageBox.Show(
                this,
                $"{model.LicenseSummary}\n\nAccept {model.LicenseName} to download {model.DisplayName}?",
                "License acceptance required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                _lblStatus.Text = "Download cancelled: license was not accepted.";
                return;
            }

            _settings.Config.EmbeddedFimAcceptedLicenseModelIds.Add(model.Id);
        }

        _btnDownload.Enabled = false;
        _lblStatus.Text = "Downloading / preparing model…";
        try
        {
            var progress = new Progress<FimModelProgress>(p =>
            {
                if (IsHandleCreated)
                    BeginInvoke(() => _lblStatus.Text = p.Message);
            });
            await _bootstrap.EnsureReadyAsync(progress).ConfigureAwait(true);
            _lblStatus.Text = _bootstrap.SelectedModelDiskStatus;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Failed: " + ex.Message;
        }
        finally
        {
            _btnDownload.Enabled = true;
        }
    }

    private async Task DeleteAsync()
    {
        SaveFromUi();
        try
        {
            await _bootstrap.DeleteSelectedModelAsync().ConfigureAwait(true);
            _lblStatus.Text = _bootstrap.SelectedModelDiskStatus;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Delete failed: " + ex.Message;
        }
    }

    private async Task SpeedAsync()
    {
        SaveFromUi();
        var c = _settings.Config;
        _btnSpeed.Enabled = false;
        _lblStatus.Text = "Running speed test…";
        try
        {
            var report = await _bootstrap.RunSpeedTestAsync(
                (int)_nudMaxTokens.Value,
                (int)_nudPromptTokens.Value,
                c.EmbeddedFimPrefixPercentage,
                c.EmbeddedFimSuffixPercentage).ConfigureAwait(true);
            _lblStatus.Text = report.Succeeded
                ? $"{report.ModelName}: {report.TokensPerSecond:0.#} tok/s ({report.ElapsedMs} ms)."
                : $"{report.ModelName}: speed test produced no completion.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Speed test failed: " + ex.Message;
        }
        finally
        {
            _btnSpeed.Enabled = true;
        }
    }
}
