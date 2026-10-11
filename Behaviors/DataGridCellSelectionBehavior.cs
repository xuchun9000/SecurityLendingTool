using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecurityLendingTool.Behaviors;

/// <summary>
/// Attached behavior for a DataGrid with SelectionUnit="Cell".
/// <list type="bullet">
/// <item><c>IsEnabled</c>: syncs <c>SelectedItem</c> from the top-left selected cell and
/// enables F3 (move selection down one row) / F4 (up one row), wrapping at the ends.</item>
/// <item><c>SelectedItem</c>: two-way bindable row item of the top-left selected cell.
/// DataGrid.SelectedItem is never touched (it throws with SelectionUnit=Cell).</item>
/// </list>
/// Usage:
/// <code>
/// &lt;DataGrid SelectionUnit="Cell"
///           b:DataGridCellSelectionBehavior.IsEnabled="True"
///           b:DataGridCellSelectionBehavior.SelectedItem="{Binding SelectedPeople, Mode=TwoWay}" /&gt;
/// </code>
/// </summary>
public static class DataGridCellSelectionBehavior
{
    // ---------- IsEnabled ----------

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridCellSelectionBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    // ---------- SelectedItem ----------

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(DataGridCellSelectionBehavior),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged));

    public static object? GetSelectedItem(DependencyObject obj) => obj.GetValue(SelectedItemProperty);
    public static void SetSelectedItem(DependencyObject obj, object? value) => obj.SetValue(SelectedItemProperty, value);

    // Guards re-entrancy while the behavior itself writes SelectedItem.
    private static readonly DependencyPropertyKey IsUpdatingPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsUpdating",
            typeof(bool),
            typeof(DataGridCellSelectionBehavior),
            new PropertyMetadata(false));

    private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingPropertyKey.DependencyProperty);
    private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingPropertyKey, value);

    // ---------- Attach / detach ----------

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        // Always unsubscribe first so handlers are never attached twice.
        // Handlers are static (no captured target), so the DataGrid holds no reference
        // back to anything else — no leak.
        grid.SelectedCellsChanged -= OnSelectedCellsChanged;
        grid.PreviewKeyDown -= OnPreviewKeyDown;

        if (e.NewValue is true)
        {
            grid.SelectedCellsChanged += OnSelectedCellsChanged;
            grid.PreviewKeyDown += OnPreviewKeyDown;
            UpdateSelectedItem(grid);
        }
    }

    // ---------- Selection -> SelectedItem ----------

    private static void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (sender is DataGrid grid)
            UpdateSelectedItem(grid);
    }

    private static void UpdateSelectedItem(DataGrid grid)
    {
        object? item = GetTopLeftCell(grid, grid.SelectedCells)?.Item;
        if (ReferenceEquals(GetSelectedItem(grid), item))
            return;

        SetIsUpdating(grid, true);
        try
        {
            // SetCurrentValue keeps the TwoWay binding intact.
            grid.SetCurrentValue(SelectedItemProperty, item);
        }
        finally
        {
            SetIsUpdating(grid, false);
        }
    }

    // ---------- SelectedItem (from VM) -> selection ----------

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid || GetIsUpdating(grid) || !GetIsEnabled(grid))
            return;

        // Value pushed from the ViewModel: reflect it in the grid if it differs.
        if (ReferenceEquals(GetTopLeftCell(grid, grid.SelectedCells)?.Item, e.NewValue))
            return;

        grid.SelectedCells.Clear();
        if (e.NewValue is null || grid.Items.IndexOf(e.NewValue) < 0)
            return;

        var column = grid.Columns.Where(c => c.Visibility == Visibility.Visible)
                                 .OrderBy(c => c.DisplayIndex)
                                 .FirstOrDefault();
        if (column is null)
            return;

        var cell = new DataGridCellInfo(e.NewValue, column);
        grid.SelectedCells.Add(cell);
        grid.CurrentCell = cell;
        grid.ScrollIntoView(cell.Item, cell.Column);
    }

    // ---------- F3 / F4 ----------

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid || e.Key is not (Key.F3 or Key.F4))
            return;

        if (TryMoveSelection(grid, e.Key == Key.F3 ? 1 : -1))
            e.Handled = true;
    }

    /// <summary>
    /// Shift the whole cell selection by <paramref name="rowDelta"/> rows
    /// (F3 = +1 down, F4 = -1 up). The block wraps as a unit: past bottom -> top,
    /// past top -> bottom.
    /// </summary>
    private static bool TryMoveSelection(DataGrid grid, int rowDelta)
    {
        int count = grid.Items.Count;
        if (grid.SelectedCells.Count == 0 || count == 0)
            return false;

        var selected = grid.SelectedCells.ToList();
        int minRow = selected.Min(c => grid.Items.IndexOf(c.Item));
        int maxRow = selected.Max(c => grid.Items.IndexOf(c.Item));
        int height = maxRow - minRow + 1;

        int newMin = minRow + rowDelta;
        if (newMin < 0)
            newMin = count - height;
        else if (newMin + height > count)
            newMin = 0;

        var moved = new List<DataGridCellInfo>(selected.Count);
        foreach (var cell in selected)
        {
            int rowIndex = grid.Items.IndexOf(cell.Item);
            object newItem = grid.Items[newMin + (rowIndex - minRow)];
            moved.Add(new DataGridCellInfo(newItem, cell.Column));
        }

        grid.SelectedCells.Clear();
        foreach (var cell in moved)
            grid.SelectedCells.Add(cell);

        // Keep keyboard focus / current cell on the new top-left cell.
        var first = GetTopLeftCell(grid, moved)!.Value;
        grid.CurrentCell = first;
        grid.ScrollIntoView(first.Item, first.Column);
        grid.Focus();

        return true;
    }

    // ---------- Helpers ----------

    private static DataGridCellInfo? GetTopLeftCell(DataGrid grid, IEnumerable<DataGridCellInfo> cells)
    {
        var list = cells.ToList();
        if (list.Count == 0)
            return null;

        return list
            .OrderBy(c => grid.Items.IndexOf(c.Item))
            .ThenBy(c => c.Column?.DisplayIndex ?? int.MaxValue)
            .First();
    }
}
