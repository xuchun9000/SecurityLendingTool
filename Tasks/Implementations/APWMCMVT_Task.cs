using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP WMC MVT" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APWMCMVT_Task : SLTaskBase
{
    public APWMCMVT_Task()
        : base("AP WMC MVT", "AP WMC MVT")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP WMC MVT".
        await Task.CompletedTask;
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