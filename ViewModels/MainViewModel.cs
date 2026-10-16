using CommunityToolkit.Mvvm.ComponentModel;

namespace SecurityLendingTool.ViewModels;

/// <summary>
/// Application-level ViewModel for the main window (tabs, shell concerns).
/// Owns tab ViewModels so views reach them through MainViewModel.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public TasksViewModel SLTasksTab { get; } = new();
}
