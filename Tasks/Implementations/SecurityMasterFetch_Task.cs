using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Security Master Fetch" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class SecurityMasterFetch_Task : SLTaskBase
{
    public SecurityMasterFetch_Task()
        : base("Security Master Fetch", "Security Master Fetch")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Security Master Fetch".
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