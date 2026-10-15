using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS Fees File PRB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSFeesFilePRB_Task : SLTaskBase
{
    public GOSOSFeesFilePRB_Task()
        : base("GOSOS Fees File PRB", "GOSOS Fees File PRB")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS Fees File PRB".
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