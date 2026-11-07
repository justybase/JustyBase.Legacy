using JustyBaseLegacy.UI;

namespace JustData.UiTests;

/// <summary>
/// Guards for the static shell: tab reordering must preserve selection and
/// the WinForms edge must not reference DockPanelSuite any more.
/// </summary>
public sealed class StaticShellTabStripTests
{
    [Fact]
    public void MoveTabPage_moves_page_and_preserves_selection()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var tabs = new TabControl();
                var first = new TabPage("First");
                var second = new TabPage("Second");
                var third = new TabPage("Third");
                tabs.TabPages.Add(first);
                tabs.TabPages.Add(second);
                tabs.TabPages.Add(third);
                tabs.SelectedTab = second;

                BaseWindow.MoveTabPage(tabs, 0, 2);

                Assert.Equal(["Second", "Third", "First"], tabs.TabPages.Cast<TabPage>().Select(page => page.Text));
                Assert.Same(second, tabs.SelectedTab);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    [Fact]
    public void MoveTabPage_ignores_invalid_indices()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var tabs = new TabControl();
                tabs.TabPages.Add(new TabPage("First"));
                tabs.TabPages.Add(new TabPage("Second"));

                BaseWindow.MoveTabPage(tabs, -1, 1);
                BaseWindow.MoveTabPage(tabs, 0, 5);
                BaseWindow.MoveTabPage(tabs, 1, 1);

                Assert.Equal(["First", "Second"], tabs.TabPages.Cast<TabPage>().Select(page => page.Text));
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    [Fact]
    public void JustData_does_not_reference_DockPanelSuite()
    {
        var references = typeof(BaseWindow).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => reference.Name?.Contains("WeifenLuo", StringComparison.OrdinalIgnoreCase) == true);
    }
}
