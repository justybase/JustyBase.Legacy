using DatabaseDataGridView.WinForms;

namespace AppBase.Tests.JustData.Helpers;

/// <summary>
/// Regression: assigning Height on every virtual row unshares the entire grid and
/// freezes/flickers the UI for tens of thousands of result rows.
/// </summary>
public sealed class VirtualGridRowMetricsPolicyRegressionTests
{
    [Theory]
    [InlineData(true, 80_000, false)]
    [InlineData(true, 500, false)]
    [InlineData(true, 0, false)]
    [InlineData(false, 80_000, false)]
    [InlineData(false, 65, false)]
    [InlineData(false, 64, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 0, false)]
    public void ShouldAssignIndividualRowHeights_matches_virtual_mode_guard(
        bool virtualMode,
        int rowCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            VirtualGridRowMetricsPolicy.ShouldAssignIndividualRowHeights(virtualMode, rowCount));
    }

    [Fact]
    public void Large_virtual_result_never_retouches_individual_rows()
    {
        Assert.False(VirtualGridRowMetricsPolicy.ShouldAssignIndividualRowHeights(
            virtualMode: true,
            rowCount: 80_000));
    }
}
