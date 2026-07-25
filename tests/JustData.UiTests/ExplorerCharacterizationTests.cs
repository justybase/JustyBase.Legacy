using FastColoredTextBoxNS.Helpers;
using JustyBaseLegacy.UI.Controls;
using System.Windows.Forms;

namespace JustData.UiTests;

/// <summary>
/// Characterization tests for the legacy object explorer.  These tests intentionally
/// exercise the existing control before the Phase 6 view-model path is composed.
/// </summary>
public sealed class ExplorerCharacterizationTests
{
    [Fact]
    [Trait("Category", "Characterization")]
    public void Legacy_object_explorer_keeps_sql_reference_order_and_comment_filtering()
    {
        RunInSta(() =>
        {
            using var control = new ObjectExplorerControl(
                hostWindows: null!,
                uiHelperService: null!,
                colorTheme: null!,
                autocompleteClass: new NoSqlAutocomplete(),
                imageList: new ImageList());
            control.CreateControl();

            const string sql = "-- INSERT INTO ignored_table\nINSERT INTO app.orders VALUES (1);\nCREATE TEMP TABLE temp_orders AS (SELECT 1);\nDROP TABLE app.old_orders;";
            control.ReBuildObjectExplorer(sql);

            WaitFor(() => control.ExplorerList.Count == 3);
            Assert.Collection(
                control.ExplorerList,
                item =>
                {
                    Assert.Equal(AppBase.Common.Enums.ExplorerItemType.Insert, item.type);
                    Assert.Equal("app.orders", item.Title);
                },
                item =>
                {
                    Assert.Equal(AppBase.Common.Enums.ExplorerItemType.TemporatyTable, item.type);
                    Assert.Equal("temp_orders", item.Title);
                },
                item =>
                {
                    Assert.Equal(AppBase.Common.Enums.ExplorerItemType.Drop, item.type);
                    Assert.Equal("app.old_orders", item.Title);
                });
        });
    }

    private static void WaitFor(Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!predicate() && DateTime.UtcNow < deadline)
        {
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(predicate(), "The legacy explorer did not publish its parsed list.");
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        using var completed = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
            finally { completed.Set(); }
            System.Windows.Forms.Application.ExitThread();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(completed.Wait(TimeSpan.FromSeconds(15)), "The STA characterization test timed out.");
        thread.Join();
        if (failure is not null)
            throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    private sealed class NoSqlAutocomplete : IAutocompleteClass
    {
        public Task AddAutocompleteForGeneral(int selectionStart, string cleanSqlText) => Task.CompletedTask;
        public Task AddAutocompleteForNZ(int selectionStart, string cleanSqlText) => Task.CompletedTask;
        public int LastSelect(ref string innerString, bool doTrim = true) => -1;
        public int FirstFrom(string afterSelect) => -1;
        public int FirstWhereGroupLimit(string txt) => -1;
    }
}
