using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT LON" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTLON_Task : SLTaskBase
{
    public GOSOSDTLON_Task()
        : base("GOSOS DT LON", "GOSOS DT LON")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT LON".
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