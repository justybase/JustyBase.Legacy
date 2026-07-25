namespace JustData.Application.QueryWatch;

public interface IQueryWatchService
{
    bool IsSupported(int databaseType);

    Task<IReadOnlyList<QueryWatchRow>> RefreshAsync(
        QueryWatchContext context,
        CancellationToken cancellationToken = default);

    Task DropSessionAsync(
        string dropSql,
        QueryWatchContext context,
        CancellationToken cancellationToken = default);
}
