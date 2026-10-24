using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.ViewModels;

/// <summary>ViewModel for the schedule dialog: every value is an int edited with spinners, sliders and preset buttons.</summary>
public partial class ScheduleSettingsViewModel : ObservableObject
{
    public const int MinRetrySeconds = 5;
    public const int MaxRetrySeconds = 3600;
    public const int MinRetentionMonths = 1;
    public const int MaxRetentionMonths = 120;

    public ScheduleSettingsViewModel(SLScheduleSettings settings)
    {
        _StartHour = settings.StartTime.Hours;
        _StartMinute = settings.StartTime.Minutes;
        _StopHour = settings.StopTime.Hours;
        _StopMinute = settings.StopTime.Minutes;
        _RetryIntervalSeconds = Math.Clamp(settings.RetryIntervalSeconds, MinRetrySeconds, MaxRetrySeconds);
        _RetentionMonths = Math.Clamp(settings.MessageRetentionMonths, MinRetentionMonths, MaxRetentionMonths);
    }

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(OkCommand))] 
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(WindowSummary), nameof(OvernightNote), nameof(StartTimeText))] 
    private int _StartHour;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(OkCommand))] 
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(WindowSummary), nameof(OvernightNote), nameof(StartTimeText))] 
    private int _StartMinute;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(OkCommand))] 
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(WindowSummary), nameof(OvernightNote), nameof(StopTimeText))] 
    private int _StopHour;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(OkCommand))] 
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(WindowSummary), nameof(OvernightNote), nameof(StopTimeText))] 
    private int _StopMinute;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(RetrySummary), nameof(RetrySliderValue))] 
    private int _RetryIntervalSeconds;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(RetentionSummary), nameof(RetentionSliderValue))] 
    private int _RetentionMonths;

    /// <summary>Raised with true (OK) or false (Cancel); the window closes itself.</summary>
    public event EventHandler<bool>? CloseRequested;

    public SLScheduleSettings? Result { get; private set; }

    public const int RetrySliderMaximum = 600;
    public const int RetentionSliderMaximum = 24;

    /// <summary>Slider view of the retry interval (5 - 600); the spinner can go higher without the slider pulling it back.</summary>
    public int RetrySliderValue
    {
        get { return Math.Min(RetryIntervalSeconds, RetrySliderMaximum); }
        set { RetryIntervalSeconds = Math.Clamp(value, MinRetrySeconds, RetrySliderMaximum); }
    }

    /// <summary>Slider view of the retention (1 - 24); the spinner can go up to 120.</summary>
    public int RetentionSliderValue
    {
        get { return Math.Min(RetentionMonths, RetentionSliderMaximum); }
        set { RetentionMonths = Math.Clamp(value, MinRetentionMonths, RetentionSliderMaximum); }
    }

    public int MinRetry => MinRetrySeconds;

    public int MaxRetry => MaxRetrySeconds;

    public int MinRetention => MinRetentionMonths;

    public int MaxRetention => MaxRetentionMonths;

    private TimeSpan StartTime => new TimeSpan(StartHour, StartMinute, 0);

    private TimeSpan StopTime => new TimeSpan(StopHour, StopMinute, 0);

    public string StartTimeText => FormatTime(StartTime);

    public string StopTimeText => FormatTime(StopTime);

    /// <summary>E.g. "Runs 09:00 â†’ 17:00 (8 h)".</summary>
    public string WindowSummary
    {
        get
        {
            if (StartTime == StopTime)
            {
                return "Start and stop time are the same";
            }

            TimeSpan length = StopTime - StartTime;
            if (length < TimeSpan.Zero)
            {
                length += TimeSpan.FromDays(1);
            }

            return $"Runs {StartTimeText} â†’ {StopTimeText} ({FormatDuration(length)})";
        }
    }

    public string OvernightNote => StopTime < StartTime ? "Overnight window: stops the next day" : string.Empty;

    public string RetrySummary => $"Wait {FormatSeconds(RetryIntervalSeconds)} before retrying SLTasks that did not complete";

    public string RetentionSummary
    {
        get
        {
            string unit = RetentionMonths == 1 ? "month" : "months";
            return $"Keep the last {RetentionMonths} {unit}; older messages are deleted automatically";
        }
    }

    public string ErrorText => StartTime == StopTime ? "Start and stop time must differ" : string.Empty;

    private static string FormatTime(TimeSpan time)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{time.Hours:D2}:{time.Minutes:D2}");
    }

    private static string FormatDuration(TimeSpan length)
    {
        int hours = (int)length.TotalHours;
        if (length.Minutes == 0)
        {
            return $"{hours} h";
        }

        if (hours == 0)
        {
            return $"{length.Minutes} min";
        }

        return $"{hours} h {length.Minutes} min";
    }

    private static string FormatSeconds(int seconds)
    {
        int minutes = seconds / 60;
        int rest = seconds % 60;
        if (minutes == 0)
        {
            return $"{rest} s";
        }

        if (rest == 0)
        {
            return $"{minutes} min";
        }

        return $"{minutes} min {rest} s";
    }

    [RelayCommand]
    private void SetRetry(string? seconds)
    {
        if (int.TryParse(seconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            RetryIntervalSeconds = Math.Clamp(value, MinRetrySeconds, MaxRetrySeconds);
        }
    }

    [RelayCommand]
    private void SetRetention(string? months)
    {
        if (int.TryParse(months, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            RetentionMonths = Math.Clamp(value, MinRetentionMonths, MaxRetentionMonths);
        }
    }

    /// <summary>Preset daily window, parameter "HH:mm-HH:mm".</summary>
    [RelayCommand]
    private void SetWindow(string? window)
    {
        string[] parts = (window ?? string.Empty).Split('-');
        if (parts.Length == 2
            && TimeSpan.TryParseExact(parts[0], SLScheduleSettings.TimeFormat, CultureInfo.InvariantCulture, out TimeSpan start)
            && TimeSpan.TryParseExact(parts[1], SLScheduleSettings.TimeFormat, CultureInfo.InvariantCulture, out TimeSpan stop))
        {
            StartHour = start.Hours;
            StartMinute = start.Minutes;
            StopHour = stop.Hours;
            StopMinute = stop.Minutes;
        }
    }

    private bool CanOk()
    {
        return StartTime != StopTime;
    }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok()
    {
        Result = new SLScheduleSettings
        {
            StartTime = StartTime,
            StopTime = StopTime,
            RetryIntervalSeconds = Math.Clamp(RetryIntervalSeconds, MinRetrySeconds, MaxRetrySeconds),
            MessageRetentionMonths = Math.Clamp(RetentionMonths, MinRetentionMonths, MaxRetentionMonths),
        };
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}
