using System.Windows;
using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Views;

/// <summary>Lists every message an SLTask has reported, oldest first, grouped by attempt. Refresh re-reads the history.</summary>
public partial class SLTaskHistoryWindow : Window
{
    private readonly ISLTask _slTask;

    public SLTaskHistoryWindow(ISLTask slTask)
    {
        InitializeComponent();
        _slTask = slTask;
        Title = $"Message History - {slTask.Name}";
        LoadHistory();
    }

    private void LoadHistory()
    {
        IReadOnlyList<SLTaskMessage> history = _slTask.GetMessageHistory();
        List<SLTaskHistoryRow> rows = new List<SLTaskHistoryRow>(history.Count);
        int attempt = 0;
        foreach (SLTaskMessage message in history)
        {
            // Every Start() begins with Started, or with PreconditionNotMet when it is skipped.
            bool startsAttempt = message.Event is SLTaskProgressEvent.Started or SLTaskProgressEvent.PreconditionNotMet;
            if (startsAttempt || attempt == 0)
            {
                attempt++;
            }

            rows.Add(new SLTaskHistoryRow(attempt, startsAttempt && attempt > 1, message));
        }

        HistoryGrid.ItemsSource = rows;
        CountText.Text = $"{rows.Count} messages, {attempt} attempts";
        if (rows.Count > 0)
        {
            HistoryGrid.ScrollIntoView(rows[rows.Count - 1]);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
