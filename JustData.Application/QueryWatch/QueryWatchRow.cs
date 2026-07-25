namespace JustData.Application.QueryWatch;

/// <summary>One active session / query row from the monitor query.</summary>
public sealed class QueryWatchRow
{
    public QueryWatchRow(
        IReadOnlyDictionary<string, object?> values,
        string? dropSessionSql)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        DropSessionSql = string.IsNullOrWhiteSpace(dropSessionSql)
            ? null
            : dropSessionSql.Trim();
    }

    /// <summary>Visible column values (excludes DROP_SESSION_SQL).</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>SQL to terminate the session, when available for this provider.</summary>
    public string? DropSessionSql { get; }

    public bool CanDrop => DropSessionSql is not null;
}
