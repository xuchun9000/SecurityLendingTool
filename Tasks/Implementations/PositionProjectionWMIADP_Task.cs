using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Position Projection WMI ADP" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class PositionProjectionWMIADP_Task : SLTaskBase
{
    public PositionProjectionWMIADP_Task()
        : base("Position Projection WMI ADP", "Position Projection WMI ADP")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Position Projection WMI ADP".
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