namespace JustData.Application.Login;

public interface IDatabaseCatalogService
{
    Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
}
