namespace SecurityLendingTool.Behaviors;

/// <summary>
/// Implement on DataGrid row items that can be "empty" placeholder rows.
/// DataGridCellSelectionBehavior skips rows whose <see cref="IsEmpty"/> is true
/// when moving the selection with F3/F4.
/// </summary>
public interface IEmptyRow
{
    bool IsEmpty { get; }
}