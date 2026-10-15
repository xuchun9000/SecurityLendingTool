using System.Windows;
using Microsoft.Win32;
using SecurityLendingTool.ViewModels;

namespace SecurityLendingTool.Views;

/// <summary>Database-backed Message History: every SLTask message saved by SLTaskMessageStore, by day.</summary>
public partial class MessageHistoryWindow : Window
{
    private readonly MessageHistoryViewModel _viewModel;

    public MessageHistoryWindow(MessageHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.AskExportPath = AskExportPath;
        DataContext = _viewModel;
        Loaded += MessageHistoryWindow_Loaded;
    }

    private async void MessageHistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshCommand.ExecuteAsync(null);
    }

    private string? AskExportPath(string suggestedName)
    {
        SaveFileDialog dialog = new SaveFileDialog
        {
            FileName = suggestedName,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void DayTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is MessageHistoryTreeNode node && node.Kind == MessageHistoryNodeKind.Day)
        {
            _viewModel.SelectedDay = node;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
