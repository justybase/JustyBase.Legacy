using AppBase.Common;
using System.Drawing;

namespace AppBase.Tests.Common;

public sealed class DpiScaleAndDateTimeTests
{
    [Theory]
    [InlineData(96, 1f)]
    [InlineData(144, 1.5f)]
    [InlineData(192, 2f)]
    public void Factor_ReturnsScaleRelativeToDefaultDpi(int dpi, float expected)
    {
        Assert.Equal(expected, DpiScale.Factor(dpi));
    }

    [Fact]
    public void Scale_ScalesNumbersSizesAndPoints()
    {
        Assert.Equal(150, DpiScale.Scale(100, 144));
        Assert.Equal(7.5f, DpiScale.Scale(5f, 144));
        Assert.Equal(new Size(150, 75), DpiScale.Scale(new Size(100, 50), 144));
        Assert.Equal(new Point(15, 30), DpiScale.Scale(new Point(10, 20), 144));
    }

    [Theory]
    [InlineData("2026-04-07", "2026-04-03")]
    [InlineData("2026-06-05", "2026-06-03")]
    [InlineData("2026-01-05", "2026-01-02")]
    public void PreviousWorkDay_SkipsWeekendsAndPolishHolidays(string value, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), DateTimeAddons.PreviousWorkDay(DateTime.Parse(value)));
    }
}
