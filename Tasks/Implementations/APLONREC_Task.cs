using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP LON REC" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APLONREC_Task : SLTaskBase
{
    public APLONREC_Task()
        : base("AP LON REC", "AP LON REC")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP LON REC".
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