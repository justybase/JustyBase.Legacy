using System.Drawing;

namespace AppBase.Common;

public static class DpiScale
{
    public const int DefaultDpi = 96;

    public static float Factor(int deviceDpi) => deviceDpi / (float)DefaultDpi;

    public static int Scale(int logicalPixels, int deviceDpi) =>
        (int)Math.Round(logicalPixels * Factor(deviceDpi));

    public static Size Scale(Size logicalSize, int deviceDpi) =>
        new(Scale(logicalSize.Width, deviceDpi), Scale(logicalSize.Height, deviceDpi));

    public static Point Scale(Point logicalPoint, int deviceDpi) =>
        new(Scale(logicalPoint.X, deviceDpi), Scale(logicalPoint.Y, deviceDpi));

    public static float Scale(float logicalValue, int deviceDpi) =>
        logicalValue * Factor(deviceDpi);
}
