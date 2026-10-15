using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP WMC REC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APWMCREC_Task : SLTaskBase
{
    public APWMCREC_Task()
        : base("AP WMC REC", "AP WMC REC")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP WMC REC".
        // TEST ONLY: random duration / random exception. Remove when implementing.
        await SLTaskTestSimulator.SimulateAsync(Name, (percent, message) => ReportProgress(percent, message), cancellationToken);
        return null;
    }

    public override void OnFinished()
    {
    }

    protected override void OnStop()
    {
    }

    protected override void OnReset()
    {
    }
}