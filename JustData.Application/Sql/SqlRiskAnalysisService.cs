namespace JustData.Application.Sql;

public enum SqlRiskKind
{
    UnsafeUpdateDelete,
    MissingDistribute,
    SelectInto
}

public sealed record SqlRisk(SqlRiskKind Kind, string Message, bool IsBlocking);

public interface ISqlRiskAnalysisService
{
    IReadOnlyList<SqlRisk> Analyze(string sql, string? driverName = null);
}

/// <summary>Thin host adapter over <see cref="JustyBase.Core.Risk.SqlRiskAnalysisService"/>.</summary>
public sealed class SqlRiskAnalysisService : ISqlRiskAnalysisService
{
    private static readonly JustyBase.Core.Risk.SqlRiskAnalysisService Shared = new();

    public IReadOnlyList<SqlRisk> Analyze(string sql, string? driverName = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return [];

        return Shared.Analyze(sql, driverName)
            .Select(risk => new SqlRisk(ToLegacyKind(risk.Kind), risk.Message, risk.IsBlocking))
            .ToArray();
    }

    private static SqlRiskKind ToLegacyKind(JustyBase.Core.Risk.SqlRiskKind kind)
        => kind switch
        {
            JustyBase.Core.Risk.SqlRiskKind.UnsafeUpdateDelete => SqlRiskKind.UnsafeUpdateDelete,
            JustyBase.Core.Risk.SqlRiskKind.MissingDistribute => SqlRiskKind.MissingDistribute,
            JustyBase.Core.Risk.SqlRiskKind.SelectInto => SqlRiskKind.SelectInto,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}
