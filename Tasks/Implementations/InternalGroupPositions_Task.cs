using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Internal Group Positions" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class InternalGroupPositions_Task : SLTaskBase
{
    public InternalGroupPositions_Task()
        : base("Internal Group Positions", "Internal Group Positions")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Internal Group Positions".
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