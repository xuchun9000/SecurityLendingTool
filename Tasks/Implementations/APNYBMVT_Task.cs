using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP NYB MVT" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APNYBMVT_Task : SLTaskBase
{
    public APNYBMVT_Task()
        : base("AP NYB MVT", "AP NYB MVT")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP NYB MVT".
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