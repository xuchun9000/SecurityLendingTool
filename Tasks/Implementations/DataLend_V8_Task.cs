using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "DataLend_V8" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class DataLend_V8_Task : SLTaskBase
{
    public DataLend_V8_Task()
        : base("DataLend_V8", "DataLend_V8")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "DataLend_V8".
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