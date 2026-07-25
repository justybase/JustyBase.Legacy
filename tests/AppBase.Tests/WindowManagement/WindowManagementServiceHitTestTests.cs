using AppBase.Common.WindowManagement;
using AppBase.Services;

namespace AppBase.Tests.WindowManagement;

public sealed class WindowManagementServiceHitTestTests
{
    private const int FW = 8;  // frameWidth
    private const int FH = 8;  // frameHeight
    private const int FO = 2;  // frameOffset (caption button reserve)
    private const int CT = 30; // captionTopHeight (_tMargins.cyTopHeight)
    private const int W = 200; // window width
    private const int H = 150; // window height

    // ── Corners ──

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 4)]
    [InlineData(7, 7)]
    [InlineData(0, 7)]
    [InlineData(7, 0)]
    public void HitTest_top_left_corner(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTTOPLEFT, result);
    }

    [Theory]
    [InlineData(W - 1, 0)]
    [InlineData(W - 4, 4)]
    [InlineData(W - FW, 7)]
    [InlineData(W - 1, 7)]
    public void HitTest_top_right_corner(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTTOPRIGHT, result);
    }

    [Theory]
    [InlineData(0, H - 1)]
    [InlineData(4, H - 4)]
    [InlineData(7, H - FW)]
    [InlineData(0, H - FW)]
    public void HitTest_bottom_left_corner(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTBOTTOMLEFT, result);
    }

    [Theory]
    [InlineData(W - 1, H - 1)]
    [InlineData(W - 4, H - 4)]
    [InlineData(W - FW, H - FW)]
    [InlineData(W - 1, H - FW)]
    public void HitTest_bottom_right_corner(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTBOTTOMRIGHT, result);
    }

    // ── Edges ──

    [Theory]
    [InlineData(FW + 10, 0)]
    [InlineData(FW + 10, 4)]
    [InlineData(FW + 10, FH - 1)]
    [InlineData(30, 4)]
    public void HitTest_top_edge(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTTOP, result);
    }

    [Theory]
    [InlineData(0, FH + 10)]
    [InlineData(4, FH + 20)]
    [InlineData(0, H - FH - 5)]
    [InlineData(FW / 2, FH + 10)]
    public void HitTest_left_edge(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTLEFT, result);
    }

    [Theory]
    [InlineData(W - 1, FH + 10)]
    [InlineData(W - FW / 2, FH + 20)]
    [InlineData(W - 1, H - FH - 5)]
    public void HitTest_right_edge(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTRIGHT, result);
    }

    [Theory]
    [InlineData(FW + 10, H - 1)]
    [InlineData(FW + 10, H - FH / 2)]
    [InlineData(FW + 10, H - FH)]
    [InlineData(W - FW - 10, H - 1)]
    public void HitTest_bottom_edge(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTBOTTOM, result);
    }

    // ── Caption ──

    [Theory]
    [InlineData(FW + 10, FH + 5)]
    [InlineData(FW + 10, FH + 15)]
    [InlineData(FW + 10, CT - 1)]
    [InlineData(50, FH + 10)]
    public void HitTest_caption(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTCAPTION, result);
    }

    // ── Client area (fallback) ──

    [Theory]
    [InlineData(FW + 10, CT + 10)]
    [InlineData(100, 80)]
    [InlineData(W - FW - 10, H - FH - 10)]
    public void HitTest_client(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTCLIENT, result);
    }

    // ── Caption top height = 0 (no glass) ──

    [Theory]
    [InlineData(FW + 10, 4)]
    [InlineData(50, 5)]
    public void HitTest_no_glass_caption(int x, int y)
    {
        // When captionTopHeight equals frameHeight (FH=8), caption zone is empty.
        // Points with y in [0, FH) fall to TOP region instead.
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, captionTopHeight: FH);
        Assert.Equal(HIT_CONSTANTS.HTTOP, result);
    }

    [Theory]
    [InlineData(FW + 10, FH + 1)]
    public void HitTest_no_glass_client(int x, int y)
    {
        var result = WindowManagementService.HitTest(
            x, y, W, H, FW, FH, FO, captionTopHeight: FH);
        Assert.Equal(HIT_CONSTANTS.HTCLIENT, result);
    }

    // ── Zero frame ──

    [Fact]
    public void HitTest_zero_frame_returns_client_for_interior()
    {
        var result = WindowManagementService.HitTest(
            50, 50, W, H, frameWidth: 0, frameHeight: 0, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTCLIENT, result);
    }

    [Fact]
    public void HitTest_zero_frame_top_left_origin_is_top_left()
    {
        var result = WindowManagementService.HitTest(
            0, 0, W, H, frameWidth: 0, frameHeight: 0, FO, CT);
        // With 0 frame, only caption region can be hit at origin
        Assert.Equal(HIT_CONSTANTS.HTCAPTION, result);
    }

    // ── Negative coordinates (cursor outside window) ──

    [Fact]
    public void HitTest_negative_coordinates_returns_client()
    {
        var result = WindowManagementService.HitTest(
            -5, -5, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTCLIENT, result);
    }

    [Fact]
    public void HitTest_beyond_window_returns_client()
    {
        var result = WindowManagementService.HitTest(
            W + 10, H + 10, W, H, FW, FH, FO, CT);
        Assert.Equal(HIT_CONSTANTS.HTCLIENT, result);
    }
}
