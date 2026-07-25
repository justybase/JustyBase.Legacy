using System.Drawing;

namespace JustyBaseLegacy.UI.Helpers;

internal static class SnapLayoutHelper
{
    public static bool IsWindows11OrGreater =>
        Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

    public static Point GetScreenPointFromLParam(IntPtr lParam)
    {
        long lp = lParam.ToInt64();
        return new Point(
            (short)(lp & 0xFFFF),
            (short)((lp >> 16) & 0xFFFF));
    }
}
