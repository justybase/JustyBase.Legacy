using JustyBase.Core.Risk;

namespace JustData.Application.Sql;

/// <summary>Host confirmation seam used by SQL risk checks.</summary>
public interface ISqlRiskConfirmation
{
    /// <summary>Returns true when execution should continue.</summary>
    bool Confirm(SqlRisk risk);
}

public sealed class SqlExecutionRiskGate(SqlRiskAnalysisService analysis)
{
    private readonly SqlRiskAnalysisService _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));

    public bool AllowExecution(
        string sql,
        string? driverName,
        bool suppressWarnings,
        ISqlRiskConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        foreach (SqlRisk risk in _analysis.Analyze(sql, driverName))
        {
            // Keep the legacy setting name for persisted-config compatibility.
            // It suppresses only destructive statement confirmations.
            if (suppressWarnings
                && risk.Kind is SqlRiskKind.UnsafeUpdateDelete or SqlRiskKind.UnsafeDrop)
            {
                continue;
            }

            if (!confirmation.Confirm(risk))
                return false;
        }

        return true;
    }
}
