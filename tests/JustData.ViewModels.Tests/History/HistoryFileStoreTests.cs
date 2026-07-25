using JustData.Application.History;

namespace JustData.ViewModels.Tests.History;

public sealed class HistoryFileStoreTests
{
    [Fact]
    public async Task LoadAsync_reads_legacy_binary_history_records()
    {
        string path = Path.Combine(Path.GetTempPath(), $"justdata-history-{Guid.NewGuid():N}.dat");
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(new DateTime(2026, 7, 23, 10, 30, 0, DateTimeKind.Utc).ToBinary());
                writer.Write("SELECT 1");
                writer.Write("TESTDB");
                writer.Write("local");
            }

            var entries = await new HistoryFileStore().LoadAsync(path);

            var entry = Assert.Single(entries);
            Assert.Equal("SELECT 1", entry.Sql);
            Assert.Equal("TESTDB", entry.Database);
            Assert.Equal("local", entry.ConnectionName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_returns_empty_for_missing_file()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-history-{Guid.NewGuid():N}.dat");

        Assert.Empty(await new HistoryFileStore().LoadAsync(path));
    }

    [Fact]
    public async Task LoadAsync_honors_a_pre_canceled_token()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cancel-history-{Guid.NewGuid():N}.dat");
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: false))
            {
                for (int index = 0; index < 3; index++)
                {
                    writer.Write(DateTime.UtcNow.ToBinary());
                    writer.Write($"SELECT {index}");
                    writer.Write("TESTDB");
                    writer.Write("local");
                }
            }

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new HistoryFileStore().LoadAsync(path, cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
