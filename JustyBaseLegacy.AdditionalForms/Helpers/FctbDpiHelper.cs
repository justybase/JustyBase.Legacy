using AppBase.Common;
using FastColoredTextBoxNS;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

internal static class FctbDpiHelper
{
    public static void ApplyCharMetrics(FastColoredTextBox fctb, int paddingLogical = 10)
    {
        if (fctb?.Font == null)
        {
            return;
        }

        if (!fctb.IsHandleCreated)
        {
            fctb.HandleCreated += OnHandleCreated;
            return;
        }

        ApplyCharMetricsCore(fctb, paddingLogical);
    }

    private static void OnHandleCreated(object? sender, EventArgs e)
    {
        if (sender is FastColoredTextBox fctb)
        {
            fctb.HandleCreated -= OnHandleCreated;
            ApplyCharMetricsCore(fctb, 10);
        }
    }

    private static void ApplyCharMetricsCore(FastColoredTextBox fctb, int paddingLogical)
    {
        using Graphics graphics = fctb.CreateGraphics();
        Size charSize = TextRenderer.MeasureText(
            graphics,
            "W",
            fctb.Font,
            new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding);

        fctb.CharWidth = Math.Max(1, charSize.Width);
        fctb.CharHeight = Math.Max(1, (int)Math.Ceiling(fctb.Font.GetHeight(graphics)));
        fctb.Paddings = new Padding(DpiScale.Scale(paddingLogical, fctb.DeviceDpi));
        fctb.Invalidate();
    }
}
