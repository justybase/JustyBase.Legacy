using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public class VisualMarker
    {
        public Rectangle rectangle;

        public VisualMarker(Rectangle rectangle)
        {
            this.rectangle = rectangle;
        }

        public virtual void Draw(Graphics gr, Pen pen)
        {
        }

        public virtual Cursor Cursor
        {
            get { return Cursors.Hand; }
        }
    }

    public class CollapseFoldingMarker: VisualMarker
    {
        public readonly int iLine;

        public CollapseFoldingMarker(int iLine, Rectangle rectangle)
            : base(rectangle)
        {
            this.iLine = iLine;
        }

        public void Draw(Graphics gr, Pen pen, Brush backgroundBrush, Pen forePen)
        {
            //draw minus
            gr.FillRectangle(backgroundBrush, rectangle);
            gr.DrawRectangle(pen, rectangle);
            gr.DrawLine(forePen, rectangle.Left + 2, rectangle.Top + rectangle.Height / 2, rectangle.Right - 2, rectangle.Top + rectangle.Height / 2);
        }
    }

    public class ExpandFoldingMarker : VisualMarker
    {
        public readonly int iLine;

        public ExpandFoldingMarker(int iLine, Rectangle rectangle)
            : base(rectangle)
        {
            this.iLine = iLine;
        }

        public void Draw(Graphics gr, Pen pen,  Brush backgroundBrush, Pen forePen)
        {
            //draw plus
            gr.FillRectangle(backgroundBrush, rectangle);
            gr.DrawRectangle(pen, rectangle);
            gr.DrawLine(forePen, rectangle.Left + 2, rectangle.Top + rectangle.Height / 2, rectangle.Right - 2, rectangle.Top + rectangle.Height / 2);
            gr.DrawLine(forePen, rectangle.Left + rectangle.Width / 2, rectangle.Top + 2, rectangle.Left + rectangle.Width / 2, rectangle.Bottom - 2);
        }
    }

    public class FoldedAreaMarker : VisualMarker
    {
        public readonly int iLine;

        public FoldedAreaMarker(int iLine, Rectangle rectangle)
            : base(rectangle)
        {
            this.iLine = iLine;
        }

        public override void Draw(Graphics gr, Pen pen)
        {
            gr.DrawRectangle(pen, rectangle);
        }
    }

    public class StyleVisualMarker : VisualMarker
    {
        public Style Style{get;private set;}

        public StyleVisualMarker(Rectangle rectangle, Style style)
            : base(rectangle)
        {
            this.Style = style;
        }
    }

    public class LightbulbMarker : VisualMarker
    {
        public readonly int iLine;
        public bool IsHovered { get; set; }
        private readonly List<object> _actions = new();

        public IReadOnlyList<object> Actions => _actions;

        public LightbulbMarker(int iLine, Rectangle rectangle)
            : base(rectangle)
        {
            this.iLine = iLine;
        }

        public void SetActions<T>(IReadOnlyList<T> actions)
        {
            _actions.Clear();
            _actions.AddRange(actions!);
        }

        public override void Draw(Graphics gr, Pen pen)
        {
            int cx = rectangle.Left + rectangle.Width / 2;
            int cy = rectangle.Top + rectangle.Height / 2;
            int r = Math.Max(3, Math.Min(rectangle.Width, rectangle.Height) / 2 - 2);

            // A dark halo keeps the marker readable on both light and dark gutters.
            using var haloBrush = new SolidBrush(Color.FromArgb(210, 35, 35, 35));
            gr.FillEllipse(haloBrush, cx - r - 2, cy - r - 2, (r + 2) * 2, (r + 2) * 2);

            using var bgBrush = new SolidBrush(IsHovered
                ? Color.FromArgb(255, 235, 75)
                : Color.FromArgb(255, 205, 25));
            using var borderPen = new Pen(Color.FromArgb(255, 125, 90, 0), 1.2f);

            gr.FillEllipse(bgBrush, cx - r, cy - r, r * 2, r * 2);
            gr.DrawEllipse(borderPen, cx - r, cy - r, r * 2, r * 2);

            using var exBrush = new SolidBrush(Color.FromArgb(70, 50, 10));
            using var stemPen = new Pen(exBrush, Math.Max(1.5f, r / 4f));
            int arm = Math.Max(2, r / 3);
            gr.DrawLine(stemPen, cx, cy - arm, cx, cy + 1);
            gr.FillRectangle(exBrush, cx - Math.Max(2, r / 3), cy + arm - 1,
                Math.Max(4, r * 2 / 3), Math.Max(2, r / 4));
        }

        public override Cursor Cursor => Cursors.Hand;
    }

    public class VisualMarkerEventArgs : MouseEventArgs
    {
        public Style Style { get; private set; }
        public StyleVisualMarker Marker { get; private set; }

        public VisualMarkerEventArgs(Style style, StyleVisualMarker marker, MouseEventArgs args)
            : base(args.Button, args.Clicks, args.X, args.Y, args.Delta)
        {
            this.Style = style;
            this.Marker = marker;
        }
    }
}
