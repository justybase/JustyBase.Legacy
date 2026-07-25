using System;
using System.Drawing;
using System.Windows.Forms;
using AppBase.Common;

namespace JustyBaseLegacy.UI.Helpers;

internal static class ResultsToolbarMetrics
{
    public static int Height(int dpi) => DpiScale.Scale(28, dpi);

    public static void Layout(Control parent, Button btAbort, ProgressBar? progressBar, Control? logView, int dpi)
    {
        int toolbarHeight = Height(dpi);
        int margin = DpiScale.Scale(4, dpi);

        btAbort.AutoSize = true;
        btAbort.MinimumSize = new Size(DpiScale.Scale(72, dpi), toolbarHeight);
        btAbort.Height = toolbarHeight;
        btAbort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btAbort.Location = new Point(Math.Max(0, parent.ClientSize.Width - btAbort.Width - margin), 0);

        if (progressBar is not null)
        {
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(0, 0);
            progressBar.Height = toolbarHeight;
            progressBar.Width = Math.Max(DpiScale.Scale(100, dpi), btAbort.Left - margin);
        }

        if (logView is not null)
        {
            logView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            logView.Location = new Point(0, toolbarHeight + margin);
            logView.Width = parent.ClientSize.Width;
            logView.Height = Math.Max(0, parent.ClientSize.Height - toolbarHeight - margin);
        }
    }
}
