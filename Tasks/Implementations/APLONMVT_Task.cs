using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "AP LON MVT" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class APLONMVT_Task : SLTaskBase
{
    public APLONMVT_Task()
        : base("AP LON MVT", "AP LON MVT")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "AP LON MVT".
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