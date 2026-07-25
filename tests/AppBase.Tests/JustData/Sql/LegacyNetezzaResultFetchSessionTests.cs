using System.Data.Common;
using JustyBaseLegacy.UI.Sql;

namespace AppBase.Tests.JustData.Sql;

public sealed class LegacyNetezzaResultFetchSessionTests
{
    private sealed class FakeDbException : DbException
    {
        public FakeDbException(string message) : base(message) { }
    }

    [Fact]
    public void After_DbFault_simulated_NextResult_loop_stops_even_when_reader_stays_open()
    {
        var session = new LegacyNetezzaResultFetchSession();
        int iterations = 0;
        bool nextAlwaysTrue() => true;

        do
        {
            iterations++;
            if (iterations == 1)
                session.OnFetchFault(new FakeDbException("ERROR: pg_atoi: can't parse"));
        } while (session.ShouldContinueNextResult(cancelling: false, readerClosed: false)
                 && nextAlwaysTrue());

        Assert.Equal(1, iterations);
        Assert.False(session.ShouldContinueNextResult(cancelling: false, readerClosed: false));
        Assert.True(session.FetchFailed);
        Assert.True(session.StopResultSets);
    }

    [Fact]
    public void After_fault_ShouldCloseReaderAfterLoop_is_false()
    {
        var session = new LegacyNetezzaResultFetchSession();
        session.OnFetchFault(LegacyNetezzaFetchExceptionKind.DbFault, "type error");

        Assert.False(session.ShouldCloseReaderAfterLoop());
    }

    [Fact]
    public void Before_first_row_does_not_attach_and_fault_disposes_unattached()
    {
        var session = new LegacyNetezzaResultFetchSession();

        Assert.False(session.ShouldAttachTabForRow(0));
        Assert.False(session.TabAttached);

        session.OnFetchFault(LegacyNetezzaFetchExceptionKind.DbFault, "pg_atoi");

        Assert.Equal(
            LegacyNetezzaResultTabDisposition.DisposeUnattached,
            session.DecideTabDisposition(cancelling: false, weirdCancelEmptySchema: false, rowCount: 0));
    }

    [Fact]
    public void First_row_allows_attach_and_success_empty_keeps_tab_with_single_register()
    {
        var session = new LegacyNetezzaResultFetchSession();

        Assert.True(session.ShouldAttachTabForRow(1));
        session.MarkTabAttached();

        Assert.Equal(
            LegacyNetezzaResultTabDisposition.KeepAndInit,
            session.DecideTabDisposition(cancelling: false, weirdCancelEmptySchema: false, rowCount: 0));

        Assert.True(session.ShouldRegisterGridOnSuccess());
        Assert.False(session.ShouldRegisterGridOnSuccess());
    }

    [Fact]
    public void ShouldRegisterGridOnSuccess_is_false_until_success_path_even_after_first_row_attach()
    {
        var session = new LegacyNetezzaResultFetchSession();
        Assert.True(session.ShouldAttachTabForRow(1));
        session.MarkTabAttached();

        // Registration is deferred to success completion — not on first-row attach.
        Assert.False(session.GridRegistered);

        session.BeginResultSet();
        Assert.False(session.TabAttached);
        Assert.True(session.ShouldRegisterGridOnSuccess());
    }

    [Fact]
    public void PreferLogTab_only_when_not_successful()
    {
        Assert.True(LegacyNetezzaResultFetchSession.PreferLogTab(isSuccess: false));
        Assert.False(LegacyNetezzaResultFetchSession.PreferLogTab(isSuccess: true));
    }

    [Fact]
    public void Benign_NoData_SystemException_does_not_set_FetchFailed()
    {
        var session = new LegacyNetezzaResultFetchSession();
        var ex = new InvalidOperationException(LegacyNetezzaResultFetchSession.BenignNoDataMessage);

        // InvalidOperationException is SystemException
        Assert.Equal(LegacyNetezzaFetchExceptionKind.BenignNoData, LegacyNetezzaResultFetchSession.Classify(ex));

        session.OnFetchFault(ex);

        Assert.False(session.FetchFailed);
        Assert.True(session.SoftBreak);
        Assert.True(session.StopResultSets);
        Assert.Null(session.FailureMessage);
        Assert.Equal(
            LegacyNetezzaResultTabDisposition.DisposeUnattached,
            session.DecideTabDisposition(cancelling: false, weirdCancelEmptySchema: false, rowCount: 0));
        Assert.False(session.ShouldRegisterGridOnSuccess());
    }

    [Fact]
    public void SoftBreak_after_attach_discards_result_tab()
    {
        var session = new LegacyNetezzaResultFetchSession();
        session.MarkTabAttached();
        session.OnFetchFault(LegacyNetezzaFetchExceptionKind.BenignNoData, LegacyNetezzaResultFetchSession.BenignNoDataMessage);

        Assert.Equal(
            LegacyNetezzaResultTabDisposition.DiscardAttached,
            session.DecideTabDisposition(cancelling: false, weirdCancelEmptySchema: false, rowCount: 0));
    }

    [Fact]
    public void Classify_DbException_as_DbFault()
    {
        Assert.Equal(
            LegacyNetezzaFetchExceptionKind.DbFault,
            LegacyNetezzaResultFetchSession.Classify(new FakeDbException("nz error")));
    }

    [Fact]
    public void DiscardAttached_when_fault_after_tab_was_shown()
    {
        var session = new LegacyNetezzaResultFetchSession();
        session.MarkTabAttached();
        session.OnFetchFault(LegacyNetezzaFetchExceptionKind.SystemFault, "transport");

        Assert.Equal(
            LegacyNetezzaResultTabDisposition.DiscardAttached,
            session.DecideTabDisposition(cancelling: false, weirdCancelEmptySchema: false, rowCount: 0));
    }

    [Fact]
    public void BeginResultSet_resets_attach_and_register_but_keeps_stop_flags()
    {
        var session = new LegacyNetezzaResultFetchSession();
        session.OnFetchFault(LegacyNetezzaFetchExceptionKind.DbFault, "err");
        session.MarkTabAttached();
        Assert.False(session.ShouldRegisterGridOnSuccess()); // FetchFailed blocks register

        session.BeginResultSet();

        Assert.False(session.TabAttached);
        Assert.False(session.GridRegistered);
        Assert.True(session.FetchFailed);
        Assert.True(session.StopResultSets);
    }
}
