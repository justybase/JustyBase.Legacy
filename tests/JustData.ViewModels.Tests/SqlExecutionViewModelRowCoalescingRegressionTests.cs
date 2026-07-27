using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

/// <summary>
/// Regression: large result sets must not marshal every 500-row batch onto the UI
/// thread. After the preview threshold, remaining rows are flushed once.
/// </summary>
public sealed class SqlExecutionViewModelRowCoalescingRegressionTests
{
    [Fact]
    public async Task Large_result_marshals_preview_then_one_coalesced_rows_flush()
    {
        var documentId = EditorDocumentId.New();
        var dispatcher = new RecordingDispatcher();
        var useCase = new ManyRowBatchesUseCase(documentId, batchCount: 160, batchSize: 500);
        using var vm = new SqlExecutionViewModel(
            documentId,
            (mode, outputMode, outputPath) => new SqlExecutionRequest(documentId, "select * from t")
            {
                Mode = mode,
                OutputMode = outputMode,
                OutputPath = outputPath
            },
            useCase,
            dispatcher);

        var rowsEvents = new List<SqlExecutionEvent>();
        vm.EventReceived += e =>
        {
            if (e.Kind == SqlExecutionEventKind.Rows)
                rowsEvents.Add(e);
        };

        SqlExecutionOutcome outcome = await vm.RunAsync(new SqlExecutionRequest(documentId, "select * from t"));

        Assert.Equal(SqlExecutionOutcome.Success, outcome);
        Assert.Equal(80_000, vm.RowCount);

        // Preview batch (500) + one coalesced flush (79_500) — not 160 UI rows events.
        Assert.Equal(2, rowsEvents.Count);
        Assert.Equal(500, rowsEvents[0].Rows!.Count);
        Assert.Equal(79_500, rowsEvents[1].Rows!.Count);

        // Critical invariant: far fewer UI marshals than one-per-batch (160+).
        Assert.True(
            dispatcher.InvocationCount < 40,
            $"Expected coalesced UI traffic well below one-invoke-per-batch, got {dispatcher.InvocationCount}.");
    }

    [Fact]
    public async Task Small_result_below_preview_threshold_is_not_coalesced_away()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new ManyRowBatchesUseCase(documentId, batchCount: 1, batchSize: 1);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        var rowsEvents = new List<SqlExecutionEvent>();
        vm.EventReceived += e =>
        {
            if (e.Kind == SqlExecutionEventKind.Rows)
                rowsEvents.Add(e);
        };

        await vm.RunAsync(new SqlExecutionRequest(documentId, "select 1"));

        Assert.Equal(1, vm.RowCount);
        Assert.Single(rowsEvents);
        Assert.Single(rowsEvents[0].Rows!);
    }

    [Fact]
    public async Task Coalesced_rows_are_flushed_even_when_completion_event_is_missing()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new MissingCompletionAfterLargeRowsUseCase(documentId, batchCount: 160, batchSize: 500);
        using var vm = new SqlExecutionViewModel(documentId, useCase);

        var rowsEvents = new List<SqlExecutionEvent>();
        vm.EventReceived += e =>
        {
            if (e.Kind == SqlExecutionEventKind.Rows)
                rowsEvents.Add(e);
        };

        SqlExecutionOutcome outcome = await vm.RunAsync(new SqlExecutionRequest(documentId, "select * from t"));

        Assert.Equal(SqlExecutionOutcome.Failed, outcome);
        Assert.Equal(80_000, vm.RowCount);
        Assert.Equal(2, rowsEvents.Count);
        Assert.Equal(500, rowsEvents[0].Rows!.Count);
        Assert.Equal(79_500, rowsEvents[1].Rows!.Count);
    }

    private sealed class ManyRowBatchesUseCase(
        EditorDocumentId documentId,
        int batchCount,
        int batchSize) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId, 1);
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                $"{documentId}-0-0",
                "Result 1",
                [new ResultColumnDescriptor(0, "c1", "INTEGER")],
                StatementIndex: 0));

            for (int batch = 0; batch < batchCount; batch++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = new IReadOnlyList<object?>[batchSize];
                for (int i = 0; i < batchSize; i++)
                    rows[i] = [batch * batchSize + i];
                yield return SqlExecutionEvent.RowsBatch(documentId, rows, statementIndex: 0, resultSetId: $"{documentId}-0-0");
            }

            yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class MissingCompletionAfterLargeRowsUseCase(
        EditorDocumentId documentId,
        int batchCount,
        int batchSize) : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Started(documentId, 1);
            yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
                $"{documentId}-0-0",
                "Result 1",
                [new ResultColumnDescriptor(0, "c1", "INTEGER")],
                StatementIndex: 0));

            for (int batch = 0; batch < batchCount; batch++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = new IReadOnlyList<object?>[batchSize];
                for (int i = 0; i < batchSize; i++)
                    rows[i] = [batch * batchSize + i];
                yield return SqlExecutionEvent.RowsBatch(documentId, rows, statementIndex: 0, resultSetId: $"{documentId}-0-0");
            }

            await Task.CompletedTask;
        }
    }

    private sealed class RecordingDispatcher : IUiDispatcher
    {
        public int InvocationCount { get; private set; }

        public bool CheckAccess() => false;

        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InvocationCount++;
            action();
            return Task.CompletedTask;
        }
    }
}
