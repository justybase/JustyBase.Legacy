using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Settings;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>Projects the draft's appearance into the legacy process and restores it on cancel.</summary>
public sealed class WinFormsSettingsThemePreviewAdapter : ISettingsThemePreview
{
    private readonly IApplicationSettingsContext _applicationSettingsContext;
    private readonly Action _repaint;
    private ApplicationSettingsDraft? _original;

    /// <summary>Allows the Preferences form to postpone the expensive repaint until it closes.</summary>
    public bool DeferCommitRepaint { get; set; }

    public WinFormsSettingsThemePreviewAdapter(IApplicationSettingsContext applicationSettingsContext, Action repaint)
    {
        _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
        _repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
    }

    public void Preview(ApplicationSettingsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        _original ??= LegacyApplicationSettingsMapper.ToSnapshot(_applicationSettingsContext.Config).ToDraft();
        LegacyApplicationSettingsMapper.ApplyToLegacy(draft, _applicationSettingsContext.Config);
        Repaint();
    }

    public void Commit(ApplicationSettingsSnapshot snapshot)
    {
        _original = null;
        if (!DeferCommitRepaint)
        {
            Repaint();
        }
    }

    public void Revert()
    {
        if (_original is null) return;
        LegacyApplicationSettingsMapper.ApplyToLegacy(_original, _applicationSettingsContext.Config);
        _original = null;
        Repaint();
    }

    private void Repaint()
    {
        Application.SetColorMode(_applicationSettingsContext.Config.UseSpecialColoring
            ? SystemColorMode.Dark
            : SystemColorMode.Classic);
        _repaint();
    }
}
