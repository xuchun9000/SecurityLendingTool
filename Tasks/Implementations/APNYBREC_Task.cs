using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP NYB REC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APNYBREC_Task : SLTaskBase
{
    public APNYBREC_Task()
        : base("AP NYB REC", "AP NYB REC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP NYB REC".
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