using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS RD NYB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSRDNYB_Task : SLTaskBase
{
    public GOSOSRDNYB_Task()
        : base("GOSOS RD NYB", "GOSOS RD NYB")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS RD NYB".
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