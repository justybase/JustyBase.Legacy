using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBaseLegacy.UI.Controls;
using JustData.Application.Variables;
using JustData.ViewModels.Variables;
using NSubstitute;

namespace JustData.Preferences.Tests;

public sealed class VariablesCharacterizationTests
{
    [Fact]
    public void Legacy_variables_control_combines_session_and_global_rows()
    {
        ISessionVariableRuntimeContext helpers = Substitute.For<ISessionVariableRuntimeContext>();
        IReadOnlyDictionary<string, string> sessionVariables = new Dictionary<string, string>
        {
            ["session_id"] = "42"
        };
        IReadOnlyDictionary<string, string> globalVariables = new Dictionary<string, string>
        {
            ["global_name"] = "value"
        };
        helpers.ActualTabTitleText.Returns("query.sql");
        helpers.GetSessionVariables("query.sql").Returns(sessionVariables);
        helpers.GlobalVariables.Returns(globalVariables);

        using VariablesViewModel vm = new(new TestSessionVariableStore(
            new Dictionary<string, Dictionary<string, string>> { ["query.sql"] = new(sessionVariables) },
            new Dictionary<string, string>(globalVariables)));
        using VariablesControl control = new(
            baseWindow: null!,
            vm,
            () => helpers.ActualTabTitleText,
            Substitute.For<IUiHelperService>(),
            Substitute.For<IColorTheme>());

        Assert.Equal(2, control.RowCount);
    }

    [Fact]
    public void Legacy_clear_button_clears_global_values_but_keeps_session_values()
    {
        ISessionVariableRuntimeContext helpers = Substitute.For<ISessionVariableRuntimeContext>();
        Dictionary<string, string> globals = new() { ["global_name"] = "value" };
        Dictionary<string, string> session = new() { ["session_id"] = "42" };
        helpers.ActualTabTitleText.Returns("query.sql");
        helpers.GetSessionVariables("query.sql").Returns(session);
        helpers.GlobalVariables.Returns(globals);

        using VariablesViewModel vm = new(new TestSessionVariableStore(
            new Dictionary<string, Dictionary<string, string>> { ["query.sql"] = session },
            globals));
        using VariablesControl control = new(
            baseWindow: null!,
            vm,
            () => helpers.ActualTabTitleText,
            Substitute.For<IUiHelperService>(),
            Substitute.For<IColorTheme>());

        control.Controls
            .OfType<Panel>()
            .SelectMany(panel => panel.Controls.OfType<Button>())
            .Single(button => button.Name == "_btClearVariables")
            .PerformClick();

        Assert.Empty(globals);
        Assert.Single(session);
    }

    private sealed class TestSessionVariableStore(
        Dictionary<string, Dictionary<string, string>> sessionVariables,
        Dictionary<string, string> globalVariables) : ISessionVariableStore
    {
        public event EventHandler? Changed;
        public IReadOnlyDictionary<string, string> GlobalVariables => globalVariables;

        public IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey) =>
            sessionVariables.TryGetValue(documentKey, out Dictionary<string, string>? values)
                ? values
                : new Dictionary<string, string>();

        public void ClearGlobalVariables()
        {
            globalVariables.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
