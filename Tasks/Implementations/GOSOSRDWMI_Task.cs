using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS RD WMI" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSRDWMI_Task : SLTaskBase
{
    public GOSOSRDWMI_Task()
        : base("GOSOS RD WMI", "GOSOS RD WMI")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS RD WMI".
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