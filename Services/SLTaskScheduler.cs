using System.Windows.Threading;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.Services;

/// <summary>
/// Checks the clock every second while scanning. Raises StartRequested once when the daily window opens
/// (or immediately if scanning begins inside it) and StopRequested once when the window closes.
/// If StopTime is earlier than StartTime the window runs overnight (e.g. 22:00 -> 06:00 next day).
/// If StartTime equals StopTime the window is empty and nothing fires.
/// </summary>
public sealed class SLTaskScheduler
{
    private readonly DispatcherTimer _timer;
    private DateTime? _lastStartedWindowDate;
    private bool _isWindowActive;

    public SLTaskScheduler()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
    }

    public SLScheduleSettings Settings { get; set; } = new SLScheduleSettings();

    public bool IsScanning => _timer.IsEnabled;

    public event EventHandler? StartRequested;

    public event EventHandler? StopRequested;

    public void StartScan()
    {
        _lastStartedWindowDate = null;
        _isWindowActive = false;
        _timer.Start();
        Check(DateTime.Now);
    }

    public void StopScan()
    {
        _timer.Stop();
        _isWindowActive = false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Check(DateTime.Now);
    }

    /// <summary>Returns the date on which the window containing 'now' started, or null if now is outside the window.</summary>
    private DateTime? GetWindowDate(DateTime now)
    {
        TimeSpan time = now.TimeOfDay;
        TimeSpan start = Settings.StartTime;
        TimeSpan stop = Settings.StopTime;
        if (start < stop)
        {
            if (time >= start && time < stop)
            {
                return now.Date;
            }

            return null;
        }

        if (start > stop)
        {
            if (time >= start)
            {
                return now.Date;
            }

            if (time < stop)
            {
                return now.Date.AddDays(-1);
            }
        }

        return null;
    }

    private void Check(DateTime now)
    {
        DateTime? windowDate = GetWindowDate(now);
        if (windowDate.HasValue)
        {
            if (_lastStartedWindowDate != windowDate.Value)
            {
                _lastStartedWindowDate = windowDate.Value;
                _isWindowActive = true;
                StartRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (_isWindowActive)
        {
            _isWindowActive = false;
            StopRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
