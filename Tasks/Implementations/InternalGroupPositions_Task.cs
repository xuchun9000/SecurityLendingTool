using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Internal Group Positions" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class InternalGroupPositions_Task : SLTaskBase
{
    public InternalGroupPositions_Task()
        : base("Internal Group Positions", "Internal Group Positions")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Internal Group Positions".
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