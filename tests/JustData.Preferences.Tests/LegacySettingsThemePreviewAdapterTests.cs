using AppBase.Common;
using AppBase.Common.Interfaces;
using JustData.Application.Settings;
using JustyBaseLegacy.UI;
using JustyBaseLegacy.UI.Configuration;
using NSubstitute;

namespace JustData.Preferences.Tests;

public sealed class WinFormsSettingsThemePreviewAdapterTests
{
    [Fact]
    public void Preview_is_reversible_and_commit_keeps_the_preview()
    {
        var config = new ApplicationConfig();
        config.MakeChangesInWrongConfigValues();
        bool original = config.UseSpecialColoring;
        var helpers = Substitute.For<IApplicationSettingsContext>();
        helpers.Config.Returns(config);
        int repaintCalls = 0;
        var adapter = new WinFormsSettingsThemePreviewAdapter(helpers, () => repaintCalls++);
        var draft = JustyBaseLegacy.UI.Configuration.LegacyApplicationSettingsMapper.ToSnapshot(config).ToDraft();
        draft.Appearance.UseSpecialColoring = !original;

        adapter.Preview(draft);
        Assert.Equal(!original, config.UseSpecialColoring);
        adapter.Revert();
        Assert.Equal(original, config.UseSpecialColoring);

        adapter.Preview(draft);
        adapter.Commit(new ApplicationSettingsSnapshot(draft));
        Assert.Equal(!original, config.UseSpecialColoring);
        adapter.Revert();
        Assert.Equal(!original, config.UseSpecialColoring);
        Assert.Equal(4, repaintCalls);
    }
}
