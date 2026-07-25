using System.Data.Common;

namespace JustyBaseLegacy.UI.Sql;

public enum LegacyNetezzaFetchExceptionKind
{
    /// <summary>Database / Netezza SQL error during Read (e.g. type mismatch).</summary>
    DbFault,

    /// <summary>Transport / IO / other SystemException that must stop the result-set loop.</summary>
    SystemFault,

    /// <summary>Driver quirk treated as soft break, not a SQL failure for the VM bridge.</summary>
    BenignNoData,

    /// <summary>Unexpected non-SystemException failure during fetch.</summary>
    GenericFault
}

public enum LegacyNetezzaResultTabDisposition
{
    /// <summary>Tab was never shown — dispose prepared controls.</summary>
    DisposeUnattached,

    /// <summary>Tab was attached — remove it from the TabControl.</summary>
    DiscardAttached,

    /// <summary>Keep the tab and InitGrid (including successful empty results).</summary>
    KeepAndInit
}

/// <summary>
/// Pure decision/state helper for the legacy Netezza grid fetch loop.
/// Keeps NextResult / tab-attach / Close / Log-selection policy out of BaseWindow
/// so regressions (endless empty Result tabs, transport MessageBox after pg_atoi) are unit-testable.
/// </summary>
public sealed class LegacyNetezzaResultFetchSession
{
    public const string BenignNoDataMessage = "No data exists for the row/column.";

    public bool FetchFailed { get; private set; }
    public bool StopResultSets { get; private set; }
    public bool TabAttached { get; private set; }
    public bool GridRegistered { get; private set; }
    public string? FailureMessage { get; private set; }
    public bool SoftBreak { get; private set; }

    /// <summary>
    /// Call at the start of each result-set iteration so attach/register state
    /// is per-grid while StopResultSets/FetchFailed span the whole reader.
    /// </summary>
    public void BeginResultSet()
    {
        TabAttached = false;
        GridRegistered = false;
    }

    public static LegacyNetezzaFetchExceptionKind Classify(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        if (ex is DbException)
            return LegacyNetezzaFetchExceptionKind.DbFault;

        if (ex is SystemException)
        {
            if (string.Equals(ex.Message, BenignNoDataMessage, StringComparison.Ordinal))
                return LegacyNetezzaFetchExceptionKind.BenignNoData;
            return LegacyNetezzaFetchExceptionKind.SystemFault;
        }

        return LegacyNetezzaFetchExceptionKind.GenericFault;
    }

    public void OnFetchFault(LegacyNetezzaFetchExceptionKind kind, string? message)
    {
        switch (kind)
        {
            case LegacyNetezzaFetchExceptionKind.BenignNoData:
                SoftBreak = true;
                StopResultSets = true;
                break;

            case LegacyNetezzaFetchExceptionKind.DbFault:
            case LegacyNetezzaFetchExceptionKind.SystemFault:
            case LegacyNetezzaFetchExceptionKind.GenericFault:
                FetchFailed = true;
                StopResultSets = true;
                if (!string.IsNullOrWhiteSpace(message))
                    FailureMessage = message;
                break;

            default:
                FetchFailed = true;
                StopResultSets = true;
                if (!string.IsNullOrWhiteSpace(message))
                    FailureMessage = message;
                break;
        }
    }

    public void OnFetchFault(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        OnFetchFault(Classify(ex), ex.Message);
    }

    /// <summary>
    /// Whether the do/while may call <c>NextResult()</c>. Caller evaluates this first;
    /// only when true should it invoke <c>rdr.NextResult()</c>.
    /// </summary>
    public bool ShouldContinueNextResult(bool cancelling, bool readerClosed) =>
        !StopResultSets && !cancelling && !readerClosed;

    public bool ShouldAttachTabForRow(int oneBasedRowNumber) =>
        oneBasedRowNumber == 1 && !FetchFailed && !SoftBreak;

    public void MarkTabAttached()
    {
        TabAttached = true;
    }

    /// <summary>
    /// Register the grid once on the success completion path — never on first-row attach —
    /// to avoid duplicate ResultSetId entries in the document registry.
    /// </summary>
    public bool ShouldRegisterGridOnSuccess()
    {
        if (FetchFailed || SoftBreak || GridRegistered)
            return false;

        GridRegistered = true;
        return true;
    }

    public LegacyNetezzaResultTabDisposition DecideTabDisposition(
        bool cancelling,
        bool weirdCancelEmptySchema,
        int rowCount)
    {
        // SoftBreak (benign ODBC "No data exists…") must not KeepAndInit — Log already
        // recorded the quirk and an empty Result tab would be misleading.
        if (FetchFailed || SoftBreak)
        {
            return TabAttached
                ? LegacyNetezzaResultTabDisposition.DiscardAttached
                : LegacyNetezzaResultTabDisposition.DisposeUnattached;
        }

        if (cancelling && weirdCancelEmptySchema && rowCount == 0)
        {
            return TabAttached
                ? LegacyNetezzaResultTabDisposition.DiscardAttached
                : LegacyNetezzaResultTabDisposition.DisposeUnattached;
        }

        return LegacyNetezzaResultTabDisposition.KeepAndInit;
    }

    public bool ShouldCloseReaderAfterLoop() => !StopResultSets && !FetchFailed;

    /// <summary>
    /// When the Log tab's IsSuccess is false, FinalizeSqlRun must keep Log selected.
    /// </summary>
    public static bool PreferLogTab(bool isSuccess) => !isSuccess;
}
