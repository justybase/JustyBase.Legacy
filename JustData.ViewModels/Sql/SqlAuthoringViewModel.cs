using CommunityToolkit.Mvvm.ComponentModel;

using JustData.Application;

using JustData.Application.Editor;

using JustData.Application.Sql;

using JustyBase.NetezzaSqlParser.Authoring;

using System.Collections.ObjectModel;



namespace JustData.ViewModels.Sql;



public sealed class SqlAuthoringViewModel : ObservableObject, IDisposable

{

    private readonly EditorDocumentId _documentId;

    private readonly ISqlAuthoringUseCase _useCase;

    private readonly IUiDispatcher? _uiDispatcher;

    private readonly object _sync = new();

    private CancellationTokenSource? _lintCancellation;

    private long _lintVersion;

    private bool _isLinting;

    private bool _lintOnSave = true;

    private bool _disposed;



    public SqlAuthoringViewModel(

        EditorDocumentId documentId,

        ISqlAuthoringUseCase? useCase = null,

        IUiDispatcher? uiDispatcher = null)

    {

        _documentId = documentId;

        _useCase = useCase ?? EmptySqlAuthoringUseCase.Instance;

        _uiDispatcher = uiDispatcher;

    }



    public ObservableCollection<SqlDiagnostic> Diagnostics { get; } = [];

    public ObservableCollection<string> DisabledRules { get; } = [];



    public event Action<IReadOnlyList<SqlDiagnostic>>? DiagnosticsChanged;



    public bool IsLinting

    {

        get => _isLinting;

        private set => SetProperty(ref _isLinting, value);

    }



    public bool LintOnSave

    {

        get => _lintOnSave;

        set => SetProperty(ref _lintOnSave, value);

    }



    public async Task ScheduleLintAsync(
        string sqlText,
        string connectionName = "",
        TimeSpan? debounce = null,
        int knownLineCount = -1)
    {
        ThrowIfDisposed();
        sqlText ??= string.Empty;
        int lineCount = SqlPerformancePolicy.ResolveLineCountForLintGate(sqlText, knownLineCount);
        CancellationTokenSource cancellation = RenewLintCancellation();
        long version = Interlocked.Increment(ref _lintVersion);
        if (SqlPerformancePolicy.ShouldSkipLint(SqlLintInvocation.Live, lineCount, sqlText.Length))
        {
            if (Diagnostics.Count == 0)
            {
                await FinishLintAsync(version, cancellation);
                return;
            }
            try
            {
                await PublishEmptyDiagnosticsAsync(version, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            finally
            {
                await FinishLintAsync(version, cancellation);
            }

            return;
        }

        try
        {
            TimeSpan delay = debounce ?? TimeSpan.FromMilliseconds(
                SqlPerformancePolicy.GetLintDebounceMs(sqlText, knownLineCount));
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellation.Token);

            await RunLintAsync(
                sqlText,
                connectionName,
                version,
                cancellation.Token,
                knownLineCount: lineCount,
                invocation: SqlLintInvocation.Live);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            await FinishLintAsync(version, cancellation);
        }
    }



    public Task LintOnSaveAsync(string sqlText, string connectionName = "", int knownLineCount = -1) =>

        LintOnSave

            ? LintNowAsync(sqlText, connectionName, SqlLintInvocation.Save, knownLineCount)

            : Task.CompletedTask;



    public async Task<SqlLintResult> LintNowAsync(

        string sqlText,

        string connectionName = "",

        SqlLintInvocation invocation = SqlLintInvocation.Manual,

        int knownLineCount = -1)

    {

        ThrowIfDisposed();

        CancellationTokenSource cancellation = RenewLintCancellation();

        long version = Interlocked.Increment(ref _lintVersion);

        try

        {

            return await RunLintAsync(

                sqlText,

                connectionName,

                version,

                cancellation.Token,

                knownLineCount,

                invocation);

        }

        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)

        {

            return new SqlLintResult(_documentId, Diagnostics.ToArray(), version);

        }

        finally

        {

            await FinishLintAsync(version, cancellation);

        }

    }



    public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(

        string sqlText,

        int caretOffset,

        string connectionName = "",

        CancellationToken cancellationToken = default)

    {

        ThrowIfDisposed();

        return _useCase.CompleteAsync(

            new SqlCompletionRequest(_documentId, sqlText ?? string.Empty, caretOffset, connectionName),

            cancellationToken);

    }



    public Task<SqlSignatureHelp?> GetSignatureHelpAsync(

        string sqlText,

        int caretOffset,

        string connectionName = "",

        CancellationToken cancellationToken = default)

    {

        ThrowIfDisposed();

        return _useCase.GetSignatureHelpAsync(

            new SqlSignatureHelpRequest(_documentId, sqlText ?? string.Empty, caretOffset, connectionName),

            cancellationToken);

    }



    public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(

        string sqlText,

        SqlDiagnostic diagnostic,

        string connectionName = "",

        CancellationToken cancellationToken = default)

    {

        ThrowIfDisposed();

        return _useCase.GetCodeActionsAsync(

            new SqlCodeActionRequest(_documentId, sqlText ?? string.Empty, diagnostic, connectionName),

            cancellationToken);

    }



    public void DisableRule(string ruleId)

    {

        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(ruleId))

            return;

        if (!DisabledRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))

            DisabledRules.Add(ruleId);

        _useCase.DisableRule(ruleId);

    }



    public void EnableRule(string ruleId)

    {

        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(ruleId))

            return;

        for (int index = DisabledRules.Count - 1; index >= 0; index--)

        {

            if (string.Equals(DisabledRules[index], ruleId, StringComparison.OrdinalIgnoreCase))

                DisabledRules.RemoveAt(index);

        }

        _useCase.EnableRule(ruleId);

    }



    private async Task PublishEmptyDiagnosticsAsync(long version, CancellationToken cancellationToken)

    {

        if (!IsCurrent(version))

            return;



        await _uiDispatcher.InvokeOnUiAsync(() =>

        {

            if (!IsCurrent(version))

                return;

            Diagnostics.Clear();

            DiagnosticsChanged?.Invoke(Diagnostics.ToArray());

        }, cancellationToken);

    }



    private async Task<SqlLintResult> RunLintAsync(

        string sqlText,

        string connectionName,

        long version,

        CancellationToken cancellationToken,

        int knownLineCount = -1,

        SqlLintInvocation invocation = SqlLintInvocation.Live)

    {

        await _uiDispatcher.InvokeOnUiAsync(

            () => IsLinting = true,

            cancellationToken);

        SqlLintResult result = await _useCase.LintAsync(

            new SqlLintRequest(

                _documentId,

                sqlText ?? string.Empty,

                connectionName,

                KnownLineCount: knownLineCount,

                Invocation: invocation),

            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsCurrent(version) || result.DocumentId != _documentId)

            return result;



        await _uiDispatcher.InvokeOnUiAsync(() =>

        {

            Diagnostics.Clear();

            foreach (SqlDiagnostic diagnostic in result.Diagnostics)

                Diagnostics.Add(diagnostic);

            DiagnosticsChanged?.Invoke(Diagnostics.ToArray());

        }, cancellationToken);

        return result;

    }



    private CancellationTokenSource RenewLintCancellation()

    {

        lock (_sync)

        {

            _lintCancellation?.Cancel();

            _lintCancellation = new CancellationTokenSource();

            return _lintCancellation;

        }

    }



    private async Task FinishLintAsync(long version, CancellationTokenSource cancellation)

    {

        lock (_sync)

        {

            if (ReferenceEquals(_lintCancellation, cancellation))

                _lintCancellation = null;

        }

        cancellation.Dispose();

        if (IsCurrent(version))

        {

            await _uiDispatcher.InvokeOnUiAsync(

                () => IsLinting = false,

                CancellationToken.None);

        }

    }



    private bool IsCurrent(long version) => version == Volatile.Read(ref _lintVersion);



    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);



    public void Dispose()

    {

        if (_disposed)

            return;

        _disposed = true;

        lock (_sync)

        {

            _lintCancellation?.Cancel();

            _lintCancellation?.Dispose();

            _lintCancellation = null;

        }

        _useCase.Release(_documentId);

        DiagnosticsChanged = null;

    }



    private sealed class EmptySqlAuthoringUseCase : ISqlAuthoringUseCase

    {

        public static EmptySqlAuthoringUseCase Instance { get; } = new();



        public Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default) =>

            Task.FromResult(new SqlLintResult(request.DocumentId, []));



        public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default) =>

            Task.FromResult<IReadOnlyList<SqlCompletionItem>>([]);



        public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default) =>

            Task.FromResult<SqlSignatureHelp?>(null);



        public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(SqlCodeActionRequest request, CancellationToken cancellationToken = default) =>

            Task.FromResult<IReadOnlyList<SqlCodeAction>>([]);



        public void DisableRule(string ruleId) { }

        public void EnableRule(string ruleId) { }

        public void Release(EditorDocumentId documentId) { }

    }

}


