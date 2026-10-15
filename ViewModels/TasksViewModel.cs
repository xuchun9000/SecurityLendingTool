using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityLendingTool.Models;
using SecurityLendingTool.Services;
using SecurityLendingTool.Tasks.Core;
using SecurityLendingTool.Tasks.Implementations;

namespace SecurityLendingTool.ViewModels;

/// <summary>ViewModel for the Tasks tab: the list of available tasks.</summary>
public partial class TasksViewModel : ObservableObject
{
    public TasksViewModel()
    {
        SLTasks = new ObservableCollection<ISLTask>
        {
            new PositionProjectionPB_Task(),
            new InternalGroupPositions_Task(),
            new PositionProjection_Task(),
            new PositionProjectionWMIADP_Task(),
            new PositionProjectionBPSMerge_Task(),
            new SecurityMasterFetch_Task(),
            new RehypDataImport_Task(),
            new DataLend_Task(),
            new DataLend_V6_Task(),
            new DataLend_V7_Task(),
            new DataLend_V8_Task(),
            new GOSOSDTWMI_Task(),
            new GOSOSDTWMC_Task(),
            new GOSOSDTPRB_Task(),
            new GOSOSRDWMI_Task(),
            new GOSOSRDWMC_Task(),
            new GOSOSRDPRB_Task(),
            new GOSOSRDNYB_Task(),
            new GOSOSRDLON_Task(),
            new GOSOSFeesFileWMI_Task(),
            new GOSOSFeesFileWMC_Task(),
            new GOSOSFeesFilePRB_Task(),
            new GOSOSFeesFileNYB_Task(),
            new GOSOSFeesFileLON_Task(),
            new APWMIREC_Task(),
            new APWMIMVT_Task(),
            new APWMCMVT_Task(),
            new APWMCREC_Task(),
            new APNYBREC_Task(),
            new APNYBMVT_Task(),
            new APLONMVT_Task(),
            new APLONREC_Task(),
            new GOSOSSCC1_Task(),
            new OCC_Task(),
            new GOSOSDTNYB_Task(),
            new GOSOSDTLON_Task(),
        };

        _scheduler.Settings = SLScheduleSettings.Load();
        _scheduler.StartRequested += Scheduler_StartRequested;
        _scheduler.StopRequested += Scheduler_StopRequested;
        UpdateScanStatusText();
    }

    private readonly SLTaskScheduler _scheduler = new SLTaskScheduler();

    private CancellationTokenSource? _runAllCancellation;

    private CancellationTokenSource? _cycleCancellation;

    public ObservableCollection<ISLTask> SLTasks { get; }

    [ObservableProperty] private ISLTask? _SelectedSLTask;

    /// <summary>True while Run All is going; disables every row's Start/Stop button.</summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSLTaskCommand), nameof(StopSLTaskCommand))] private bool _IsRunningAll;

    /// <summary>True while the scheduler checks the clock every second.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ScanButtonText))] [NotifyCanExecuteChangedFor(nameof(OpenScheduleCommand))] private bool _IsScanning;

    [ObservableProperty] private string _ScanStatusText = string.Empty;

    /// <summary>True while a scheduled cycle (passes + retry waits) is active; disables the manual Run All button.</summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RunAllSLTasksCommand))] private bool _IsScheduledCycleActive;

    public string ScanButtonText => IsScanning ? "Stop Scan" : "Scan";

    /// <summary>Set by the view: shows the schedule dialog and returns the new settings, or null on Cancel.</summary>
    public Func<SLScheduleSettings, SLScheduleSettings?>? ShowScheduleDialog { get; set; }

    private bool CanRunSingleSLTask(ISLTask? slTask)
    {
        return !IsRunningAll;
    }

    /// <summary>Starts one task; several tasks may run at the same time.</summary>
    [RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(CanRunSingleSLTask))]
    private async Task StartSLTask(ISLTask? slTask)
    {
        if (slTask == null || slTask.Status == SLTaskStatus.Running)
        {
            return;
        }

        await slTask.Start();
    }

    /// <summary>Asks one running SLTask to stop.</summary>
    [RelayCommand(CanExecute = nameof(CanRunSingleSLTask))]
    private void StopSLTask(ISLTask? slTask)
    {
        if (slTask == null)
        {
            return;
        }

        slTask.Stop();
    }

    /// <summary>
    /// Runs every SLTask one after another. While it runs, the command's CanExecute is false,
    /// so the Run All button stays disabled until the last SLTask has finished or Run All is stopped.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRunAllSLTasks))]
    private async Task RunAllSLTasks()
    {
        IsRunningAll = true;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        _runAllCancellation = cancellation;
        try
        {
            foreach (ISLTask slTask in SLTasks)
            {
                if (cancellation.IsCancellationRequested)
                {
                    break;
                }

                if (slTask.Status == SLTaskStatus.Running)
                {
                    continue;
                }

                await slTask.Start(cancellation.Token);
            }
        }
        finally
        {
            _runAllCancellation = null;
            IsRunningAll = false;
        }
    }

    private bool CanRunAllSLTasks()
    {
        return !IsScheduledCycleActive;
    }

    /// <summary>
    /// Scheduled cycle: reset all SLTasks, then run passes. Each pass runs (in order) only SLTasks that are not Completed.
    /// When all are Completed the scan stops; otherwise wait RetryIntervalSeconds and do another pass.
    /// Cancelled by the stop time or by Stop Scan.
    /// </summary>
    private async Task RunScheduledCycleAsync(CancellationToken cancellationToken)
    {
        IsScheduledCycleActive = true;
        try
        {
            while (IsRunningAll)
            {
                ScanStatusText = "Waiting for manual Run All to finish...";
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            foreach (ISLTask slTask in SLTasks)
            {
                slTask.Reset();
            }

            int pass = 1;
            while (true)
            {
                ScanStatusText = $"Pass {pass}: running...";
                IsRunningAll = true;
                try
                {
                    foreach (ISLTask slTask in SLTasks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (slTask.Status == SLTaskStatus.Completed || slTask.Status == SLTaskStatus.Running)
                        {
                            continue;
                        }

                        await slTask.Start(cancellationToken);
                    }
                }
                finally
                {
                    IsRunningAll = false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                int notCompleted = SLTasks.Count(slTask => slTask.Status != SLTaskStatus.Completed);
                if (notCompleted == 0)
                {
                    _scheduler.StopScan();
                    IsScanning = false;
                    ScanStatusText = $"All {SLTasks.Count} tasks completed at {DateTime.Now:HH:mm:ss}, scan stopped";
                    return;
                }

                int retrySeconds = Math.Max(5, _scheduler.Settings.RetryIntervalSeconds);
                DateTime retryAt = DateTime.Now.AddSeconds(retrySeconds);
                ScanStatusText = $"Pass {pass}: {notCompleted} not completed, retrying at {retryAt:HH:mm:ss}";
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(retrySeconds), cancellationToken);
                pass++;
            }
        }
        catch (OperationCanceledException)
        {
            // Stop time or Stop Scan; the canceller sets the status text.
        }
        finally
        {
            IsScheduledCycleActive = false;
        }
    }

    /// <summary>Cancels the scheduled cycle (current pass and retry wait) and stops running SLTasks.</summary>
    private void CancelScheduledCycle()
    {
        _cycleCancellation?.Cancel();
        _cycleCancellation?.Dispose();
        _cycleCancellation = null;
        StopAllSLTasks();
    }

    /// <summary>Cancels the Run All loop (remaining SLTasks are not started) and stops every running SLTask.</summary>
    private void StopAllSLTasks()
    {
        _runAllCancellation?.Cancel();
        foreach (ISLTask slTask in SLTasks)
        {
            if (slTask.Status == SLTaskStatus.Running)
            {
                slTask.Stop();
            }
        }
    }

    private bool CanOpenSchedule()
    {
        return !IsScanning;
    }

    /// <summary>Opens the schedule dialog and saves the new start/stop time.</summary>
    [RelayCommand(CanExecute = nameof(CanOpenSchedule))]
    private void OpenSchedule()
    {
        if (ShowScheduleDialog == null)
        {
            return;
        }

        SLScheduleSettings? newSettings = ShowScheduleDialog(_scheduler.Settings);
        if (newSettings == null)
        {
            return;
        }

        newSettings.Save();
        _scheduler.Settings = newSettings;
        UpdateScanStatusText();
    }

    /// <summary>Turns scanning on or off.</summary>
    [RelayCommand]
    private void ToggleScan()
    {
        if (IsScanning)
        {
            _scheduler.StopScan();
            IsScanning = false;
            CancelScheduledCycle();
        }
        else
        {
            IsScanning = true;
            UpdateScanStatusText();
            _scheduler.StartScan();
            return;
        }

        UpdateScanStatusText();
    }

    private void Scheduler_StartRequested(object? sender, EventArgs e)
    {
        CancelScheduledCycle();
        _cycleCancellation = new CancellationTokenSource();
        _ = RunScheduledCycleAsync(_cycleCancellation.Token);
    }

    private void Scheduler_StopRequested(object? sender, EventArgs e)
    {
        CancelScheduledCycle();
        int notCompleted = SLTasks.Count(slTask => slTask.Status != SLTaskStatus.Completed);
        string start = _scheduler.Settings.StartTime.ToString(@"hh\:mm");
        ScanStatusText = $"Stop time reached at {DateTime.Now:HH:mm:ss}: {notCompleted} not completed, next cycle at {start}";
    }

    private void UpdateScanStatusText()
    {
        string start = _scheduler.Settings.StartTime.ToString(@"hh\:mm");
        string stop = _scheduler.Settings.StopTime.ToString(@"hh\:mm");
        ScanStatusText = IsScanning ? $"Scanning: start {start}, stop {stop}" : $"Not scanning (start {start}, stop {stop})";
    }
}