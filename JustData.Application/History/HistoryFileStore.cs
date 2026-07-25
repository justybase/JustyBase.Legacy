namespace JustData.Application.History;

public sealed class HistoryFileStore : IHistoryStore
{
    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            return [];

        var entries = new List<HistoryEntry>();

        await Task.Run(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8);

            while (stream.Position < stream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var date = DateTime.FromBinary(reader.ReadInt64());
                var sql = reader.ReadString();
                var database = reader.ReadString();
                var connectionName = reader.ReadString();

                entries.Add(new HistoryEntry(date, sql, database, connectionName));
            }
        }, cancellationToken);

        return entries;
    }
}
