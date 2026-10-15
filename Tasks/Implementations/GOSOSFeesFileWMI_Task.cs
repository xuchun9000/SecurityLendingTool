using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS Fees File WMI" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSFeesFileWMI_Task : SLTaskBase
{
    public GOSOSFeesFileWMI_Task()
        : base("GOSOS Fees File WMI", "GOSOS Fees File WMI")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS Fees File WMI".
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