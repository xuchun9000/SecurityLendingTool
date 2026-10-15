using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS Fees File LON" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSFeesFileLON_Task : SLTaskBase
{
    public GOSOSFeesFileLON_Task()
        : base("GOSOS Fees File LON", "GOSOS Fees File LON")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS Fees File LON".
        await Task.CompletedTask;
        return null;
    }

    public override void OnFinished()
    {
    }

    protected override void OnStop()
    {
    }

    protected override void OnReset()
    {
    }
}