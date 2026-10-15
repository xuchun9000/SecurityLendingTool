using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Position Projection" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class PositionProjection_Task : SLTaskBase
{
    public PositionProjection_Task()
        : base("Position Projection", "Position Projection")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Position Projection".
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