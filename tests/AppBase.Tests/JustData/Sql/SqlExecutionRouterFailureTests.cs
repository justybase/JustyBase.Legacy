using AppBase.Data.Core.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.UI.Sql;
using NSubstitute;

namespace AppBase.Tests.JustData.Sql;

public sealed class SqlExecutionRouterFailureTests
{
    [Fact]
    public async Task Provider_exception_is_redacted_and_ends_with_one_failed_event()
    {
        var database = Substitute.For<IGeneralDbService>();
        database.DriverName(Arg.Any<string>()).Returns("TestDriver");
        var context = new SqlExecutionEngineContext();
        var router = new SqlExecutionRouter(database, [new ThrowingEngine()], context);
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1") { ConnectionName = "test" };

        SqlExecutionEvent[] events = await CollectAsync(router.ExecuteAsync(request));

        SqlExecutionEvent terminal = Assert.Single(events, item => item.Kind == SqlExecutionEventKind.Completed);
        Assert.Equal(SqlExecutionOutcome.Failed, terminal.Outcome);
        Assert.DoesNotContain("do-not-leak", terminal.ErrorMessage);
    }

    private static async Task<SqlExecutionEvent[]> CollectAsync(IAsyncEnumerable<SqlExecutionEvent> source)
    {
        var events = new List<SqlExecutionEvent>();
        await foreach (SqlExecutionEvent item in source)
            events.Add(item);
        return events.ToArray();
    }

    private sealed class ThrowingEngine : ISqlExecutionEngine
    {
        public bool CanExecute(string driverName) => driverName == "TestDriver";

        public async IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
            SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request.SqlText is null)
                yield return SqlExecutionEvent.Completed(request.DocumentId, SqlExecutionOutcome.Success);

            await Task.Yield();
            throw new InvalidOperationException("password=do-not-leak");
        }
    }
}
