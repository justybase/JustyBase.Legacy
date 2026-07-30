using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Sql;

namespace JustData.ViewModels.Tests;

public sealed class SqlAuthoringViewModelRuleTests
{
    [Fact]
    public void DisableRule_adds_to_disabled_rules_collection()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());

        vm.DisableRule("no-unnecessary-semicolons");

        Assert.Single(vm.DisabledRules);
        Assert.Equal("no-unnecessary-semicolons", vm.DisabledRules[0]);
    }

    [Fact]
    public void DisableRule_does_not_add_duplicates()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());

        vm.DisableRule("rule1");
        vm.DisableRule("rule1");
        vm.DisableRule("RULE1"); // case-insensitive

        Assert.Single(vm.DisabledRules);
    }

    [Fact]
    public void DisableRule_ignores_whitespace_rule_id()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());

        vm.DisableRule("");
        vm.DisableRule("  ");
        vm.DisableRule(null!);

        Assert.Empty(vm.DisabledRules);
    }

    [Fact]
    public void EnableRule_removes_from_disabled_rules_collection()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());
        vm.DisableRule("rule1");
        vm.DisableRule("rule2");

        vm.EnableRule("rule1");

        Assert.Single(vm.DisabledRules);
        Assert.Equal("rule2", vm.DisabledRules[0]);
    }

    [Fact]
    public void EnableRule_ignores_whitespace_rule_id()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());
        vm.DisableRule("rule1");

        vm.EnableRule("");
        vm.EnableRule("  ");
        vm.EnableRule(null!);

        Assert.Single(vm.DisabledRules);
    }

    [Fact]
    public void EnableRule_nonexistent_rule_does_not_throw()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());
        vm.DisableRule("rule1");

        vm.EnableRule("nonexistent");

        Assert.Single(vm.DisabledRules);
    }

    [Fact]
    public async Task DiagnosticsChanged_event_fires_after_lint()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SimpleAuthoringUseCase();
        using var vm = new SqlAuthoringViewModel(documentId, useCase);
        IReadOnlyList<SqlDiagnostic>? received = null;
        vm.DiagnosticsChanged += d => received = d;

        await vm.LintNowAsync("select 1");

        Assert.NotNull(received);
        Assert.Single(received!);
    }

    [Fact]
    public async Task Code_actions_returns_from_use_case()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SimpleAuthoringUseCase();
        using var vm = new SqlAuthoringViewModel(documentId, useCase);
        var diagnostic = new SqlDiagnostic(SqlDiagnosticSeverity.Warning, "test");

        var actions = await vm.GetCodeActionsAsync("select 1", diagnostic);

        Assert.Single(actions);
        Assert.Equal("Fix it", actions[0].Title);
    }

    [Fact]
    public void Dispose_calls_release_on_use_case()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SimpleAuthoringUseCase();
        var vm = new SqlAuthoringViewModel(documentId, useCase);

        vm.Dispose();

        Assert.Equal(documentId, useCase.ReleasedDocumentId);
    }

    [Fact]
    public void Dispose_can_be_called_multiple_times()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SimpleAuthoringUseCase();
        var vm = new SqlAuthoringViewModel(documentId, useCase);

        vm.Dispose();
        vm.Dispose(); // should not throw
    }

    [Fact]
    public async Task ScheduleLintAsync_debounces_rapid_calls()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new CountingAuthoringUseCase();
        using var vm = new SqlAuthoringViewModel(documentId, useCase);

        // Fire multiple lint requests with very short debounce
        var tasks = Enumerable.Range(0, 5)
            .Select(i => vm.ScheduleLintAsync($"select {i}", debounce: TimeSpan.FromMilliseconds(10)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Should have been debounced - fewer lint calls than total requests
        Assert.True(useCase.LintCount < 5, $"Expected debounce, but got {useCase.LintCount} lint calls");
        Assert.False(vm.IsLinting);
    }

    [Fact]
    public void LintOnSave_default_is_true()
    {
        using var vm = new SqlAuthoringViewModel(EditorDocumentId.New());
        Assert.True(vm.LintOnSave);
    }

    [Fact]
    public async Task ScheduleLintAsync_huge_script_skips_use_case_and_clears_diagnostics()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new SeedThenCountAuthoringUseCase();
        using var vm = new SqlAuthoringViewModel(documentId, useCase);
        await vm.LintNowAsync("select 1");
        Assert.Equal(1, useCase.LintCount);
        Assert.Single(vm.Diagnostics);

        IReadOnlyList<SqlDiagnostic>? received = null;
        vm.DiagnosticsChanged += d => received = d;

        await vm.ScheduleLintAsync(
            "select 1",
            debounce: TimeSpan.Zero,
            knownLineCount: JustyBase.NetezzaSqlParser.Authoring.SqlPerformancePolicy.HugeScriptLineThreshold + 1);

        Assert.Equal(1, useCase.LintCount);
        Assert.Empty(vm.Diagnostics);
        Assert.NotNull(received);
        Assert.Empty(received!);
    }

    [Fact]
    public async Task LintNowAsync_save_on_huge_script_still_calls_use_case()
    {
        var documentId = EditorDocumentId.New();
        var useCase = new CountingAuthoringUseCase();
        using var vm = new SqlAuthoringViewModel(documentId, useCase);

        await vm.LintOnSaveAsync(
            "select 1",
            knownLineCount: JustyBase.NetezzaSqlParser.Authoring.SqlPerformancePolicy.HugeScriptLineThreshold + 1);

        Assert.Equal(1, useCase.LintCount);
        Assert.Equal(
            JustyBase.NetezzaSqlParser.Authoring.SqlLintInvocation.Save,
            useCase.LastInvocation);
    }

    private sealed class SeedThenCountAuthoringUseCase : ISqlAuthoringUseCase
    {
        private int _lintCount;
        public int LintCount => Volatile.Read(ref _lintCount);

        public Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _lintCount);
            return Task.FromResult(new SqlLintResult(
                request.DocumentId,
                [new SqlDiagnostic(SqlDiagnosticSeverity.Warning, "seed")]));
        }

        public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SqlCompletionItem>>([]);

        public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<SqlSignatureHelp?>(null);

        public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(SqlCodeActionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SqlCodeAction>>([]);

        public void DisableRule(string ruleId) { }
        public void EnableRule(string ruleId) { }
        public void Release(EditorDocumentId documentId) { }
    }

    private sealed class SimpleAuthoringUseCase : ISqlAuthoringUseCase
    {
        public EditorDocumentId? ReleasedDocumentId { get; private set; }

        public Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SqlLintResult(request.DocumentId,
                [new SqlDiagnostic(SqlDiagnosticSeverity.Information, request.SqlText)]));
        }

        public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SqlCompletionItem>>([]);

        public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<SqlSignatureHelp?>(null);

        public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(SqlCodeActionRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SqlCodeAction>>([
                new SqlCodeAction("Fix it", [])
            ]);
        }

        public void DisableRule(string ruleId) { }
        public void EnableRule(string ruleId) { }
        public void Release(EditorDocumentId documentId) => ReleasedDocumentId = documentId;
    }

    private sealed class CountingAuthoringUseCase : ISqlAuthoringUseCase
    {
        private int _lintCount;
        public int LintCount => Volatile.Read(ref _lintCount);
        public JustyBase.NetezzaSqlParser.Authoring.SqlLintInvocation? LastInvocation { get; private set; }

        public Task<SqlLintResult> LintAsync(SqlLintRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _lintCount);
            LastInvocation = request.Invocation;
            return Task.FromResult(new SqlLintResult(request.DocumentId, []));
        }

        public Task<IReadOnlyList<SqlCompletionItem>> CompleteAsync(SqlCompletionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SqlCompletionItem>>([]);

        public Task<SqlSignatureHelp?> GetSignatureHelpAsync(SqlSignatureHelpRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<SqlSignatureHelp?>(null);

        public Task<IReadOnlyList<SqlCodeAction>> GetCodeActionsAsync(SqlCodeActionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SqlCodeAction>>([]);

        public void DisableRule(string ruleId) { }
        public void EnableRule(string ruleId) { }
        public void Release(EditorDocumentId documentId) { }
    }
}
