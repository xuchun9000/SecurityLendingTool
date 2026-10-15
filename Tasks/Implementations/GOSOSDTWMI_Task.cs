using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS DT WMI" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSDTWMI_Task : SLTaskBase
{
    public GOSOSDTWMI_Task()
        : base("GOSOS DT WMI", "GOSOS DT WMI")
    {
    }

    public override SLPreConditionResult CheckPreCondition()
    {
        return SLPreConditionResult.Met;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS DT WMI".
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