namespace JustData.Application.Schema;

/// <summary>
/// Provider-neutral schema access. Implementations own provider caches and may load
/// children lazily; callers never need to copy a provider's mutable dictionaries.
/// </summary>
public interface ISchemaRepository
{
    Task<IReadOnlyList<SchemaNode>> GetRootsAsync(
        string? connectionName = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemaNode>> GetChildrenAsync(
        SchemaNode parent,
        CancellationToken cancellationToken = default);

    Task<SchemaSearchResult> SearchAsync(
        SchemaSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemaReference>> GetReferencesAsync(
        string sql,
        string? connectionName = null,
        CancellationToken cancellationToken = default);

    Task RefreshAsync(
        string? connectionName = null,
        CancellationToken cancellationToken = default,
        SchemaRefreshRequest? request = null);

    /// <summary>Downloads a single database catalog entry and merges it into the connection schema.</summary>
    /// <returns><c>true</c> when the database was attached successfully.</returns>
    Task<bool> AttachDatabaseAsync(
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken = default);
}

public interface ISchemaDdlService
{
    Task<string> GetDdlAsync(
        SchemaDdlRequest request,
        CancellationToken cancellationToken = default);
}

