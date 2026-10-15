using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Position Projection BPS Merge" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class PositionProjectionBPSMerge_Task : SLTaskBase
{
    public PositionProjectionBPSMerge_Task()
        : base("Position Projection BPS Merge", "Position Projection BPS Merge")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Position Projection BPS Merge".
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