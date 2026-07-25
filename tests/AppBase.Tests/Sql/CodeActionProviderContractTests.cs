using AppBase.Data.Completion;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Sql;

namespace AppBase.Tests.Sql;

public sealed class CodeActionProviderContractTests
{
    private readonly ICodeActionProvider _sut = new CodeActionProvider();

    [Fact]
    public void Implements_ICodeActionProvider()
    {
        Assert.IsAssignableFrom<ICodeActionProvider>(_sut);
    }

    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(CodeActionProvider.Default, CodeActionProvider.Default);
    }

    [Fact]
    public void Default_implements_interface()
    {
        Assert.IsAssignableFrom<ICodeActionProvider>(CodeActionProvider.Default);
    }

    [Fact]
    public void Static_methods_delegate_to_default()
    {
        var staticResult = CodeActionProvider.GetFormatAction();
        var instanceResult = CodeActionProvider.Default.DoGetFormatAction();
        Assert.NotNull(staticResult);
        Assert.NotNull(instanceResult);
        Assert.Equal(staticResult.Description, instanceResult.Description);
        Assert.Equal(staticResult.Kind, instanceResult.Kind);
    }

    [Fact]
    public void Interface_methods_delegate_correctly()
    {
        var interfaceResult = _sut.GetFormatAction();
        var instanceResult = CodeActionProvider.Default.DoGetFormatAction();
        Assert.NotNull(interfaceResult);
        Assert.NotNull(instanceResult);
        Assert.Equal(interfaceResult.Description, instanceResult.Description);
        Assert.Equal(interfaceResult.Kind, instanceResult.Kind);
    }

    // ── GetFormatAction ──

    [Fact]
    public void GetFormatAction_returns_non_null()
    {
        var result = _sut.GetFormatAction();
        Assert.NotNull(result);
    }

    [Fact]
    public void GetFormatAction_has_expected_description()
    {
        var result = _sut.GetFormatAction();
        Assert.Equal("Format SQL", result.Description);
    }

    [Fact]
    public void GetFormatAction_kind_is_format_document()
    {
        var result = _sut.GetFormatAction();
        Assert.Equal(CodeActionKind.FormatDocument, result.Kind);
    }

    [Fact]
    public void GetFormatAction_apply_returns_formatted_sql()
    {
        var result = _sut.GetFormatAction();
        Assert.NotNull(result.Apply);

        string input = "select 1";
        string formatted = result.Apply(input);
        Assert.Contains("SELECT", formatted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFormatAction_rule_id_is_empty()
    {
        var result = _sut.GetFormatAction();
        Assert.Equal(string.Empty, result.RuleId);
    }

    // ── GetActions ──

    [Fact]
    public void GetActions_returns_disable_rule_action_even_without_quickfix()
    {
        // Create a simple lint issue that may not have a quick fix
        var issue = new LintIssue("TEST001", "Test issue", LintSeverity.Warning, 0, 5, 1, 0, 1, 5);

        var actions = _sut.GetActions(issue, "select 1");
        Assert.NotEmpty(actions);

        // Should always include a "Disable rule" action
        Assert.Contains(actions, a =>
            a.Kind == CodeActionKind.DisableRule &&
            a.Description.Contains("TEST001"));
    }

    [Fact]
    public void GetActions_severity_label_maps_correctly()
    {
        var testCases = new[]
        {
            (LintSeverity.Error, "Error"),
            (LintSeverity.Warning, "Warning"),
            (LintSeverity.Information, "Info"),
            (LintSeverity.Hint, "Hint")
        };

        foreach (var (severity, expectedLabel) in testCases)
        {
            var issue = new LintIssue("SEV001", $"{severity}: test", severity, 0, 5, 1, 0, 1, 5);

            var actions = _sut.GetActions(issue, "select 1");
            Assert.NotEmpty(actions);
            // The severity label is set via TooltipMessage and SeverityLabel
            Assert.Contains(actions, a =>
                a.Kind == CodeActionKind.DisableRule &&
                a.SeverityLabel == expectedLabel);
        }
    }

    [Fact]
    public void GetActions_has_tooltip_message()
    {
        var issue = new LintIssue("TIP001", "TIP001: detailed info about the issue", LintSeverity.Information, 0, 5, 1, 0, 1, 5);

        var actions = _sut.GetActions(issue, "select 1");

        // The message without the rule ID prefix should be set as TooltipMessage
        Assert.Contains(actions, a =>
            a.Kind == CodeActionKind.DisableRule &&
            a.TooltipMessage == "detailed info about the issue");
    }

    [Fact]
    public void GetActions_through_interface()
    {
        var issue = new LintIssue("IFACE01", "Interface test", LintSeverity.Hint, 0, 5, 1, 0, 1, 5);

        var actions = _sut.GetActions(issue, "select 1");
        Assert.NotEmpty(actions);
        Assert.Contains(actions, a => a.Kind == CodeActionKind.DisableRule);
    }
}
