using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS RD PRB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSRDPRB_Task : SLTaskBase
{
    public GOSOSRDPRB_Task()
        : base("GOSOS RD PRB", "GOSOS RD PRB")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS RD PRB".
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