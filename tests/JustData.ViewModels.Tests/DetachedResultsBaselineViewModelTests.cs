using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

/// <summary>
/// Baseline ViewModel guards that detached results must not break.
/// Green on main.
/// </summary>
public sealed class DetachedResultsBaselineViewModelTests
{
    [Fact]
    public async Task Pin_Clear_keeps_pinned_only()
    {
        var id = EditorDocumentId.New();
        using var vm = new SqlExecutionViewModel(id, new DualUseCase(id));
        await vm.RunAsync(new SqlExecutionRequest(id, "select 1"));
        Assert.Equal(2, vm.ResultSets.Count);
        vm.PinResult("result-1");
        vm.ClearResults();
        Assert.Single(vm.ResultSets);
        Assert.Equal("result-1", vm.ResultSets[0].ResultSetId);
        Assert.True(vm.ResultSets[0].IsPinned);
    }

    [Fact]
    public async Task RemoveResult_updates_selection()
    {
        var id = EditorDocumentId.New();
        using var vm = new SqlExecutionViewModel(id, new DualUseCase(id));
        await vm.RunAsync(new SqlExecutionRequest(id, "select 1"));
        vm.SelectResult("result-1");
        vm.RemoveResult("result-1");
        Assert.Equal("result-2", vm.SelectedResultSet!.ResultSetId);
    }

    [Fact]
    public async Task ClearResults_emits_Removed_for_unpinned_only()
    {
        var id = EditorDocumentId.New();
        using var vm = new SqlExecutionViewModel(id, new DualUseCase(id));
        var removed = new List<ResultSetKey>();
        vm.ResultRemoved += removed.Add;
        await vm.RunAsync(new SqlExecutionRequest(id, "select 1"));
        vm.PinResult("result-1");
        vm.ClearResults();
        Assert.Equal([new ResultSetKey(id, "result-2")], removed);
    }

    [Fact]
    public async Task Selecting_result_from_another_document_is_rejected()
    {
        var id = EditorDocumentId.New();
        using var vm = new SqlExecutionViewModel(id, new SingleUseCase(id));
        await vm.RunAsync(new SqlExecutionRequest(id, "select 1"));
        Assert.Throws<ArgumentException>(() => vm.SelectResultKey(new ResultSetKey(EditorDocumentId.New(), "result-1")));
    }

    private sealed class SingleUseCase(EditorDocumentId id) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(SqlExecutionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return SqlExecutionEvent.Started(id);
            yield return SqlExecutionEvent.Result(id, new ResultSetDescriptor("result-1", "Result 1", [new ResultColumnDescriptor(0, "c", "VARCHAR")]));
            yield return SqlExecutionEvent.RowsBatch(id, [["v"]]);
            yield return SqlExecutionEvent.Completed(id, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class DualUseCase(EditorDocumentId id) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(SqlExecutionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return SqlExecutionEvent.Started(id);
            yield return SqlExecutionEvent.Result(id, new ResultSetDescriptor("result-1", "Result 1", [new ResultColumnDescriptor(0, "c1", "VARCHAR")]));
            yield return SqlExecutionEvent.Result(id, new ResultSetDescriptor("result-2", "Result 2", [new ResultColumnDescriptor(0, "c2", "INTEGER")]));
            yield return SqlExecutionEvent.RowsBatch(id, [["v"]]);
            yield return SqlExecutionEvent.Completed(id, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }
}
