using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

public sealed class SqlExecutionViewModelTests
{
    [Fact]
    public void Sensitive_values_are_not_exposed_in_execution_errors()
    {
        string safe = SqlSensitiveDataRedactor.Redact(
            "provider failed; password='super secret'; token=\"abc 123\"; pwd=plain");

        Assert.DoesNotContain("super secret", safe, StringComparison.Ordinal);
        Assert.DoesNotContain("abc 123", safe, StringComparison.Ordinal);
        Assert.DoesNotContain("plain", safe, StringComparison.Ordinal);
        Assert.Contains("[redacted]", safe, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execution_tracks_results_rows_and_pinned_sets_without_copying_batches()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeExecutionUseCase((request, cancellationToken) =>
            Events(request.DocumentId));
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);
        var request = new SqlExecutionRequest(documentId, "select 1");

        SqlExecutionOutcome outcome = await viewModel.RunAsync(request);

        Assert.Equal(SqlExecutionOutcome.Success, outcome);
        Assert.Equal(SqlExecutionState.Succeeded, viewModel.State);
        Assert.Equal(2, viewModel.ResultSets.Count);
        Assert.Equal(2, viewModel.RowCount);
        Assert.Equal(3, viewModel.AffectedRows);
        Assert.Same(viewModel.ResultSets[0], viewModel.SelectedResultSet);

        viewModel.PinResult("result-1");
        viewModel.ClearResults();

        Assert.Single(viewModel.ResultSets);
        Assert.True(viewModel.ResultSets[0].IsPinned);

        viewModel.RemoveResult("result-1");
        Assert.Empty(viewModel.ResultSets);
        Assert.Null(viewModel.SelectedResultSet);
    }

    [Fact]
    public async Task Row_count_events_do_not_require_a_second_row_batch()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeExecutionUseCase((request, cancellationToken) =>
            CountOnlyEvents(request.DocumentId));
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);

        Assert.Equal(SqlExecutionOutcome.Success, await viewModel.RunAsync(
            new SqlExecutionRequest(documentId, "select 1")));
        Assert.Equal(1_000_000, viewModel.RowCount);
    }

    [Fact]
    public async Task Cancellation_returns_idle_cancelled_and_allows_restart()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new BlockingExecutionUseCase();
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);
        var request = new SqlExecutionRequest(documentId, "select 1");

        Task<SqlExecutionOutcome> first = viewModel.RunAsync(request);
        await useCase.Started.Task;
        Assert.True(await viewModel.StopAsync());

        Assert.Equal(SqlExecutionOutcome.Cancelled, await first);
        Assert.Equal(SqlExecutionState.Idle, viewModel.State);

        useCase.Block = false;
        Assert.Equal(SqlExecutionOutcome.Success, await viewModel.RunAsync(request));
        Assert.Equal(SqlExecutionState.Succeeded, viewModel.State);
    }

    [Fact]
    public async Task Parallel_execution_is_rejected_for_one_document()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new BlockingExecutionUseCase();
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);
        var request = new SqlExecutionRequest(documentId, "select 1");
        Task<SqlExecutionOutcome> first = viewModel.RunAsync(request);
        await useCase.Started.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.RunAsync(request));
        await viewModel.StopAsync();
        await first;
    }

    [Fact]
    public async Task Failed_execution_keeps_failed_state_and_can_restart()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SequencedExecutionUseCase(
            SqlExecutionOutcome.Failed,
            SqlExecutionOutcome.Success);
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);
        var request = new SqlExecutionRequest(documentId, "select 1");

        Assert.Equal(SqlExecutionOutcome.Failed, await viewModel.RunAsync(request));
        Assert.Equal(SqlExecutionState.Failed, viewModel.State);
        Assert.Equal(SqlExecutionOutcome.Success, await viewModel.RunAsync(request));
        Assert.Equal(SqlExecutionState.Succeeded, viewModel.State);
    }

    [Fact]
    public async Task Stream_without_completion_is_failed_instead_of_reported_as_success()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeExecutionUseCase((request, cancellationToken) =>
            MissingCompletionEvents(request.DocumentId));
        using var viewModel = new SqlExecutionViewModel(documentId, useCase);

        Assert.Equal(SqlExecutionOutcome.Failed, await viewModel.RunAsync(
            new SqlExecutionRequest(documentId, "select 1")));
        Assert.Equal(SqlExecutionState.Failed, viewModel.State);
        Assert.Contains(viewModel.Diagnostics, diagnostic => diagnostic.Code == "MissingCompletion");
    }

    [Fact]
    public async Task Blocked_execution_returns_to_idle_without_becoming_successful()
    {
        var documentId = EditorDocumentId.New();
        using var viewModel = new SqlExecutionViewModel(
            documentId,
            new SequencedExecutionUseCase(SqlExecutionOutcome.Blocked));
        var request = new SqlExecutionRequest(documentId, "select 1");

        Assert.Equal(SqlExecutionOutcome.Blocked, await viewModel.RunAsync(request));
        Assert.Equal(SqlExecutionState.Idle, viewModel.State);
        Assert.Equal(SqlExecutionOutcome.Blocked, viewModel.LastOutcome);
    }

    [Fact]
    public async Task Disposing_an_active_execution_cancels_provider_enumeration()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new BlockingExecutionUseCase();
        var viewModel = new SqlExecutionViewModel(documentId, useCase);
        var request = new SqlExecutionRequest(documentId, "select 1");
        Task<SqlExecutionOutcome> execution = viewModel.RunAsync(request);
        await useCase.Started.Task;

        viewModel.Dispose();

        Assert.Equal(SqlExecutionOutcome.Cancelled, await execution);
        Assert.True(useCase.CancellationObserved);
    }

    private static async IAsyncEnumerable<SqlExecutionEvent> Events(
        EditorDocumentId documentId)
    {
        yield return SqlExecutionEvent.Started(documentId, 2);
        yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
            "result-1",
            "Result 1",
            [new ResultColumnDescriptor(0, "value", "INTEGER")]));
        yield return SqlExecutionEvent.RowsBatch(documentId, [[1]]);
        yield return new SqlExecutionEvent(SqlExecutionEventKind.AffectedRows, documentId)
        {
            AffectedRows = 3
        };
        yield return SqlExecutionEvent.Result(documentId, new ResultSetDescriptor(
            "result-2",
            "Result 2",
            [new ResultColumnDescriptor(0, "value", "INTEGER")]));
        yield return SqlExecutionEvent.RowsBatch(documentId, [[2]]);
        yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<SqlExecutionEvent> CountOnlyEvents(
        EditorDocumentId documentId)
    {
        yield return SqlExecutionEvent.RowsObserved(documentId, 1_000_000, resultSetId: "result-1");
        yield return SqlExecutionEvent.Completed(documentId, SqlExecutionOutcome.Success);
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<SqlExecutionEvent> MissingCompletionEvents(
        EditorDocumentId documentId)
    {
        yield return SqlExecutionEvent.Started(documentId);
        await Task.CompletedTask;
    }

    private sealed class FakeExecutionUseCase(
        Func<SqlExecutionRequest, CancellationToken, IAsyncEnumerable<SqlExecutionEvent>> factory)
        : ISqlExecutionUseCase
    {
        public IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            CancellationToken cancellationToken = default) => factory(request, cancellationToken);
    }

    private sealed class BlockingExecutionUseCase : ISqlExecutionUseCase
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Block { get; set; } = true;
        public bool CancellationObserved { get; private set; }

        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            yield return SqlExecutionEvent.Started(request.DocumentId);
            if (Block)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    CancellationObserved = true;
                    throw;
                }
            }
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Success);
        }
    }

    private sealed class SequencedExecutionUseCase(params SqlExecutionOutcome[] outcomes) : ISqlExecutionUseCase
    {
        private int _index;

        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int index = Math.Min(Interlocked.Increment(ref _index) - 1, outcomes.Length - 1);
            yield return SqlExecutionEvent.Completed(request.DocumentId, outcomes[index]);
            await Task.CompletedTask;
        }
    }
}
