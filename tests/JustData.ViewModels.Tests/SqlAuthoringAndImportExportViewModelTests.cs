using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.ImportExport;
using JustData.Application.Sql;
using JustData.ViewModels.ImportExport;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

public sealed class SqlAuthoringAndImportExportViewModelTests
{
    [Fact]
    public async Task Superseded_lint_results_are_not_published()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeAuthoringUseCase(blockFirstLint: true);
        using var viewModel = new SqlAuthoringViewModel(documentId, useCase);

        Task first = viewModel.ScheduleLintAsync("first", debounce: TimeSpan.Zero);
        await useCase.FirstLintStarted.Task;
        Task second = viewModel.ScheduleLintAsync("second", debounce: TimeSpan.Zero);

        useCase.ReleaseFirstLint.TrySetResult();
        await Task.WhenAll(first, second);

        Assert.Single(viewModel.Diagnostics);
        Assert.Equal("second", viewModel.Diagnostics[0].Message);
    }

    [Fact]
    public async Task Lint_on_save_and_authoring_requests_keep_the_document_id()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeAuthoringUseCase(blockFirstLint: false);
        using var viewModel = new SqlAuthoringViewModel(documentId, useCase);

        await viewModel.LintOnSaveAsync("saved");
        SqlLintResult result = await viewModel.LintNowAsync("saved");
        IReadOnlyList<SqlCompletionItem> completions = await viewModel.CompleteAsync("sel", 3);
        SqlSignatureHelp? signature = await viewModel.GetSignatureHelpAsync("count(", 6);

        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(documentId, useCase.LastCompletionRequest?.DocumentId);
        Assert.Equal(documentId, useCase.LastSignatureRequest?.DocumentId);
        Assert.NotEmpty(completions);
        Assert.NotNull(signature);
    }

    [Fact]
    public async Task Disabled_lint_on_save_skips_parser_and_disposal_releases_document()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeAuthoringUseCase(blockFirstLint: false);
        var viewModel = new SqlAuthoringViewModel(documentId, useCase)
        {
            LintOnSave = false
        };

        await viewModel.LintOnSaveAsync("not linted");
        viewModel.Dispose();

        Assert.Equal(0, useCase.LintCalls);
        Assert.Equal(documentId, useCase.ReleasedDocumentId);
    }

    [Fact]
    public async Task Immediate_lint_clears_busy_state_after_success_and_failure()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new FakeAuthoringUseCase(blockFirstLint: false);
        using var viewModel = new SqlAuthoringViewModel(documentId, useCase);

        await viewModel.LintNowAsync("select 1");
        Assert.False(viewModel.IsLinting);

        useCase.ThrowOnLint = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.LintNowAsync("broken"));
        Assert.False(viewModel.IsLinting);
    }

    [Fact]
    public async Task Import_export_view_model_reports_progress_and_cleans_up_cancellation()
    {
        var import = new FakeImportUseCase();
        var export = new FakeExportUseCase();
        using var viewModel = new ImportExportViewModel(import, export);
        var request = new ImportRequest(null, "file.csv", ImportFormat.Csv);
        viewModel.CurrentImportRequest = request;

        ImportResult? result = await viewModel.ImportAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, viewModel.RowsRead);
        Assert.False(viewModel.IsRunning);
        Assert.Equal(1, import.DisposeCount);

        var exportRequest = new ExportRequest(EditorDocumentId.New(), "out.csv", ExportFormat.Csv);
        await viewModel.ExportAsync(exportRequest);

        Assert.Equal(4, viewModel.RowsWritten);
        Assert.False(viewModel.IsRunning);
        Assert.True(export.Completed);
    }

    [Fact]
    public async Task Import_cancellation_cleans_up_and_allows_restart()
    {
        var import = new FakeImportUseCase { Block = true };
        using var viewModel = new ImportExportViewModel(import);
        var request = new ImportRequest(null, "file.csv", ImportFormat.Csv);

        Task<ImportResult?> first = viewModel.ImportAsync(request);
        await import.Started.Task;
        await viewModel.CancelAsync();

        Assert.Null(await first);
        Assert.Equal("Import cancelled.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsRunning);
        Assert.Equal(1, import.DisposeCount);

        import.Block = false;
        Assert.NotNull(await viewModel.ImportAsync(request));
        Assert.Equal(2, import.DisposeCount);
    }

    [Fact]
    public async Task Import_command_is_disabled_while_running_and_reenabled_after_cancellation()
    {
        var import = new FakeImportUseCase { Block = true };
        using var viewModel = new ImportExportViewModel(import);
        viewModel.CurrentImportRequest = new ImportRequest(null, "file.csv", ImportFormat.Csv);

        Task running = viewModel.ImportCommand.ExecuteAsync(null);
        await import.Started.Task;

        Assert.False(viewModel.ImportCommand.CanExecute(null));
        Assert.True(viewModel.CancelCommand.CanExecute(null));

        await viewModel.CancelAsync();
        await running;

        Assert.False(viewModel.IsRunning);
        Assert.True(viewModel.ImportCommand.CanExecute(null));
        Assert.False(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task Import_progress_and_lifecycle_are_applied_through_the_ui_dispatcher()
    {
        var import = new FakeImportUseCase();
        var dispatcher = new RecordingDispatcher();
        using var viewModel = new ImportExportViewModel(import, uiDispatcher: dispatcher);

        ImportResult? result = await viewModel.ImportAsync(
            new ImportRequest(null, "file.csv", ImportFormat.Csv));

        Assert.NotNull(result);
        Assert.False(viewModel.IsRunning);
        Assert.True(dispatcher.InvocationCount >= 4);
    }

    [Fact]
    public async Task Disposing_active_import_cancels_and_allows_enumerator_cleanup()
    {
        var import = new FakeImportUseCase { Block = true };
        var viewModel = new ImportExportViewModel(import);
        Task<ImportResult?> running = viewModel.ImportAsync(
            new ImportRequest(null, "file.csv", ImportFormat.Csv));
        await import.Started.Task;

        viewModel.Dispose();

        Assert.Null(await running);
        Assert.Equal("Import cancelled.", viewModel.ErrorMessage);
        Assert.Equal(1, import.DisposeCount);
    }

    [Fact]
    public async Task Import_preserves_partial_errors_and_rejects_parallel_operation()
    {
        var import = new FakeImportUseCase { Block = true };
        using var viewModel = new ImportExportViewModel(import);
        var request = new ImportRequest(null, "file.csv", ImportFormat.Csv);

        Task<ImportResult?> running = viewModel.ImportAsync(request);
        await import.Started.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.ImportAsync(request));
        await viewModel.CancelAsync();
        await running;

        import.Block = false;
        import.ReturnPartialError = true;
        ImportResult? partial = await viewModel.ImportAsync(request);

        Assert.NotNull(partial);
        Assert.True(partial.IsPartial);
        Assert.Equal("row 2 failed", viewModel.ErrorMessage);
        Assert.Contains("row 2 failed", partial.Errors);
    }

    [Fact]
    public async Task Import_and_export_exceptions_are_redacted_and_do_not_poison_restart()
    {
        var import = new ThrowOnceImportUseCase();
        var export = new ThrowOnceExportUseCase();
        using var viewModel = new ImportExportViewModel(import, export);
        var importRequest = new ImportRequest(null, "file.csv", ImportFormat.Csv);

        Assert.Null(await viewModel.ImportAsync(importRequest));
        Assert.DoesNotContain("import secret", viewModel.ErrorMessage, StringComparison.Ordinal);
        Assert.False(viewModel.IsRunning);
        Assert.NotNull(await viewModel.ImportAsync(importRequest));

        var exportRequest = new ExportRequest(EditorDocumentId.New(), "out.csv", ExportFormat.Csv);
        await viewModel.ExportAsync(exportRequest);
        Assert.DoesNotContain("export secret", viewModel.ErrorMessage, StringComparison.Ordinal);
        Assert.False(viewModel.IsRunning);
        await viewModel.ExportAsync(exportRequest);
        Assert.Null(viewModel.ErrorMessage);
        Assert.Equal(1, viewModel.RowsWritten);
    }

    private sealed class FakeAuthoringUseCase : ISqlAuthoringUseCase
    {
        private readonly bool _blockFirstLint;
        private int _lintCalls;

        public FakeAuthoringUseCase(bool blockFirstLint)
        {
            _blockFirstLint = blockFirstLint;
        }

        public TaskCompletionSource FirstLintStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirstLint { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public SqlCompletionRequest? LastCompletionRequest { get; private set; }
        public SqlSignatureHelpRequest? LastSignatureRequest { get; private set; }
        public int LintCalls => Volatile.Read(ref _lintCalls);
        public bool ThrowOnLint { get; set; }
        public EditorDocumentId? ReleasedDocumentId { get; private set; }

        public async Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default)
        {
            int call = Interlocked.Increment(ref _lintCalls);
            if (ThrowOnLint)
                throw new InvalidOperationException("lint failed");
            if (_blockFirstLint && call == 1)
            {
                FirstLintStarted.TrySetResult();
                await ReleaseFirstLint.Task.WaitAsync(cancellationToken);
            }

            return new SqlLintResult(request.DocumentId, [new SqlDiagnostic(
                SqlDiagnosticSeverity.Information,
                request.SqlText)]);
        }

        public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default)
        {
            LastCompletionRequest = request;
            return Task.FromResult<IReadOnlyList<SqlCompletionItem>>([new("select", "select")]);
        }

        public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default)
        {
            LastSignatureRequest = request;
            return Task.FromResult<SqlSignatureHelp?>(new SqlSignatureHelp([
                new SqlSignatureInformation("count(expression)")
            ]));
        }

        public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(SqlCodeActionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SqlCodeAction>>([]);

        public void DisableRule(string ruleId) { }
        public void EnableRule(string ruleId) { }
        public void Release(EditorDocumentId documentId) => ReleasedDocumentId = documentId;
    }

    private sealed class FakeImportUseCase : IImportUseCase
    {
        public int DisposeCount { get; private set; }
        public bool Block { get; set; }
        public bool ReturnPartialError { get; set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ImportPreview> PreviewAsync(ImportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImportPreview(request.SourcePath, request.Format, [], [], 2));

        public async IAsyncEnumerable<ImportProgress> ImportAsync(
            ImportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                yield return new ImportProgress("reading", RowsRead: 1);
                Started.TrySetResult();
                if (Block)
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                await Task.Yield();
                if (ReturnPartialError)
                {
                    var partial = new ImportResult(2, 1, 0, ["row 2 failed"], IsPartial: true);
                    yield return new ImportProgress(
                        "failed",
                        RowsRead: 2,
                        RowsImported: 1,
                        IsCompleted: true,
                        Result: partial,
                        ErrorMessage: "row 2 failed");
                    yield break;
                }
                yield return new ImportProgress(
                    "completed",
                    RowsRead: 2,
                    RowsImported: 2,
                    IsCompleted: true,
                    Result: new ImportResult(2, 2, 0, []));
            }
            finally
            {
                DisposeCount++;
            }
        }
    }

    private sealed class FakeExportUseCase : IResultExportUseCase
    {
        public bool Completed { get; private set; }

        public async IAsyncEnumerable<ExportProgress> ExportAsync(
            ExportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ExportProgress("writing", 2);
            await Task.Yield();
            Completed = true;
            yield return new ExportProgress("completed", 4, IsCompleted: true);
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

    private sealed class ThrowOnceImportUseCase : IImportUseCase
    {
        private int _calls;

        public Task<ImportPreview> PreviewAsync(ImportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImportPreview(request.SourcePath, request.Format, [], [], 0));

        public async IAsyncEnumerable<ImportProgress> ImportAsync(
            ImportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
                throw new InvalidOperationException("password='import secret'");
            yield return new ImportProgress(
                "completed",
                RowsRead: 1,
                RowsImported: 1,
                IsCompleted: true,
                Result: new ImportResult(1, 1, 0, []));
            await Task.CompletedTask;
        }
    }

    private sealed class ThrowOnceExportUseCase : IResultExportUseCase
    {
        private int _calls;

        public async IAsyncEnumerable<ExportProgress> ExportAsync(
            ExportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
                throw new InvalidOperationException("token='export secret'");
            yield return new ExportProgress("completed", 1, IsCompleted: true);
            await Task.CompletedTask;
        }
    }
}
