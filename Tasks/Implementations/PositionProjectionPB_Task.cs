using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Position Projection PB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class PositionProjectionPB_Task : SLTaskBase
{
    public PositionProjectionPB_Task()
        : base("Position Projection PB", "Position Projection PB")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Position Projection PB".
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