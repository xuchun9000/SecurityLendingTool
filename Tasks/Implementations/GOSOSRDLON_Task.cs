using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS RD LON" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSRDLON_Task : SLTaskBase
{
    public GOSOSRDLON_Task()
        : base("GOSOS RD LON", "GOSOS RD LON")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS RD LON".
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