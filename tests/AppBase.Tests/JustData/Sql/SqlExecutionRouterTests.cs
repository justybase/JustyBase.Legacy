using AppBase.Data.Core.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.UI.Sql;
using NSubstitute;
using System.Data;

namespace AppBase.Tests.JustData.Sql;

public sealed class SqlExecutionRouterTests
{
    [Theory]
    [InlineData("NetezzaSQL", SqlExecutionMode.Selection, SqlOutputMode.Grid)]
    [InlineData("Postgres", SqlExecutionMode.RunToCursor, SqlOutputMode.Csv)]
    [InlineData("Oracle", SqlExecutionMode.SingleBatch, SqlOutputMode.Xlsx)]
    [InlineData("NetezzaSQL", SqlExecutionMode.Script, SqlOutputMode.Xlsb)]
    public async Task Routes_named_modes_to_the_matching_engine_unchanged(
        string driver,
        SqlExecutionMode mode,
        SqlOutputMode outputMode)
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Success);
        SqlExecutionRouter router = CreateRouter(driver, presenter);
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
        {
            ConnectionName = "test_nz_connection",
            Mode = mode,
            OutputMode = outputMode,
            KeepConnectionOpen = true
        };

        SqlExecutionEvent[] events = await CollectAsync(router.ExecuteAsync(request));

        Assert.Equal(mode, presenter.Request!.Mode);
        Assert.Equal(outputMode, presenter.Request.OutputMode);
        Assert.True(presenter.Request.KeepConnectionOpen);
        Assert.Equal(SqlExecutionOutcome.Success, events.Last().Outcome);
    }

    [Fact]
    public async Task Netezza_route_preserves_explain_and_export_path()
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Success);
        SqlExecutionRouter router = CreateRouter("NetezzaSQL", presenter);
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
        {
            ConnectionName = "Production",
            Explain = true,
            OutputPath = "result.xlsx"
        };

        await CollectAsync(router.ExecuteAsync(request));

        Assert.True(presenter.Request!.Explain);
        Assert.Equal("result.xlsx", presenter.Request.OutputPath);
    }

    [Fact]
    public async Task Netezza_normal_document_execution_uses_the_provider_backend()
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Success);
        var context = new SqlExecutionEngineContext();
        context.AttachPresenter(presenter);
        var engine = new NetezzaSqlExecutionEngine(
            new SqlExecutionSessionRegistry(),
            exportTasks: null,
            context);

        SqlExecutionEvent[] events = await CollectAsync(engine.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
            {
                ConnectionName = $"missing-{Guid.NewGuid():N}"
            }));

        Assert.Equal(SqlExecutionOutcome.Blocked, Assert.Single(events).Outcome);
        Assert.Contains("connection", events[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(presenter.Request);
    }

    [Fact]
    public async Task Netezza_legacy_connection_mode_keeps_the_compatibility_backend()
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Success);
        var context = new SqlExecutionEngineContext();
        context.AttachPresenter(presenter);
        var engine = new NetezzaSqlExecutionEngine(
            new SqlExecutionSessionRegistry(),
            exportTasks: null,
            context);

        SqlExecutionEvent[] events = await CollectAsync(engine.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
            {
                ConnectionName = "test_nz_connection",
                KeepConnectionOpen = true
            }));

        Assert.Equal(SqlExecutionOutcome.Success, Assert.Single(events).Outcome);
        Assert.NotNull(presenter.Request);
    }

    [Fact]
    public async Task Engine_failure_is_not_overwritten_by_success_and_sql_is_redacted()
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Failed);
        SqlExecutionRouter router = CreateRouter("Postgres", presenter);

        SqlExecutionEvent[] events = await CollectAsync(router.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "password=top-secret") { ConnectionName = "test_nz_connection" }));

        Assert.Single(events, item => item.Kind == SqlExecutionEventKind.Completed);
        Assert.Equal(SqlExecutionOutcome.Failed, events[^1].Outcome);
        Assert.DoesNotContain("top-secret", events.Single(item => item.Kind == SqlExecutionEventKind.StatementStarted).StatementText);
    }

    [Fact]
    public async Task Missing_terminal_event_is_reported_as_failure()
    {
        var presenter = new RecordingPresenter(outcome: null);
        SqlExecutionRouter router = CreateRouter("SQLite", presenter);

        SqlExecutionEvent[] events = await CollectAsync(router.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1") { ConnectionName = "test_nz_connection" }));

        Assert.Equal(SqlExecutionOutcome.Failed, events[^1].Outcome);
        Assert.Contains("without a completion", events[^1].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancellation_reaches_the_document_presenter()
    {
        var presenter = new RecordingPresenter(outcome: null, block: true);
        SqlExecutionRouter router = CreateRouter("NetezzaSQL", presenter);
        using var cancellation = new CancellationTokenSource();
        Task<SqlExecutionEvent[]> execution = CollectAsync(router.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1") { ConnectionName = "test_nz_connection" },
            cancellation.Token));
        await presenter.Started.Task;

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.True(presenter.Cancelled);
        Assert.True(presenter.CancellationObserved);
        Assert.True(presenter.EnumeratorDisposed);
    }

    [Fact]
    public async Task Unsupported_driver_is_blocked_without_invoking_the_presenter()
    {
        var presenter = new RecordingPresenter(SqlExecutionOutcome.Success);
        SqlExecutionRouter router = CreateRouter("Unsupported", presenter);

        SqlExecutionEvent[] events = await CollectAsync(router.ExecuteAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1") { ConnectionName = "test_nz_connection" }));

        Assert.Equal(SqlExecutionOutcome.Blocked, Assert.Single(events).Outcome);
        Assert.Null(presenter.Request);
    }

    [Theory]
    [InlineData(SqlExecutionMode.Selection, SqlOutputMode.Grid, 2)]
    [InlineData(SqlExecutionMode.RunToCursor, SqlOutputMode.LogOnly, 2)]
    [InlineData(SqlExecutionMode.SingleBatch, SqlOutputMode.Grid, 1)]
    [InlineData(SqlExecutionMode.Selection, SqlOutputMode.Csv, 1)]
    [InlineData(SqlExecutionMode.Script, SqlOutputMode.Xlsx, 1)]
    [InlineData(SqlExecutionMode.Script, SqlOutputMode.Grid, 2)]
    public void General_engine_preserves_legacy_batching_rules(
        SqlExecutionMode mode,
        SqlOutputMode outputMode,
        int expectedBatchCount)
    {
        IReadOnlyList<string> batches = GeneralSqlExecutionEngine.BuildBatches(
            "select 1; select 2;",
            mode,
            outputMode);

        Assert.Equal(expectedBatchCount, batches.Count);
    }

    [Fact]
    public async Task General_engine_streams_rows_without_retaining_a_second_result_set()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Rows.Add(1);
        table.Rows.Add(2);
        table.Rows.Add(3);
        using DataTableReader reader = table.CreateDataReader();
        var observed = new List<int>();

        long count = await GeneralSqlExecutionEngine.StreamRowsAsync(reader, (row, rowNumber) =>
        {
            observed.Add((int)row[0]!);
            return rowNumber < 2;
        });

        Assert.Equal(2, count);
        Assert.Equal([1, 2], observed);
    }

    private static SqlExecutionRouter CreateRouter(string driver, RecordingPresenter presenter)
    {
        var database = Substitute.For<IGeneralDbService>();
        database.DriverName(Arg.Any<string>()).Returns(driver);
        var context = new SqlExecutionEngineContext();
        context.AttachPresenter(presenter);
        return new SqlExecutionRouter(
            database,
            [new GeneralSqlExecutionEngine(context), new NetezzaSqlExecutionEngine(context)],
            context);
    }

    private static async Task<SqlExecutionEvent[]> CollectAsync(
        IAsyncEnumerable<SqlExecutionEvent> source,
        CancellationToken cancellationToken = default)
    {
        var events = new List<SqlExecutionEvent>();
        await foreach (SqlExecutionEvent item in source.WithCancellation(cancellationToken))
            events.Add(item);
        return events.ToArray();
    }

    private sealed class RecordingPresenter(SqlExecutionOutcome? outcome, bool block = false)
        : ISqlExecutionDocumentPresenter
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled { get; private set; }
        public bool CancellationObserved { get; private set; }
        public bool EnumeratorDisposed { get; private set; }
        public SqlExecutionRequest? Request { get; private set; }

        public void Cancel(EditorDocumentId documentId, string connectionName) => Cancelled = true;

        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                Request = request;
                Started.TrySetResult();
                if (block)
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

                if (outcome is { } terminalOutcome)
                    yield return SqlExecutionEvent.Completed(request.DocumentId, terminalOutcome);
            }
            finally
            {
                EnumeratorDisposed = true;
            }
        }
    }
}
