using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using DatabaseDataGridView.WinForms.Interfaces;
using FastColoredTextBoxNS;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;
using JustyBaseLegacy.UI;
using JustyBaseLegacy.UI.Sql;
using NSubstitute;
using System.Data;
using System.Windows.Forms;

namespace AppBase.Tests.JustData.Sql;

/// <summary>
/// Regression: ClearResults deferred via BeginInvoke re-tombstoned the reused
/// ResultSetId after Started cleared _removedResults, so run #2 never created a grid.
/// </summary>
public sealed class WinFormsSqlResultPresenterSecondRunRegressionTests
{
    [Fact]
    public Task Second_run_with_same_result_set_id_creates_a_new_grid() =>
        RunStaAsync(async () =>
        {
            var documentId = EditorDocumentId.New();
            string resultSetId = $"{documentId}-0-0";
            var view = new RecordingSqlResultView();
            using var presenter = new WinFormsSqlResultPresenter(view);
            using var vm = new SqlExecutionViewModel(documentId, new RepeatingResultUseCase(documentId, resultSetId));
            presenter.Attach(vm);
            vm.EventReceived += presenter.Handle;

            await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
            Assert.Equal(1, view.GridCreations);
            Assert.Empty(view.DeferredActions);

            await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
            Assert.Equal(2, view.GridCreations);
            Assert.Equal(2, view.TabCreations);
        });

    [Fact]
    public Task ClearResults_then_new_ResultSet_is_accepted_on_ui_thread() =>
        RunStaAsync(async () =>
        {
            var documentId = EditorDocumentId.New();
            string resultSetId = $"{documentId}-0-0";
            var view = new RecordingSqlResultView { InvokeRequiredValue = false };
            using var presenter = new WinFormsSqlResultPresenter(view);
            using var vm = new SqlExecutionViewModel(documentId, new RepeatingResultUseCase(documentId, resultSetId));
            presenter.Attach(vm);
            vm.EventReceived += presenter.Handle;

            await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
            vm.ClearResults();
            Assert.Empty(view.DeferredActions);

            presenter.Handle(SqlExecutionEvent.Started(documentId, 1));
            presenter.Handle(SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                resultSetId,
                "Result 1",
                [new ResultColumnDescriptor(0, "c1", "INTEGER")])));

            Assert.Equal(2, view.GridCreations);
        });

    private static Task RunStaAsync(Func<Task> body)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(async () =>
        {
            try
            {
                await body().ConfigureAwait(false);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private sealed class RepeatingResultUseCase(EditorDocumentId documentId, string resultSetId) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId, 1);
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                resultSetId,
                "Result 1",
                [new ResultColumnDescriptor(0, "c1", "INTEGER")]));
            yield return SqlExecutionEvent.RowsBatch(documentId, [[1]], resultSetId: resultSetId);
            yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class RecordingSqlResultView : IWinFormsSqlResultView
    {
        public bool InvokeRequiredValue { get; init; }
        public bool InvokeRequired => InvokeRequiredValue;
        public int GridCreations { get; private set; }
        public int TabCreations { get; private set; }
        public List<Action> DeferredActions { get; } = [];

        public bool CanPresentSqlResult(EditorDocumentId documentId) => true;

        public void BeginInvoke(Action action) => DeferredActions.Add(action);

        public TabPagePicture CreatePresentedResultTab(EditorDocumentId documentId, ResultSetDescriptor descriptor)
        {
            TabCreations++;
            return new TabPagePicture { Text = descriptor.Name };
        }

        public CustomDataGridView CreatePresentedResultGrid(
            EditorDocumentId documentId,
            TabPagePicture tab,
            ResultSetDescriptor descriptor,
            List<object[]> rows)
        {
            GridCreations++;
            var table = new DataTable();
            foreach (ResultColumnDescriptor column in descriptor.Columns)
                table.Columns.Add(column.Name, typeof(object));

            return new CustomDataGridView(
                Substitute.For<IColorTheme>(),
                Substitute.For<IExportMakes>(),
                Substitute.For<IUiHelperService>(),
                new FastColoredTextBox(),
                table,
                rows);
        }

        public void RegisterPresentedResultGrid(TabPage tab, CustomDataGridView grid)
        {
        }

        public void RemovePresentedResult(ResultSetKey key, TabPage? pendingTab = null, CustomDataGridView? pendingGrid = null)
        {
            pendingGrid?.Dispose();
            pendingTab?.Dispose();
        }
    }
}
