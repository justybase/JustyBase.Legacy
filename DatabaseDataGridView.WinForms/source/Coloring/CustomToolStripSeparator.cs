namespace DatabaseDataGridView.WinForms.Coloring;

public sealed class CustomToolStripSeparator : ToolStripSeparator
{
    private readonly bool _useSpecialColofing;
    private readonly Color _stripFore;
    private readonly Color _stripBack;

    public CustomToolStripSeparator(bool useSpecialColofing, Color stripFore, Color stripBack)
    {
        _useSpecialColofing = useSpecialColofing;
        _stripFore = stripFore;
        _stripBack = stripBack;
        Paint += CustomToolStripSeparator_Paint;
    }

    private SolidBrush? _brushBack;
    private Pen? _penFore;

    private void CustomToolStripSeparator_Paint(object? sender, PaintEventArgs e)
    {
        if (_useSpecialColofing && sender is ToolStripSeparator toolStripSeparator)
        {
            // Get the separator's width and height.
            int width = toolStripSeparator.Width;
            int height = toolStripSeparator.Height;

            _brushBack ??= new SolidBrush(_stripBack);
            _penFore ??= new Pen(_stripFore);
            // Fill the background.
            e.Graphics.FillRectangle(_brushBack, 0, 0, width, height);
            // Draw the line.
            e.Graphics.DrawLine(_penFore, 4, height / 2, width - 4, height / 2);
        }
    }
}
