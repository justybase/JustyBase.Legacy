using AppBase.Common;
using AppBase.Common.Configuration;
using JustyBaseLegacy.UI.Configuration;
using System.Text.Json;

namespace JustData.Preferences.Tests;

public sealed class TerminalRemovalTests
{
    [Fact]
    public void Legacy_terminal_fields_remain_wire_and_mapper_compatible_without_runtime_feature()
    {
        var config = new ApplicationConfig
        {
            TerminalPanelVisible = true,
            TerminalPanelHeight = 777,
            TerminalShell = 1
        };

        string json = JsonSerializer.Serialize(config, MyJsonContextApplicationConfig.Default.ApplicationConfig);
        var roundTrip = JsonSerializer.Deserialize(json, MyJsonContextApplicationConfig.Default.ApplicationConfig)!;
        var mapped = LegacyApplicationSettingsMapper.ToLegacy(
            LegacyApplicationSettingsMapper.ToSnapshot(config).ToDraft());

        Assert.True(roundTrip.TerminalPanelVisible);
        Assert.Equal(777, roundTrip.TerminalPanelHeight);
        Assert.Equal(1, roundTrip.TerminalShell);
        Assert.True(mapped.TerminalPanelVisible);
        Assert.Equal(777, mapped.TerminalPanelHeight);
        Assert.Equal(1, mapped.TerminalShell);
    }
}
