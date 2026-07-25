using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

public sealed class SqlExecutionViewModelResultTests
{
    [Fact]
    public async Task SelectResult_sets_selected_result_set()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));

        Assert.Single(vm.ResultSets);
        vm.SelectResult("result-1");
        Assert.Equal("result-1", vm.SelectedResultSet!.ResultSetId);
    }

    [Fact]
    public async Task SelectResult_empty_string_clears_selection()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.SelectResult("");

        Assert.Null(vm.SelectedResultSet);
    }

    [Fact]
    public async Task SelectResult_null_clears_selection()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.SelectResult(null);

        Assert.Null(vm.SelectedResultSet);
    }

    [Fact]
    public async Task SelectResult_nonexistent_does_not_throw()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.SelectResult("does-not-exist");

        // SelectResult with nonexistent ID sets SelectedResultSet to null via FirstOrDefault
        Assert.Null(vm.SelectedResultSet);
    }

    [Fact]
    public async Task PinResult_sets_pinned_flag_on_result_set()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.PinResult("result-1");

        Assert.True(vm.ResultSets[0].IsPinned);
    }

    [Fact]
    public async Task UnpinResult_clears_pinned_flag_on_result_set()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.PinResult("result-1");
        vm.UnpinResult("result-1");

        Assert.False(vm.ResultSets[0].IsPinned);
    }

    [Fact]
    public async Task UnpinResult_nonexistent_does_not_throw()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.UnpinResult("does-not-exist");

        Assert.Single(vm.ResultSets);
    }

    [Fact]
    public async Task ClearResults_removes_unpinned_and_keeps_pinned()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new DualResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        Assert.Equal(2, vm.ResultSets.Count);

        vm.PinResult("result-1");
        vm.ClearResults();

        Assert.Single(vm.ResultSets);
        Assert.Equal("result-1", vm.ResultSets[0].ResultSetId);
    }

    [Fact]
    public async Task ClearResults_updates_selected_when_cleared()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new DualResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.PinResult("result-2");
        vm.ClearResults();

        // result-1 was unpinned and removed, result-2 is pinned and remains
        Assert.Single(vm.ResultSets);
        Assert.Equal("result-2", vm.SelectedResultSet!.ResultSetId);
    }

    [Fact]
    public async Task RemoveResult_removes_specific_result_set()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new DualResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.RemoveResult("result-1");

        Assert.Single(vm.ResultSets);
        Assert.Equal("result-2", vm.ResultSets[0].ResultSetId);
    }

    [Fact]
    public async Task RemoveResult_selected_updates_when_selected_is_removed()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new DualResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.SelectResult("result-1");
        vm.RemoveResult("result-1");

        Assert.Equal("result-2", vm.SelectedResultSet!.ResultSetId);
    }

    [Fact]
    public async Task RemoveResult_nonexistent_does_not_throw()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.RemoveResult("does-not-exist");

        Assert.Single(vm.ResultSets);
    }

    [Fact]
    public async Task RemoveResult_empty_id_does_not_throw()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.RemoveResult("");
        vm.RemoveResult(null);
        vm.RemoveResult("  ");

        Assert.Single(vm.ResultSets);
    }

    [Fact]
    public async Task EventReceived_fires_for_every_event()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);
        var events = new List<SqlExecutionEventKind>();
        vm.EventReceived += e => events.Add(e.Kind);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));

        Assert.Contains(SqlExecutionEventKind.Started, events);
        Assert.Contains(SqlExecutionEventKind.ResultSet, events);
        Assert.Contains(SqlExecutionEventKind.Completed, events);
    }

    [Fact]
    public async Task BeginRun_clears_previous_results_and_logs()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new DualResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        Assert.Equal(2, vm.ResultSets.Count);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));

        // Results should be reset at start of each run
        Assert.Equal(2, vm.ResultSets.Count);
        Assert.Empty(vm.Logs);
        Assert.Empty(vm.Diagnostics);
    }

    [Fact]
    public async Task PinResult_nonexistent_does_not_throw()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SingleResultSetUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));
        vm.PinResult("does-not-exist");
        vm.PinResult("");
        vm.PinResult(null);

        Assert.Single(vm.ResultSets);
    }

    [Fact]
    public void OutputPath_property_fires_changed()
    {
        var documentId = EditorDocumentId.New();
        using var vm = new SqlExecutionViewModel(documentId, new SingleResultSetUseCase(documentId));
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.OutputPath = "/tmp/out.csv";

        Assert.Equal("/tmp/out.csv", vm.OutputPath);
        Assert.Contains(nameof(SqlExecutionViewModel.OutputPath), changed);
    }

    [Fact]
    public async Task Logs_and_diagnostics_accumulate_across_events()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new EventRichUseCase(documentId);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));

        Assert.Single(vm.Logs);
        Assert.Single(vm.Diagnostics);
        Assert.Equal("test log", vm.Logs[0].Message);
        Assert.Equal("test diagnostic", vm.Diagnostics[0].Message);
    }

    // ── Helper use cases ──

    private sealed class SingleResultSetUseCase(EditorDocumentId documentId) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId);
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                "result-1", "Result 1",
                [new ResultColumnDescriptor(0, "col", "VARCHAR")]));
            yield return SqlExecutionEvent.RowsBatch(documentId, [["val"]]);
            yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class DualResultSetUseCase(EditorDocumentId documentId) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId);
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                "result-1", "Result 1",
                [new ResultColumnDescriptor(0, "col1", "VARCHAR")]));
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                "result-2", "Result 2",
                [new ResultColumnDescriptor(0, "col2", "INTEGER")]));
            yield return SqlExecutionEvent.RowsBatch(documentId, [["val"]]);
            yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class EventRichUseCase(EditorDocumentId documentId) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId);
            yield return new SqlExecutionEvent(SqlExecutionEventKind.Log, documentId)
            {
                Log = new SqlLogEntry(DateTimeOffset.UtcNow, SqlLogLevel.Information, "test log")
            };
            yield return new SqlExecutionEvent(SqlExecutionEventKind.Diagnostic, documentId)
            {
                Diagnostic = new SqlDiagnostic(SqlDiagnosticSeverity.Warning, "test diagnostic")
            };
            yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }
}
