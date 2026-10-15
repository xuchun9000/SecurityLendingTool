using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Position Projection BPS Merge" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class PositionProjectionBPSMerge_Task : SLTaskBase
{
    public PositionProjectionBPSMerge_Task()
        : base("Position Projection BPS Merge", "Position Projection BPS Merge")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Position Projection BPS Merge".
        await Task.CompletedTask;
        return null;
    }

    protected override void OnStop()
    {
    }

    protected override void OnReset()
    {
    }
}