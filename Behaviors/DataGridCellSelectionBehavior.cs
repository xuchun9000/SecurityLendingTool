using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SecurityLendingTool.Behaviors;

/// <summary>
/// Attached behavior for a DataGrid with SelectionUnit="Cell".
/// <list type="bullet">
/// <item><c>IsEnabled</c>: syncs <c>SelectedItem</c> from the top-left selected cell and
/// enables F3 (move selection down one data row) / F4 (up one data row), wrapping at the ends.
/// Rows implementing <see cref="IEmptyRow"/> with IsEmpty = true are skipped.</item>
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
    /// Shift the whole cell selection by <paramref name="rowDelta"/> data rows
    /// (F3 = +1 down, F4 = -1 up). The block wraps as a unit: past the last data row -> first,
    /// past the first -> last.
    /// <para>
    /// Rows that are not real data are skipped:
    /// the "new item" placeholder row (DataGrid with CanUserAddRows=True), and items that
    /// implement <see cref="IEmptyRow"/> with <c>IsEmpty = true</c>. Grids whose items don't
    /// implement <see cref="IEmptyRow"/> simply treat every real row as a data row.
    /// </para>
    /// Selected cells that sit on skipped rows are dropped; if only skipped rows are selected,
    /// the selection jumps to the next/previous data row.
    /// </summary>
    private static bool TryMoveSelection(DataGrid grid, int rowDelta)
    {
        if (grid.SelectedCells.Count == 0 || grid.Items.Count == 0)
            return false;

        // Finish any in-progress cell/row edit first; changing the selection or current cell
        // in the middle of an edit (or an AddNew transaction) throws.
        if (!grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
            return false;

        // Grid row index of every item (reference identity, so Equals overrides can't collide),
        // and the ordered list of rows that hold data.
        var rowIndex = new Dictionary<object, int>(grid.Items.Count, ReferenceEqualityComparer.Instance);
        var dataRows = new List<int>();
        for (int i = 0; i < grid.Items.Count; i++)
        {
            object item = grid.Items[i];
            rowIndex.TryAdd(item, i);
            if (IsDataRow(item))
                dataRows.Add(i);
        }

        if (dataRows.Count == 0)
            return false;

        // Map grid row index -> position within dataRows.
        var dataPos = new Dictionary<int, int>(dataRows.Count);
        for (int p = 0; p < dataRows.Count; p++)
            dataPos[dataRows[p]] = p;

        int RowOf(DataGridCellInfo c) =>
            c.Item is not null && rowIndex.TryGetValue(c.Item, out int r) ? r : -1;

        var selected = grid.SelectedCells.Where(c => c.Column is not null).ToList();
        if (selected.Count == 0)
            return false;

        var onData = selected
            .Select(c => (Cell: c, Row: RowOf(c)))
            .Where(x => dataPos.ContainsKey(x.Row))
            .ToList();

        var moved = new List<DataGridCellInfo>(selected.Count);

        if (onData.Count > 0)
        {
            // Treat the selection as a block in "data row" space and shift it by one data row.
            int minPos = onData.Min(x => dataPos[x.Row]);
            int maxPos = onData.Max(x => dataPos[x.Row]);
            int height = maxPos - minPos + 1;

            int newMin = minPos + rowDelta;
            if (newMin < 0)
                newMin = dataRows.Count - height;
            else if (newMin + height > dataRows.Count)
                newMin = 0;

            foreach (var (cell, row) in onData)
            {
                int targetRow = dataRows[newMin + (dataPos[row] - minPos)];
                moved.Add(new DataGridCellInfo(grid.Items[targetRow], cell.Column));
            }
        }
        else
        {
            // Only skipped rows selected: jump to the nearest data row in the move direction,
            // keeping the selected columns.
            int anchor = selected.Select(RowOf).Where(r => r >= 0).DefaultIfEmpty(-1).Min();
            int targetRow = rowDelta > 0
                ? dataRows.FirstOrDefault(r => r > anchor, dataRows[0])
                : dataRows.LastOrDefault(r => r < anchor, dataRows[^1]);

            object targetItem = grid.Items[targetRow];
            foreach (var column in selected.Select(c => c.Column).Distinct())
                moved.Add(new DataGridCellInfo(targetItem, column));
        }

        grid.SelectedCells.Clear();
        foreach (var cell in moved)
            grid.SelectedCells.Add(cell);

        // Keep keyboard focus / current cell on the new top-left cell (always a data row here).
        var first = GetTopLeftCell(grid, moved)!.Value;
        grid.CurrentCell = first;
        grid.ScrollIntoView(first.Item, first.Column);
        grid.Focus();

        return true;
    }

    /// <summary>
    /// A row is a data row unless it is the DataGrid's "new item" placeholder
    /// or an <see cref="IEmptyRow"/> that reports itself empty.
    /// </summary>
    private static bool IsDataRow(object? item) =>
        item is not null
        && item != CollectionView.NewItemPlaceholder
        && item is not IEmptyRow { IsEmpty: true };

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
