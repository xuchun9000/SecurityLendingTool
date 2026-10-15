using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.ViewModels;

/// <summary>ViewModel for the schedule dialog: start/stop time as HH:mm text.</summary>
public partial class ScheduleSettingsViewModel : ObservableObject
{
    private const string TimeFormat = @"hh\:mm";

    public ScheduleSettingsViewModel(SLScheduleSettings settings)
    {
        _StartTimeText = settings.StartTime.ToString(TimeFormat, CultureInfo.InvariantCulture);
        _StopTimeText = settings.StopTime.ToString(TimeFormat, CultureInfo.InvariantCulture);
        _RetryIntervalText = settings.RetryIntervalSeconds.ToString(CultureInfo.InvariantCulture);
    }

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OkCommand))] [NotifyPropertyChangedFor(nameof(ErrorText))] private string _StartTimeText;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OkCommand))] [NotifyPropertyChangedFor(nameof(ErrorText))] private string _StopTimeText;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OkCommand))] [NotifyPropertyChangedFor(nameof(ErrorText))] private string _RetryIntervalText;

    /// <summary>Raised with true (OK) or false (Cancel); the window closes itself.</summary>
    public event EventHandler<bool>? CloseRequested;

    public SLScheduleSettings? Result { get; private set; }

    public string ErrorText
    {
        get
        {
            if (!TryParse(StartTimeText, out TimeSpan start))
            {
                return "Start time must be HH:mm (00:00 - 23:59)";
            }

            if (!TryParse(StopTimeText, out TimeSpan stop))
            {
                return "Stop time must be HH:mm (00:00 - 23:59)";
            }

            if (!TryParseRetry(RetryIntervalText, out int _))
            {
                return "Retry interval must be a whole number of seconds, at least 5";
            }

            if (start == stop)
            {
                return "Start and stop time must differ";
            }

            if (stop < start)
            {
                return "Note: overnight window (stops next day)";
            }

            return string.Empty;
        }
    }

    private static bool TryParse(string? text, out TimeSpan value)
    {
        return TimeSpan.TryParseExact(text?.Trim(), new[] { @"hh\:mm", @"h\:mm" }, CultureInfo.InvariantCulture, out value) && value < TimeSpan.FromDays(1);
    }

    private static bool TryParseRetry(string? text, out int seconds)
    {
        return int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) && seconds >= 5;
    }

    private bool CanOk()
    {
        return TryParseRetry(RetryIntervalText, out int _) && TryParse(StartTimeText, out TimeSpan start) && TryParse(StopTimeText, out TimeSpan stop) && start != stop;
    }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok()
    {
        TryParse(StartTimeText, out TimeSpan start);
        TryParse(StopTimeText, out TimeSpan stop);
        TryParseRetry(RetryIntervalText, out int retrySeconds);
        Result = new SLScheduleSettings { StartTime = start, StopTime = stop, RetryIntervalSeconds = retrySeconds };
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}
