using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS Fees File NYB" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSFeesFileNYB_Task : SLTaskBase
{
    public GOSOSFeesFileNYB_Task()
        : base("GOSOS Fees File NYB", "GOSOS Fees File NYB")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS Fees File NYB".
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