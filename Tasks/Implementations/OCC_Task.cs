using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "OCC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class OCC_Task : SLTaskBase
{
    public OCC_Task()
        : base("OCC", "OCC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "OCC".
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