using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls
{
    public partial class BlurOverlay : Form
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static partial nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        public BlurOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;

            // Make the form semi-transparent
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Set window as layered for transparency
            nint exStyle = GetWindowLongPtr(Handle, GWL_EXSTYLE);
            SetWindowLongPtr(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
            SetLayeredWindowAttributes(Handle, 0, 120, LWA_ALPHA); // 120/255 = ~47% opacity
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics g = e.Graphics;

            // Create a gradient overlay effect
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(80, 0, 0, 0),
                Color.FromArgb(120, 0, 0, 0),
                45f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Add some subtle pattern
            using (HatchBrush hatchBrush = new HatchBrush(
                HatchStyle.DottedDiamond,
                Color.FromArgb(10, 255, 255, 255),
                Color.Transparent))
            {
                g.FillRectangle(hatchBrush, ClientRectangle);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            // Close overlay when clicked
            Close();
            base.OnClick(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Close on Escape key
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
