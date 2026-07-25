using FastColoredTextBoxNS;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Abstracts the SQL editor panel (currently SQLUpperPanel).
/// Enables swapping TabPage+SplitContainer layout for DockSuite DockContent later.
/// </summary>
public interface IEditorPanel
{
    /// <summary>The underlying FastColoredTextBox editor control.</summary>
    FastColoredTextBox CurrentTb { get; }

    /// <summary>Currently selected connection name for this editor.</summary>
    string SelectedConnectionName { get; set; }

    /// <summary>Currently selected database for this editor.</summary>
    string SelectedDatabase { get; set; }

    /// <summary>Whether the DB connection should be kept open across queries.</summary>
    bool KeepConnectionOpen { get; set; }

    /// <summary>Whether to continue execution after an error in script mode.</summary>
    bool ContinueOnError { get; set; }

    /// <summary>Enables or disables the connection/database combo boxes.</summary>
    void SetEnabledConnectionsDatabases(bool enabled);
}
