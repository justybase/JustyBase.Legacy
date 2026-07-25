using FastColoredTextBoxNS;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Helpers;

public sealed class FctbDpiHelper : IFctbDpiHelper
{
    public static readonly FctbDpiHelper Default = new();

    public static void ApplyCharMetrics(FastColoredTextBox fctb)
        => Default.DoApplyCharMetrics(fctb);

    public void DoApplyCharMetrics(FastColoredTextBox fctb)
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

        ApplyCharMetricsCore(fctb);
    }

    void IFctbDpiHelper.ApplyCharMetrics(FastColoredTextBox fctb)
        => DoApplyCharMetrics(fctb);

    private static void OnHandleCreated(object? sender, EventArgs e)
    {
        if (sender is FastColoredTextBox fctb)
        {
            fctb.HandleCreated -= OnHandleCreated;
            ApplyCharMetricsCore(fctb);
        }
    }

    private static void ApplyCharMetricsCore(FastColoredTextBox fctb)
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
        fctb.Invalidate();
    }
}
