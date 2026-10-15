using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS Fees File WMC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSFeesFileWMC_Task : SLTaskBase
{
    public GOSOSFeesFileWMC_Task()
        : base("GOSOS Fees File WMC", "GOSOS Fees File WMC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS Fees File WMC".
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