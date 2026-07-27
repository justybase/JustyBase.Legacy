using JustData.Application.Sql;

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

    private sealed class AcceptAllConfirmation : ISqlRiskConfirmation
    {
        public bool Confirm(SqlRisk risk) => true;
    }

    private sealed class RejectAllConfirmation : ISqlRiskConfirmation
    {
        public bool Confirm(SqlRisk risk) => false;
    }
}
