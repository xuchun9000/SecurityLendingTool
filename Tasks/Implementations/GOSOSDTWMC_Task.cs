using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT WMC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTWMC_Task : SLTaskBase
{
    public GOSOSDTWMC_Task()
        : base("GOSOS DT WMC", "GOSOS DT WMC")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT WMC".
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