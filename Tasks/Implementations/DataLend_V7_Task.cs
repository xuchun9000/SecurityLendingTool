using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "DataLend_V7" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class DataLend_V7_Task : SLTaskBase
{
    public DataLend_V7_Task()
        : base("DataLend_V7", "DataLend_V7")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "DataLend_V7".
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