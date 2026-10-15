using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "GOSOS SC C1" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class GOSOSSCC1_Task : SLTaskBase
{
    public GOSOSSCC1_Task()
        : base("GOSOS SC C1", "GOSOS SC C1")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "GOSOS SC C1".
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