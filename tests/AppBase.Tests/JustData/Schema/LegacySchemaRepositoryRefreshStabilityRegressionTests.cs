using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;
using JustyBaseLegacy.UI.Schema;
using NSubstitute;

namespace AppBase.Tests.JustData.Schema;

/// <summary>
/// Regression: overlapping schema refreshes raced shared Netezza collections
/// ("Collection was modified") and cancelled waiters poisoned the shared task
/// (TaskCanceledException → app close).
/// </summary>
public sealed class LegacySchemaRepositoryRefreshStabilityRegressionTests
{
    [Fact]
    public async Task Concurrent_refresh_calls_share_one_in_flight_download()
    {
        var download = new CountingDownload();
        IGeneralDb session = CreateNetezzaSession(download);
        var sessions = new ConnectionSessionRegistry();
        sessions.Set("NPS_144", session);

        var repository = new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            Substitute.For<IConnectionProfileCatalog>());

        Task first = repository.RefreshAsync("NPS_144");
        await download.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task second = repository.RefreshAsync("NPS_144");
        download.Release();

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Task.WhenAll(first, second));

        Assert.Equal(1, download.StartCount);
        Assert.Equal(1, download.MaxConcurrency);
    }

    [Fact]
    public async Task Cancelling_a_waiter_does_not_cancel_or_duplicate_in_flight_download()
    {
        var download = new CountingDownload();
        IGeneralDb session = CreateNetezzaSession(download);
        var sessions = new ConnectionSessionRegistry();
        sessions.Set("NPS_144", session);

        var repository = new LegacySchemaRepository(
            Substitute.For<IGeneralDbService>(),
            Substitute.For<IDatabaseRuntimeContext>(),
            Substitute.For<INetezzaCompletionRuntimeContext>(),
            sessions,
            Substitute.For<INetezzaSchemaTableCatalogWriter>(),
            Substitute.For<IConnectionProfileCatalog>());

        using var firstCts = new CancellationTokenSource();
        Task first = repository.RefreshAsync("NPS_144", firstCts.Token);
        await download.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        firstCts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        Task second = repository.RefreshAsync("NPS_144");
        download.Release();
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => second);

        Assert.Equal(1, download.StartCount);
        Assert.Equal(1, download.MaxConcurrency);
    }

    private static IGeneralDb CreateNetezzaSession(CountingDownload download)
    {
        var session = Substitute.For<IGeneralDb, INetezza>();
        session.When(x => x.InitDb()).Do(_ => { });
        ((INetezza)session)
            .DownloadSchemaNetezza(
                Arg.Any<string>(),
                Arg.Any<NetezzaRefreshMode>(),
                Arg.Any<List<string>>(),
                Arg.Any<bool>(),
                Arg.Any<Action?>())
            .Returns(_ => download.RunAsync());
        return session;
    }

    private sealed class CountingDownload
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _concurrency;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int StartCount { get; private set; }
        public int MaxConcurrency { get; private set; }

        public void Release() => _gate.TrySetResult();

        public async Task<bool> RunAsync()
        {
            StartCount++;
            int current = Interlocked.Increment(ref _concurrency);
            MaxConcurrency = Math.Max(MaxConcurrency, current);
            Started.TrySetResult();
            try
            {
                await _gate.Task.ConfigureAwait(false);
                // Return false so RefreshDatabaseAsync skips InitializeConnectionSchemaData
                // (which needs a fully populated Netezza catalog we are not stubbing here).
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _concurrency);
            }
        }
    }
}
