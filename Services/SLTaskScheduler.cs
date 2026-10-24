using SecurityLendingTool.Models;

namespace SecurityLendingTool.Services;

/// <summary>
/// Checks the clock every second while scanning (an async PeriodicTimer loop). Raises StartRequested once when the daily window opens
/// (or immediately if scanning begins inside it) and StopRequested once when the window closes.
/// If StopTime is earlier than StartTime the window runs overnight (e.g. 22:00 -> 06:00 next day).
/// If StartTime equals StopTime the window is empty and nothing fires.
/// </summary>
public sealed class SLTaskScheduler
{
    /// <summary>How often the clock is checked while scanning.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private CancellationTokenSource? _scanCts;
    private DateTime? _lastStartedWindowDate;
    private bool _isWindowActive;

    public SLScheduleSettings Settings { get; set; } = new SLScheduleSettings();

    public bool IsScanning => _scanCts != null;

    public event EventHandler? StartRequested;

    public event EventHandler? StopRequested;

    /// <summary>
    /// Starts checking the clock. Call on the UI thread: the loop resumes on the captured
    /// SynchronizationContext, so StartRequested / StopRequested are raised on the UI thread.
    /// </summary>
    public void StartScan()
    {
        StopScan();
        _lastStartedWindowDate = null;
        _isWindowActive = false;
        _scanCts = new CancellationTokenSource();
        Check(DateTime.Now);
        _ = RunCheckLoopAsync(_scanCts.Token);
    }

    public void StopScan()
    {
        if (_scanCts != null)
        {
            _scanCts.Cancel();
            _scanCts.Dispose();
            _scanCts = null;
        }

        _isWindowActive = false;
    }

    private async Task RunCheckLoopAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(CheckInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                Check(DateTime.Now);
            }
        }
        catch (OperationCanceledException)
        {
            // StopScan was called.
        }
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
