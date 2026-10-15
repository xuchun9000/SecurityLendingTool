using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "DataLend" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class DataLend_Task : SLTaskBase
{
    public DataLend_Task()
        : base("DataLend", "DataLend")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "DataLend".
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