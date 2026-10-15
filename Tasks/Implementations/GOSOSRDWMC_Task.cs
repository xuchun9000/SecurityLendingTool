using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS RD WMC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSRDWMC_Task : SLTaskBase
{
    public GOSOSRDWMC_Task()
        : base("GOSOS RD WMC", "GOSOS RD WMC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS RD WMC".
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