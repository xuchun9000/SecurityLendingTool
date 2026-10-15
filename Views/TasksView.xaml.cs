using System.Windows;
using System.Windows.Controls;
using SecurityLendingTool.Models;
using SecurityLendingTool.ViewModels;

namespace SecurityLendingTool.Views;

public partial class TasksView : UserControl
{
    public TasksView()
    {
        InitializeComponent();
        DataContextChanged += TasksView_DataContextChanged;
    }

    private void TasksView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is TasksViewModel viewModel)
        {
            viewModel.ShowScheduleDialog = ShowScheduleDialog;
        }
    }

    /// <summary>Shows the schedule dialog centered on the main window; returns null on Cancel.</summary>
    private SLScheduleSettings? ShowScheduleDialog(SLScheduleSettings current)
    {
        ScheduleSettingsViewModel dialogViewModel = new ScheduleSettingsViewModel(current);
        ScheduleSettingsWindow dialog = new ScheduleSettingsWindow(dialogViewModel) { Owner = Window.GetWindow(this) };
        bool? isOk = dialog.ShowDialog();
        return isOk == true ? dialogViewModel.Result : null;
    }

    /// <summary>Shows the 1-based row number in the row header (also correct for recycled rows).</summary>
    private void SLTasksGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }
}