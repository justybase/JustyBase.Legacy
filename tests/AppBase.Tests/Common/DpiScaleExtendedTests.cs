using System.Drawing;
using AppBase.Common;

namespace AppBase.Tests.Common;

public sealed class DpiScaleExtendedTests
{
    [Theory]
    [InlineData(96, 1.0f)]
    [InlineData(192, 2.0f)]
    [InlineData(120, 1.25f)]
    [InlineData(72, 0.75f)]
    [InlineData(0, 0.0f)]
    public void Factor_scales_correctly(int dpi, float expected)
    {
        Assert.Equal(expected, DpiScale.Factor(dpi));
    }

    [Theory]
    [InlineData(100, 96, 100)]
    [InlineData(100, 192, 200)]
    [InlineData(100, 120, 125)]
    [InlineData(200, 96, 200)]
    public void Scale_int_scales_correctly(int logicalPixels, int dpi, int expected)
    {
        Assert.Equal(expected, DpiScale.Scale(logicalPixels, dpi));
    }

    [Fact]
    public void Scale_Size_scales_both_dimensions()
    {
        var size = new Size(100, 50);
        var scaled = DpiScale.Scale(size, 192);

        Assert.Equal(200, scaled.Width);
        Assert.Equal(100, scaled.Height);
    }

    [Fact]
    public void Scale_Point_scales_both_coordinates()
    {
        var point = new Point(10, 20);
        var scaled = DpiScale.Scale(point, 192);

        Assert.Equal(20, scaled.X);
        Assert.Equal(40, scaled.Y);
    }

    [Fact]
    public void Scale_float_scales_correctly()
    {
        Assert.Equal(25.0f, DpiScale.Scale(12.5f, 192));
    }

    [Fact]
    public void DefaultDpi_is_96()
    {
        Assert.Equal(96, DpiScale.DefaultDpi);
    }
}
