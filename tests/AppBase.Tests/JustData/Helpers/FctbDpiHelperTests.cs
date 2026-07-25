using JustyBaseLegacy.UI.Helpers;

namespace AppBase.Tests.JustData.Helpers;

public sealed class FctbDpiHelperTests
{
    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(FctbDpiHelper.Default, FctbDpiHelper.Default);
    }

    [Fact]
    public void Implements_IFctbDpiHelper()
    {
        Assert.IsAssignableFrom<IFctbDpiHelper>(FctbDpiHelper.Default);
    }

    [Fact]
    public void ApplyCharMetrics_null_does_not_throw()
    {
        // Should handle null gracefully
        FctbDpiHelper.ApplyCharMetrics(null!);
    }
}
