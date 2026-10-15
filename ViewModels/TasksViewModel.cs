using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    }

    public ObservableCollection<ISLTask> SLTasks { get; }

    [ObservableProperty] private ISLTask? _SelectedSLTask;

    /// <summary>True while Run All is going; disables every row's Start/Stop button.</summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSLTaskCommand), nameof(StopSLTaskCommand))] private bool _IsRunningAll;

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
    /// so the Run All button stays disabled until the last SLTask has finished.
    /// </summary>
    [RelayCommand]
    private async Task RunAllSLTasks()
    {
        IsRunningAll = true;
        try
        {
            foreach (ISLTask slTask in SLTasks)
            {
                if (slTask.Status == SLTaskStatus.Running)
                {
                    continue;
                }

                await slTask.Start();
            }
        }
        finally
        {
            IsRunningAll = false;
        }
    }
}