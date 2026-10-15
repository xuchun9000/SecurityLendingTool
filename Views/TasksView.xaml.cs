using System.Windows.Controls;

namespace SecurityLendingTool.Views;

public partial class TasksView : UserControl
{
    public TasksView()
    {
        InitializeComponent();
    }

    /// <summary>Shows the 1-based row number in the row header (also correct for recycled rows).</summary>
    private void SLTasksGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }
}