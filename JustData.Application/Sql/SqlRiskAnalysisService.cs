using System.Text.RegularExpressions;

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

public sealed partial class SqlRiskAnalysisService : ISqlRiskAnalysisService
{
    public IReadOnlyList<SqlRisk> Analyze(string sql, string? driverName = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return [];

        var risks = new List<SqlRisk>();

        if (StartsWithAny(sql, ["UPDATE", "DELETE"]) && !WhereClauseRegex().IsMatch(sql))
        {
            risks.Add(new SqlRisk(
                SqlRiskKind.UnsafeUpdateDelete,
                "UPDATE/DELETE without a WHERE clause.",
                IsBlocking: false));
        }

        bool isNetezza = string.Equals(driverName, "NetezzaSQL", StringComparison.OrdinalIgnoreCase);
        if (isNetezza
            && StartsWithAny(sql, ["CREATE TABLE", "CREATE TEMP TABLE"])
            && !DistributeClauseRegex().IsMatch(sql))
        {
            risks.Add(new SqlRisk(
                SqlRiskKind.MissingDistribute,
                "CREATE TABLE without a DISTRIBUTE option.",
                IsBlocking: false));
        }

        if (SelectIntoRegex().IsMatch(sql))
        {
            risks.Add(new SqlRisk(
                SqlRiskKind.SelectInto,
                "SELECT INTO may cause table distribution problems.",
                IsBlocking: false));
        }

        return risks;
    }

    private static bool StartsWithAny(string text, string[] prefixes)
    {
        foreach (string prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    [GeneratedRegex(@"\bWHERE\b", RegexOptions.IgnoreCase)]
    private static partial Regex WhereClauseRegex();

    [GeneratedRegex(@"\bDISTRIBUTE\b", RegexOptions.IgnoreCase)]
    private static partial Regex DistributeClauseRegex();

    [GeneratedRegex(@"SELECT\s+.*\s+INTO\s+\w+\s+($|FROM)", RegexOptions.IgnoreCase)]
    private static partial Regex SelectIntoRegex();
}
