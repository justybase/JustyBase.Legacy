namespace JustData.Application.History;

public interface IHistoryStore
{
    Task<IReadOnlyList<HistoryEntry>> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
