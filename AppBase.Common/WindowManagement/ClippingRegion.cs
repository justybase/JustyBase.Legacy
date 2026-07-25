namespace AppBase.Common.WindowManagement;

/// <summary>
/// Provides functionality for creating and managing clipping regions for graphics operations.
/// </summary>
public partial class ClippingRegion : IDisposable
{
    #region Fields
    private IntPtr _hClipRegion;
    private IntPtr _hDc;
    private bool _disposed = false;
    #endregion

    #region Constructors

    public ClippingRegion(IntPtr hdc, Rectangle cliprect, Rectangle canvasrect)
    {
        CreateRectangleClip(hdc, cliprect, canvasrect);
    }

    public ClippingRegion(IntPtr hdc, RECT cliprect, RECT canvasrect)
    {
        CreateRectangleClip(hdc, cliprect, canvasrect);
    }

    public ClippingRegion(IntPtr hdc, Rectangle cliprect, Rectangle canvasrect, uint radius)
    {
        CreateRoundedRectangleClip(hdc, cliprect, canvasrect, radius);
    }

    public ClippingRegion(IntPtr hdc, RECT cliprect, RECT canvasrect, uint radius)
    {
        CreateRoundedRectangleClip(hdc, cliprect, canvasrect, radius);
    }

    #endregion

    #region Methods

    public void CreateRectangleClip(IntPtr hdc, Rectangle cliprect, Rectangle canvasrect)
    {
        _hDc = hdc;
        IntPtr clip = WindowNativeMethods.CreateRectRgn(cliprect.Left, cliprect.Top, cliprect.Right, cliprect.Bottom);
        IntPtr canvas = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        _hClipRegion = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        WindowNativeMethods.CombineRgn(_hClipRegion, canvas, clip, CombineRgnStyles.RGN_DIFF);
        WindowNativeMethods.SelectClipRgn(_hDc, _hClipRegion);
        WindowNativeMethods.DeleteObject(clip);
        WindowNativeMethods.DeleteObject(canvas);
    }

    public void CreateRectangleClip(IntPtr hdc, RECT cliprect, RECT canvasrect)
    {
        _hDc = hdc;
        IntPtr clip = WindowNativeMethods.CreateRectRgn(cliprect.Left, cliprect.Top, cliprect.Right, cliprect.Bottom);
        IntPtr canvas = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        _hClipRegion = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        WindowNativeMethods.CombineRgn(_hClipRegion, canvas, clip, CombineRgnStyles.RGN_DIFF);
        WindowNativeMethods.SelectClipRgn(_hDc, _hClipRegion);
        WindowNativeMethods.DeleteObject(clip);
        WindowNativeMethods.DeleteObject(canvas);
    }

    public void CreateRoundedRectangleClip(IntPtr hdc, Rectangle cliprect, Rectangle canvasrect, uint radius)
    {
        int r = (int)radius;
        _hDc = hdc;
        // create rounded regions
        IntPtr clip = WindowNativeMethods.CreateRoundRectRgn(cliprect.Left, cliprect.Top, cliprect.Right, cliprect.Bottom, r, r);
        IntPtr canvas = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        _hClipRegion = WindowNativeMethods.CreateRoundRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom, r, r);
        WindowNativeMethods.CombineRgn(_hClipRegion, canvas, clip, CombineRgnStyles.RGN_DIFF);
        // add it in
        WindowNativeMethods.SelectClipRgn(_hDc, _hClipRegion);
        WindowNativeMethods.DeleteObject(clip);
        WindowNativeMethods.DeleteObject(canvas);
    }

    public void CreateRoundedRectangleClip(IntPtr hdc, RECT cliprect, RECT canvasrect, uint radius)
    {
        int r = (int)radius;
        _hDc = hdc;
        // create rounded regions
        IntPtr clip = WindowNativeMethods.CreateRoundRectRgn(cliprect.Left, cliprect.Top, cliprect.Right, cliprect.Bottom, r, r);
        IntPtr canvas = WindowNativeMethods.CreateRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom);
        _hClipRegion = WindowNativeMethods.CreateRoundRectRgn(canvasrect.Left, canvasrect.Top, canvasrect.Right, canvasrect.Bottom, r, r);
        WindowNativeMethods.CombineRgn(_hClipRegion, canvas, clip, CombineRgnStyles.RGN_DIFF);
        // add it in
        WindowNativeMethods.SelectClipRgn(_hDc, _hClipRegion);
        WindowNativeMethods.DeleteObject(clip);
        WindowNativeMethods.DeleteObject(canvas);
    }

    public void Release()
    {
        if (_hClipRegion != IntPtr.Zero)
        {
            // remove region
            WindowNativeMethods.SelectClipRgn(_hDc, IntPtr.Zero);
            // delete region
            WindowNativeMethods.DeleteObject(_hClipRegion);
            _hClipRegion = IntPtr.Zero;
        }
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources if any
            }

            // Dispose unmanaged resources
            Release();
            _disposed = true;
        }
    }

    ~ClippingRegion()
    {
        Dispose(false);
    }

    #endregion
}
