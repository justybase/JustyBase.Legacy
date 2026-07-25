using System.Windows.Forms;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Text-style SQL execution log (replaces the former DataGridView log tab).
/// Implementations should marshal to the UI thread when needed.
/// </summary>
public interface ISqlExecutionLog
{
    /// <summary>WinForms control hosting the log view (layout / parenting).</summary>
    Control View { get; }

    /// <summary>
    /// Appends a normal log entry. Accepts the same 2–6 field shapes formerly
    /// passed to <c>DataGridView.Rows.Add</c>: timestamp, elapsed, connection, db, info, code.
    /// </summary>
    void AppendEntry(params object?[] fields);

    /// <summary>Appends an error-styled log entry (same field shapes as <see cref="AppendEntry"/>).</summary>
    void AppendErrorEntry(params object?[] fields);

    /// <summary>Appends an emphasis-styled log entry (e.g. estimated cost).</summary>
    void AppendEmphasisEntry(params object?[] fields);

    /// <summary>Clears all log text.</summary>
    void Clear();
}
