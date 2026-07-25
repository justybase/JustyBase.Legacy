using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

public sealed class SqlExecutionViewModelLifecycleTests
{
    [Fact]
    public void Disposing_idle_execution_does_not_throw()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
    }

    [Fact]
    public void Dispose_can_be_called_multiple_times()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        viewModel.Dispose();
    }

    [Fact]
    public void Dispose_clears_event_received_backing_field()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        var field = typeof(SqlExecutionViewModel)
            .GetField("EventReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);

        viewModel.Dispose();
        var value = field.GetValue(viewModel);
        Assert.Null(value);
    }

    [Fact]
    public async Task Disposed_instance_throws_on_RunAsync()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => viewModel.RunAsync(request));
    }

    [Fact]
    public async Task RunAsync_marshals_state_changes_through_the_ui_dispatcher()
    {
        var documentId = EditorDocumentId.New();
        var dispatcher = new RecordingDispatcher();
        using var viewModel = new SqlExecutionViewModel(
            documentId,
            (mode, outputMode, outputPath) => new SqlExecutionRequest(
                documentId,
                "select 1")
            {
                Mode = mode,
                OutputMode = outputMode,
                OutputPath = outputPath
            },
            new FakeExecutionUseCase(),
            dispatcher);

        // The request factory above must preserve the owning document id.
        var request = new SqlExecutionRequest(documentId, "select 1");
        SqlExecutionOutcome outcome = await viewModel.RunAsync(request);

        Assert.Equal(SqlExecutionOutcome.Success, outcome);
        Assert.Equal(SqlExecutionState.Succeeded, viewModel.State);
        Assert.True(dispatcher.InvocationCount >= 3);
    }

    [Fact]
    public void Disposed_instance_throws_on_StopAsync()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        Action call = () => { var _ = viewModel.StopAsync(); };
        Assert.Throws<ObjectDisposedException>(call);
    }

    [Fact]
    public void Disposed_instance_throws_on_SelectResult()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        Assert.Throws<ObjectDisposedException>(() => viewModel.SelectResult("result-1"));
    }

    [Fact]
    public void Disposed_instance_throws_on_PinResult()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        Assert.Throws<ObjectDisposedException>(() => viewModel.PinResult("result-1"));
    }

    [Fact]
    public void Disposed_instance_throws_on_ClearResults()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        Assert.Throws<ObjectDisposedException>(() => viewModel.ClearResults());
    }

    [Fact]
    public void Disposed_instance_throws_on_RemoveResult()
    {
        var viewModel = new SqlExecutionViewModel(EditorDocumentId.New(),
            new FakeExecutionUseCase());
        viewModel.Dispose();
        Assert.Throws<ObjectDisposedException>(() => viewModel.RemoveResult("result-1"));
    }

    private sealed class FakeExecutionUseCase : ISqlExecutionUseCase
    {
        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    private sealed class RecordingDispatcher : JustData.Application.IUiDispatcher
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
