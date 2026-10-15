using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP WMC REC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APWMCREC_Task : SLTaskBase
{
    public APWMCREC_Task()
        : base("AP WMC REC", "AP WMC REC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP WMC REC".
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