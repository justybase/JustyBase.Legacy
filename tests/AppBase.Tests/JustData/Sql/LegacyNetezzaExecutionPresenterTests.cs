using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.UI.Sql;

namespace AppBase.Tests.JustData.Sql;

public sealed class LegacyNetezzaExecutionPresenterTests
{
    [Fact]
    public async Task ExecuteAsync_forwards_the_document_request_to_the_host_delegate()
    {
        SqlExecutionRequest? received = null;
        var presenter = new LegacyNetezzaExecutionPresenter(
            (request, _) => Execute(request),
            (_, _) => { });
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1");

        SqlExecutionEvent[] events = await CollectAsync(presenter.ExecuteAsync(request));

        Assert.Same(request, received);
        Assert.Equal(SqlExecutionOutcome.Success, Assert.Single(events).Outcome);

        async IAsyncEnumerable<SqlExecutionEvent> Execute(SqlExecutionRequest input)
        {
            received = input;
            yield return SqlExecutionEvent.Completed(input.DocumentId, SqlExecutionOutcome.Success);
            await Task.CompletedTask;
        }
    }

    [Fact]
    public void Cancel_forwards_document_and_connection_to_the_host_delegate()
    {
        EditorDocumentId? cancelledDocument = null;
        string? cancelledConnection = null;
        var presenter = new LegacyNetezzaExecutionPresenter(
            (_, _) => Empty(),
            (document, connection) =>
            {
                cancelledDocument = document;
                cancelledConnection = connection;
            });
        EditorDocumentId documentId = EditorDocumentId.New();

        presenter.Cancel(documentId, "warehouse");

        Assert.Equal(documentId, cancelledDocument);
        Assert.Equal("warehouse", cancelledConnection);
    }

    private static async IAsyncEnumerable<SqlExecutionEvent> Empty()
    {
        yield break;
    }

    private static async Task<SqlExecutionEvent[]> CollectAsync(IAsyncEnumerable<SqlExecutionEvent> source)
    {
        var events = new List<SqlExecutionEvent>();
        await foreach (SqlExecutionEvent item in source)
            events.Add(item);
        return events.ToArray();
    }
}
