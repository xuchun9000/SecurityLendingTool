using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT WMI" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTWMI_Task : SLTaskBase
{
    public GOSOSDTWMI_Task()
        : base("GOSOS DT WMI", "GOSOS DT WMI")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT WMI".
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