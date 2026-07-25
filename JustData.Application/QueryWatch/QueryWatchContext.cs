namespace JustData.Application.QueryWatch;

/// <summary>
/// Connection context for Query Watch. <see cref="DatabaseType"/> is the
/// numeric value of the host database enum (kept as int to avoid AppBase references).
/// </summary>
public sealed record QueryWatchContext(
    string ConnectionName,
    string? DatabaseName,
    int DatabaseType);
