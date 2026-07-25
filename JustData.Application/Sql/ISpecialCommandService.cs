namespace JustData.Application.Sql;

public sealed record SpecialCommandResult(
    string? ReplacementSql,
    bool WasHandled,
    int? SleepMilliseconds = null,
    int? MaxRows = null);

public interface ISpecialCommandService
{
    Task<SpecialCommandResult> TryHandleAsync(
        string sql,
        CancellationToken cancellationToken = default);
}
