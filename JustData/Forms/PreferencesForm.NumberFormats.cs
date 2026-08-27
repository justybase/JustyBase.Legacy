using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public partial class PreferencesForm
{
    private const string CustomNumberFormatPresetName = "Custom / individual formats";

    private static readonly string[] IntegerFormatOptions =
    [
        "N0",
        "G",
        "D",
        "D8",
        "C0",
        "#,##0",
        "0000",
        "# 'degrees'"
    ];

    private static readonly string[] DecimalFormatOptions =
    [
        "N0",
        "N2",
        "N3",
        "N4",
        "F2",
        "F3",
        "F4",
        "G",
        "E",
        "E2",
        "C2",
        "P2",
        "0.00",
        "#,##0.00",
        "%#0.00",
        "#0.0e0",
        "# 'degrees'"
    ];

    private static readonly NumberFormatPreset[] NumberFormatPresets =
    [
        new("Default (N0 + N4)", "N0", "N4"),
        new("General (N0 + G)", "N0", "G"),
        new("Two decimals (N0 + N2)", "N0", "N2"),
        new("No grouping (G + G)", "G", "G"),
        new("Currency (C0 + C2)", "C0", "C2"),
        new("Scientific (G + E2)", "G", "E2"),
        new("Percent (N0 + P2)", "N0", "P2"),
        new(CustomNumberFormatPresetName, string.Empty, string.Empty)
    ];

    private ComboBox? _numberFormatPresetComboBox;
    private bool _numberFormatUpdateInProgress;

    private void InitializeNumberFormatControls()
    {
        PopulateFormatOptions(cbIntFormat, IntegerFormatOptions);
        PopulateFormatOptions(cbDecimalFormat, DecimalFormatOptions);

        cbIntFormat.DropDownStyle = ComboBoxStyle.DropDown;
        cbDecimalFormat.DropDownStyle = ComboBoxStyle.DropDown;
        cbIntFormat.TextChanged += NumberFormatValueChanged;
        cbDecimalFormat.TextChanged += NumberFormatValueChanged;
        cbIntFormat.SelectedIndexChanged += NumberFormatValueChanged;
        cbDecimalFormat.SelectedIndexChanged += NumberFormatValueChanged;
    }

    private static void PopulateFormatOptions(ComboBox comboBox, string[] options)
    {
        comboBox.BeginUpdate();
        try
        {
            comboBox.Items.Clear();
            foreach (string option in options)
            {
                comboBox.Items.Add(option);
            }
        }
        finally
        {
            comboBox.EndUpdate();
        }
    }

    private ComboBox CreateNumberFormatPresetSelector()
    {
        if (_numberFormatPresetComboBox is not null)
        {
            return _numberFormatPresetComboBox;
        }

        _numberFormatPresetComboBox = new ComboBox
        {
            Name = "numberFormatPresetComboBox",
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            Dock = DockStyle.Fill
        };

        foreach (NumberFormatPreset preset in NumberFormatPresets)
        {
            _numberFormatPresetComboBox.Items.Add(preset);
        }

        _numberFormatPresetComboBox.SelectedIndexChanged += NumberFormatPresetChanged;
        _numberFormatPresetComboBox.SelectedIndex = 0;
        return _numberFormatPresetComboBox;
    }

    private void NumberFormatPresetChanged(object? sender, EventArgs e)
    {
        if (_numberFormatUpdateInProgress
            || _numberFormatPresetComboBox?.SelectedItem is not NumberFormatPreset preset
            || preset.IsCustom)
        {
            return;
        }

        _numberFormatUpdateInProgress = true;
        try
        {
            cbIntFormat.Text = preset.IntegerFormat;
            cbDecimalFormat.Text = preset.DecimalFormat;
        }
        finally
        {
            _numberFormatUpdateInProgress = false;
        }
    }

    private void NumberFormatValueChanged(object? sender, EventArgs e)
    {
        if (_numberFormatUpdateInProgress)
        {
            return;
        }

        UpdateNumberFormatPresetSelection();
    }

    private void UpdateNumberFormatPresetSelection()
    {
        if (_numberFormatPresetComboBox is null)
        {
            return;
        }

        int matchingPresetIndex = Array.FindIndex(
            NumberFormatPresets,
            preset => string.Equals(preset.IntegerFormat, cbIntFormat.Text, StringComparison.Ordinal)
                && string.Equals(preset.DecimalFormat, cbDecimalFormat.Text, StringComparison.Ordinal));

        if (matchingPresetIndex < 0)
        {
            matchingPresetIndex = NumberFormatPresets.Length - 1;
        }

        if (_numberFormatPresetComboBox.SelectedIndex == matchingPresetIndex)
        {
            return;
        }

        _numberFormatUpdateInProgress = true;
        try
        {
            _numberFormatPresetComboBox.SelectedIndex = matchingPresetIndex;
        }
        finally
        {
            _numberFormatUpdateInProgress = false;
        }
    }

    private void NumberFormatPreviewButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new NumberFormatPreviewForm(cbIntFormat.Text, cbDecimalFormat.Text)
        {
            Font = Font
        };
        _colorize.ColorForm(dialog, force: true);
        dialog.ShowDialog(FindForm());
    }

    private sealed record NumberFormatPreset(string Name, string IntegerFormat, string DecimalFormat)
    {
        public bool IsCustom => string.Equals(Name, CustomNumberFormatPresetName, StringComparison.Ordinal);

        public override string ToString() => Name;
    }
}

internal sealed class NumberFormatPreviewForm : Form
{
    private const int IntegerSample = 1_234_567;
    private const decimal DecimalSample = 12_345.6789m;

    internal NumberFormatPreviewForm(string integerFormat, string decimalFormat)
    {
        Name = "numberFormatPreviewDialog";
        Text = "Number format preview";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var explanation = new Label
        {
            Name = "numberFormatPreviewDescription",
            AutoSize = true,
            Text = $"The result grid uses .NET numeric format strings and the current culture ({GetCultureName()}).",
            Margin = new Padding(0, 0, 0, 10)
        };
        root.Controls.Add(explanation, 0, 0);

        var values = new TableLayoutPanel
        {
            Name = "numberFormatPreviewValues",
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            AutoSize = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        values.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        values.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        values.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddCell(values, "Type", 0, 0, bold: true);
        AddCell(values, "Format", 1, 0, bold: true);
        AddCell(values, "Value → preview", 2, 0, bold: true);

        AddCell(values, "Integer", 0, 1);
        AddCell(values, DisplayFormat(integerFormat), 1, 1, "integerFormatPreviewFormat");
        AddCell(values, BuildPreview(IntegerSample, integerFormat), 2, 1, "integerFormatPreviewValue");

        AddCell(values, "Decimal", 0, 2);
        AddCell(values, DisplayFormat(decimalFormat), 1, 2, "decimalFormatPreviewFormat");
        AddCell(values, BuildPreview(DecimalSample, decimalFormat), 2, 2, "decimalFormatPreviewValue");
        root.Controls.Add(values, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0),
            Padding = Padding.Empty
        };
        var closeButton = new Button
        {
            Name = "closeNumberFormatPreviewButton",
            Text = "Close",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            UseVisualStyleBackColor = false
        };
        buttons.Controls.Add(closeButton);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private static void AddCell(TableLayoutPanel table, string text, int column, int row, string? name = null, bool bold = false)
    {
        var label = new Label
        {
            Name = name ?? string.Empty,
            AutoSize = true,
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 8, 3),
            TextAlign = ContentAlignment.MiddleLeft
        };
        if (bold)
        {
            label.Font = new Font(label.Font, FontStyle.Bold);
        }

        table.Controls.Add(label, column, row);
    }

    private static string BuildPreview(IFormattable value, string format)
    {
        string source = value.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        string result;
        try
        {
            result = value.ToString(format, CultureInfo.CurrentCulture) ?? string.Empty;
        }
        catch (FormatException)
        {
            result = "Invalid format string";
        }
        catch (ArgumentException)
        {
            result = "Invalid format string";
        }

        return $"{source}  →  {result}";
    }

    private static string DisplayFormat(string format) =>
        string.IsNullOrEmpty(format) ? "(empty / default)" : format;

    private static string GetCultureName() =>
        string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name) ? "Invariant" : CultureInfo.CurrentCulture.Name;
}
