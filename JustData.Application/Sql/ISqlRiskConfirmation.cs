namespace JustData.Application.Sql;

/// <summary>Host confirmation seam used by SQL risk checks.</summary>
public interface ISqlRiskConfirmation
{
    /// <summary>Returns true when execution should continue.</summary>
    bool Confirm(SqlRisk risk);
}

public sealed class SqlExecutionRiskGate(ISqlRiskAnalysisService analysis)
{
    private readonly ISqlRiskAnalysisService _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));

    public bool AllowExecution(
        string sql,
        string? driverName,
        bool suppressWarnings,
        ISqlRiskConfirmation confirmation)
    {
        if (suppressWarnings)
            return true;

        ArgumentNullException.ThrowIfNull(confirmation);
        foreach (SqlRisk risk in _analysis.Analyze(sql, driverName))
        {
            if (!confirmation.Confirm(risk))
                return false;
        }

        return true;
    }
}
