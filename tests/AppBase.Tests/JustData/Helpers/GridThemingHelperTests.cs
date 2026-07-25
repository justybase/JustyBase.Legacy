using JustyBaseLegacy.UI.Helpers;

namespace AppBase.Tests.JustData.Helpers;

public sealed class GridThemingHelperTests
{
    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(GridThemingHelper.Default, GridThemingHelper.Default);
    }

    [Fact]
    public void Implements_IGridThemingHelper()
    {
        Assert.IsAssignableFrom<IGridThemingHelper>(GridThemingHelper.Default);
    }

    [Fact]
    public void ApplyScrollbarTheme_null_does_not_throw()
    {
        GridThemingHelper.ApplyScrollbarTheme(null!, true);
        GridThemingHelper.ApplyScrollbarTheme(null!, false);
    }

    [Fact]
    public void ApplyScrollbarThemeRecursive_null_does_not_throw()
    {
        GridThemingHelper.ApplyScrollbarThemeRecursive(null!, true);
    }

    [Fact]
    public void RecreateThemedDataGridHandlesRecursive_null_does_not_throw()
    {
        GridThemingHelper.RecreateThemedDataGridHandlesRecursive(null!);
    }

    [Fact]
    public void EnableDarkScrollbars_delegates_to_ApplyScrollbarTheme()
    {
        // Just verify it doesn't throw on null
        GridThemingHelper.EnableDarkScrollbars(null!, true);
    }
}
