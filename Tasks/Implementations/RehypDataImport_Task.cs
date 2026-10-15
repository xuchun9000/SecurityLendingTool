using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>Task "Rehyp Data Import" (from dbo.REF_schedule_mini.task_name).</summary>
public sealed class RehypDataImport_Task : SLTaskBase
{
    public RehypDataImport_Task()
        : base("Rehyp Data Import", "Rehyp Data Import")
    {
    }

    public override bool CheckPreCondition()
    {
        return true;
    }

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: implement "Rehyp Data Import".
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