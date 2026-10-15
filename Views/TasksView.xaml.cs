using System.Windows;
using System.Windows.Controls;
using SecurityLendingTool.Models;
using SecurityLendingTool.Tasks.Core;
using SecurityLendingTool.ViewModels;

namespace SecurityLendingTool.Views;

public partial class TasksView : UserControl
{
    private MessageHistoryWindow? _messageHistoryWindow;

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
            viewModel.ShowSLTaskHistoryDialog = ShowSLTaskHistoryDialog;
            viewModel.ShowMessageHistoryDialog = ShowMessageHistoryDialog;
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

    /// <summary>Shows the message history of one SLTask (a snapshot, refreshable in the window).</summary>
    private void ShowSLTaskHistoryDialog(ISLTask slTask)
    {
        SLTaskHistoryWindow dialog = new SLTaskHistoryWindow(slTask) { Owner = Window.GetWindow(this) };
        dialog.Show();
    }

    /// <summary>Shows the database Message History window; if it is already open, brings it to the front and refreshes it.</summary>
    private void ShowMessageHistoryDialog(string databasePath, int retentionMonths)
    {
        if (_messageHistoryWindow != null)
        {
            _messageHistoryWindow.Activate();
            if (_messageHistoryWindow.DataContext is MessageHistoryViewModel openViewModel)
            {
                openViewModel.RefreshCommand.Execute(null);
            }

            return;
        }

        MessageHistoryViewModel dialogViewModel = new MessageHistoryViewModel(databasePath, retentionMonths);
        _messageHistoryWindow = new MessageHistoryWindow(dialogViewModel) { Owner = Window.GetWindow(this) };
        _messageHistoryWindow.Closed += (sender, e) => _messageHistoryWindow = null;
        _messageHistoryWindow.Show();
    }

    /// <summary>Shows the 1-based row number in the row header (also correct for recycled rows).</summary>
    private void SLTasksGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }
}