using JustData.Application.Editor;
using JustData.Application.Sql;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Transitional adapter around the remaining Netezza execution surface. It
/// keeps the engine contract outside the shell form while provider execution
/// is migrated to a standalone engine.
/// </summary>
public sealed class LegacyNetezzaExecutionPresenter : ISqlExecutionDocumentPresenter
{
    private readonly Func<SqlExecutionRequest, CancellationToken, IAsyncEnumerable<SqlExecutionEvent>> _execute;
    private readonly Action<EditorDocumentId, string> _cancel;

    public LegacyNetezzaExecutionPresenter(
        Func<SqlExecutionRequest, CancellationToken, IAsyncEnumerable<SqlExecutionEvent>> execute,
        Action<EditorDocumentId, string> cancel)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
    }

    public IAsyncEnumerable<SqlExecutionEvent> ExecuteAsync(
        SqlExecutionRequest request,
        CancellationToken cancellationToken = default) => _execute(request, cancellationToken);

    public void Cancel(EditorDocumentId documentId, string connectionName) => _cancel(documentId, connectionName);
}
