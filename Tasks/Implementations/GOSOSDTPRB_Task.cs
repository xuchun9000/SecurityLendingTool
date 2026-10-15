using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT PRB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTPRB_Task : SLTaskBase
{
    public GOSOSDTPRB_Task()
        : base("GOSOS DT PRB", "GOSOS DT PRB")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT PRB".
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