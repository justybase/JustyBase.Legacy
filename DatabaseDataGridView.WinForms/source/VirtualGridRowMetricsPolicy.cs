namespace DatabaseDataGridView.WinForms;

/// <summary>
/// Guards VirtualMode grids against per-row Height assignment, which unshares
/// every row and freezes/flickers the UI for large result sets.
/// </summary>
internal static class VirtualGridRowMetricsPolicy
{
    public const int MaxNonVirtualRowsToRetouch = 64;

    public static bool ShouldAssignIndividualRowHeights(bool virtualMode, int rowCount) =>
        !virtualMode && rowCount > 0 && rowCount <= MaxNonVirtualRowsToRetouch;
}
