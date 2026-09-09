using JustData.Application.Sql;
using JustyBase.Core.Risk;

namespace AppBase.Tests.JustDataApplication.Sql;

public sealed class SqlExecutionRiskGateTests
{
    [Fact]
    public void AllowExecution_returns_true_when_warnings_are_suppressed()
    {
        var gate = new SqlExecutionRiskGate(new SqlRiskAnalysisService());

        bool allowed = gate.AllowExecution(
            "UPDATE users SET value = 1",
            driverName: null,
            suppressWarnings: true,
            new AcceptAllConfirmation());

        Assert.True(allowed);
    }

    [Fact]
    public void AllowExecution_returns_false_when_user_rejects_a_risk()
    {
        var gate = new SqlExecutionRiskGate(new SqlRiskAnalysisService());

        bool allowed = gate.AllowExecution(
            "UPDATE users SET value = 1",
            driverName: null,
            suppressWarnings: false,
            new RejectAllConfirmation());

        Assert.False(allowed);
    }

    [Fact]
    public void AllowExecution_passes_driver_to_analysis_for_netezza_checks()
    {
        var gate = new SqlExecutionRiskGate(new SqlRiskAnalysisService());

        bool allowed = gate.AllowExecution(
            "CREATE TABLE t (id INT)",
            driverName: "NetezzaSQL",
            suppressWarnings: false,
            new RejectAllConfirmation());

        Assert.False(allowed);
    }

    [Fact]
    public void AllowExecution_suppression_skips_update_delete_and_drop_but_keeps_other_risks()
    {
        var gate = new SqlExecutionRiskGate(new SqlRiskAnalysisService());
        var confirmation = new RecordingConfirmation(accept: false);

        Assert.True(gate.AllowExecution(
            "UPDATE users SET value = 1",
            driverName: null,
            suppressWarnings: true,
            confirmation));
        Assert.True(gate.AllowExecution(
            "DROP TABLE users",
            driverName: null,
            suppressWarnings: true,
            confirmation));
        Assert.False(gate.AllowExecution(
            "CREATE TABLE users (id INT)",
            driverName: "NetezzaSQL",
            suppressWarnings: true,
            confirmation));
        Assert.Single(confirmation.Risks);
        Assert.Equal(SqlRiskKind.MissingDistribute, confirmation.Risks[0].Kind);
    }

    private sealed class AcceptAllConfirmation : ISqlRiskConfirmation
    {
        public bool Confirm(SqlRisk risk) => true;
    }

    private sealed class RejectAllConfirmation : ISqlRiskConfirmation
    {
        public bool Confirm(SqlRisk risk) => false;
    }

    private sealed class RecordingConfirmation(bool accept) : ISqlRiskConfirmation
    {
        public List<SqlRisk> Risks { get; } = [];

        public bool Confirm(SqlRisk risk)
        {
            Risks.Add(risk);
            return accept;
        }
    }
}
