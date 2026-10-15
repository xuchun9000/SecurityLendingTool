using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT LON" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTLON_Task : SLTaskBase
{
    public GOSOSDTLON_Task()
        : base("GOSOS DT LON", "GOSOS DT LON")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT LON".
        // TEST ONLY: random duration / random exception. Remove when implementing.
        await SLTaskTestSimulator.SimulateAsync(Name, (percent, message) => ReportProgress(percent, message), cancellationToken);
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