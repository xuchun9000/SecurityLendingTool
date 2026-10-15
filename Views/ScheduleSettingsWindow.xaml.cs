using System.Windows;
using SecurityLendingTool.ViewModels;

namespace SecurityLendingTool.Views;

public partial class ScheduleSettingsWindow : Window
{
    public ScheduleSettingsWindow(ScheduleSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += ViewModel_CloseRequested;
    }

    private void ViewModel_CloseRequested(object? sender, bool isOk)
    {
        DialogResult = isOk;
    }
}
