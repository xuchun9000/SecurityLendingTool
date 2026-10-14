using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SecurityLendingTool.Behaviors;

/// <summary>
/// Attached behavior that aggregates the numeric values of the selected cells of a DataGrid
/// (works best with SelectionUnit="Cell" or "CellOrRowHeader").
/// <para>
/// Numeric = byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal
/// (and Nullable&lt;T&gt; of those). Non-numeric and empty (null) cells are ignored.
/// </para>
/// <list type="bullet">
/// <item><c>IsEnabled</c>: turns the behavior on.</item>
/// <item><c>Count</c>: number of numeric cells in the selection.</item>
/// <item><c>Sum</c>: sum of those values (0 when Count = 0).</item>
/// <item><c>Average</c>: Sum / Count (0 when Count = 0).</item>
/// <item><c>SelectedRows</c>: the distinct row items that have at least one selected cell,
/// in grid order. With SelectionUnit="Cell" every selected cell's row is included;
/// with "FullRow" it is simply the selected rows. The "new item" placeholder row is excluded.</item>
/// </list>
/// Count/Sum/Average/SelectedRows are written by the behavior; bind them to the ViewModel with
/// <c>Mode=OneWayToSource</c>, or read them directly with ElementName.
/// <code>
/// &lt;DataGrid x:Name="Grid" SelectionUnit="Cell"
///           b:DataGridSelectionStatsBehavior.IsEnabled="True"
///           b:DataGridSelectionStatsBehavior.Count="{Binding SelectionCount, Mode=OneWayToSource}"
///           b:DataGridSelectionStatsBehavior.Sum="{Binding SelectionSum, Mode=OneWayToSource}"
///           b:DataGridSelectionStatsBehavior.Average="{Binding SelectionAverage, Mode=OneWayToSource}"
///           b:DataGridSelectionStatsBehavior.SelectedRows="{Binding SelectedRows, Mode=OneWayToSource}" /&gt;
/// &lt;!-- or, without a ViewModel: --&gt;
/// &lt;TextBlock Text="{Binding (b:DataGridSelectionStatsBehavior.Sum), ElementName=Grid}" /&gt;
/// </code>
/// </summary>
public static class DataGridSelectionStatsBehavior
{
    // ---------- IsEnabled ----------

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridSelectionStatsBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    // ---------- Count / Sum / Average (written by the behavior) ----------

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.RegisterAttached(
            "Count",
            typeof(int),
            typeof(DataGridSelectionStatsBehavior),
            new PropertyMetadata(0));

    public static int GetCount(DependencyObject obj) => (int)obj.GetValue(CountProperty);
    public static void SetCount(DependencyObject obj, int value) => obj.SetValue(CountProperty, value);

    public static readonly DependencyProperty SumProperty =
        DependencyProperty.RegisterAttached(
            "Sum",
            typeof(double),
            typeof(DataGridSelectionStatsBehavior),
            new PropertyMetadata(0d));

    public static double GetSum(DependencyObject obj) => (double)obj.GetValue(SumProperty);
    public static void SetSum(DependencyObject obj, double value) => obj.SetValue(SumProperty, value);

    public static readonly DependencyProperty AverageProperty =
        DependencyProperty.RegisterAttached(
            "Average",
            typeof(double),
            typeof(DataGridSelectionStatsBehavior),
            new PropertyMetadata(0d));

    public static double GetAverage(DependencyObject obj) => (double)obj.GetValue(AverageProperty);
    public static void SetAverage(DependencyObject obj, double value) => obj.SetValue(AverageProperty, value);

    // ---------- SelectedRows (written by the behavior) ----------

    /// <summary>
    /// Distinct row items touched by the current selection, in grid order.
    /// A new list instance is published whenever the set of rows changes, so a
    /// OneWayToSource binding always sees the change.
    /// </summary>
    public static readonly DependencyProperty SelectedRowsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedRows",
            typeof(IReadOnlyList<object>),
            typeof(DataGridSelectionStatsBehavior),
            new PropertyMetadata(Array.Empty<object>()));

    public static IReadOnlyList<object> GetSelectedRows(DependencyObject obj) => (IReadOnlyList<object>)obj.GetValue(SelectedRowsProperty);
    public static void SetSelectedRows(DependencyObject obj, IReadOnlyList<object> value) => obj.SetValue(SelectedRowsProperty, value);

    // ---------- Attach / detach ----------

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        grid.SelectedCellsChanged -= OnSelectedCellsChanged;

        if (e.NewValue is true)
        {
            grid.SelectedCellsChanged += OnSelectedCellsChanged;
            Recalculate(grid);
            UpdateSelectedRows(grid);
        }
        else
        {
            Publish(grid, 0, 0d, 0d);
            grid.SetCurrentValue(SelectedRowsProperty, Array.Empty<object>());
        }
    }

    private static void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            Recalculate(grid);
            UpdateSelectedRows(grid);
        }
    }

    // ---------- Selected rows ----------

    private static void UpdateSelectedRows(DataGrid grid)
    {
        // Row items that own at least one selected cell.
        HashSet<object> touched = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (DataGridCellInfo cell in grid.SelectedCells)
        {
            if (cell.Item is not null && cell.Item != CollectionView.NewItemPlaceholder)
            {
                touched.Add(cell.Item);
            }
        }

        // Walk the grid once so the result follows the displayed row order.
        List<object> rows = new List<object>(touched.Count);
        if (touched.Count > 0)
        {
            foreach (object item in grid.Items)
            {
                if (touched.Remove(item))
                {
                    rows.Add(item);
                    if (touched.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        // Only publish when the set of rows actually changed.
        IReadOnlyList<object> current = GetSelectedRows(grid);
        if (SameRows(current, rows))
        {
            return;
        }

        grid.SetCurrentValue(SelectedRowsProperty, rows.AsReadOnly());
    }

    private static bool SameRows(IReadOnlyList<object>? a, List<object> b)
    {
        if (a is null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < b.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]))
            {
                return false;
            }
        }

        return true;
    }

    // ---------- Aggregation ----------

    private static void Recalculate(DataGrid grid)
    {
        int count = 0;
        // Accumulate in decimal for exact results (e.g. 0.1 + 0.2), fall back to double
        // if a value is out of decimal range (huge doubles, NaN, Infinity).
        decimal decimalSum = 0m;
        double doubleSum = 0d;
        bool useDouble = false;

        // One evaluator reused for every cell in this pass.
        BindingEvaluator evaluator = new BindingEvaluator();

        foreach (DataGridCellInfo cell in grid.SelectedCells)
        {
            if (!cell.IsValid || cell.Item is null || cell.Column is null)
            {
                continue;
            }

            object? value = GetCellValue(evaluator, cell.Item, cell.Column);
            if (!TryGetNumber(value, out decimal dec, out double dbl, out bool isDecimalSafe))
            {
                continue;
            }

            count++;
            doubleSum += dbl;

            if (!useDouble)
            {
                if (isDecimalSafe)
                {
                    try { decimalSum += dec; }
                    catch (OverflowException) { useDouble = true; }
                }
                else
                {
                    useDouble = true;
                }
            }
        }

        double sum = count == 0 ? 0d : useDouble ? doubleSum : (double)decimalSum;
        double average = count == 0 ? 0d : useDouble ? doubleSum / count : (double)(decimalSum / count);

        Publish(grid, count, sum, average);
    }

    private static void Publish(DataGrid grid, int count, double sum, double average)
    {
        // SetCurrentValue keeps any OneWayToSource / TwoWay binding on the property intact.
        grid.SetCurrentValue(CountProperty, count);
        grid.SetCurrentValue(SumProperty, sum);
        grid.SetCurrentValue(AverageProperty, average);
    }

    // ---------- Reading the raw (typed) cell value ----------

    /// <summary>
    /// Reads the underlying, un-formatted value of a cell by re-evaluating the column's own
    /// binding path against the row item. This returns the real CLR value (int, double, ...),
    /// not the displayed text, and works for virtualized rows that have no generated cell.
    /// </summary>
    private static object? GetCellValue(BindingEvaluator evaluator, object item, DataGridColumn column)
    {
        // DataGridTextColumn/CheckBox/ComboBox -> Binding; template columns -> ClipboardContentBinding
        // or SortMemberPath if the developer set one.
        PropertyPath? path =
            ((column as DataGridBoundColumn)?.Binding as Binding)?.Path
            ?? (column.ClipboardContentBinding as Binding)?.Path
            ?? (string.IsNullOrEmpty(column.SortMemberPath) ? null : new PropertyPath(column.SortMemberPath));

        if (path is null || string.IsNullOrEmpty(path.Path))
        {
            return null;
        }

        return evaluator.Evaluate(item, path);
    }

    private static bool TryGetNumber(object? value, out decimal dec, out double dbl, out bool isDecimalSafe)
    {
        dec = 0m;
        dbl = 0d;
        isDecimalSafe = true;

        // A boxed Nullable<T> is either null or a boxed T, so this also covers int?, double?, ...
        switch (value)
        {
            case byte v: dec = v; dbl = v; return true;
            case sbyte v: dec = v; dbl = v; return true;
            case short v: dec = v; dbl = v; return true;
            case ushort v: dec = v; dbl = v; return true;
            case int v: dec = v; dbl = v; return true;
            case uint v: dec = v; dbl = v; return true;
            case long v: dec = v; dbl = v; return true;
            case ulong v: dec = v; dbl = v; return true;
            case decimal v: dec = v; dbl = (double)v; return true;
            case float v: return FromDouble(v, out dec, out dbl, out isDecimalSafe);
            case double v: return FromDouble(v, out dec, out dbl, out isDecimalSafe);
            default: return false;
        }
    }

    private static bool FromDouble(double v, out decimal dec, out double dbl, out bool isDecimalSafe)
    {
        dbl = v;
        dec = 0m;
        isDecimalSafe = false;

        if (double.IsNaN(v) || double.IsInfinity(v))
        {
            return true;
        }

        try
        {
            dec = (decimal)v;
            isDecimalSafe = true;
        }
        catch (OverflowException)
        {
        }
        return true;
    }

    /// <summary>
    /// Tiny helper that lets WPF's binding engine resolve a property path
    /// (nested paths, indexers, INotifyPropertyChanged, etc.) against an object.
    /// </summary>
    private sealed class BindingEvaluator : DependencyObject
    {
        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(BindingEvaluator));

        public object? Evaluate(object source, PropertyPath path)
        {
            // Only the path is reused (no Converter/StringFormat), so we get the raw CLR value.
            Binding binding = new Binding { Path = path, Source = source, Mode = BindingMode.OneTime };
            BindingOperations.SetBinding(this, ValueProperty, binding);
            object? value = GetValue(ValueProperty);
            BindingOperations.ClearBinding(this, ValueProperty);
            return value == DependencyProperty.UnsetValue ? null : value;
        }
    }
}
