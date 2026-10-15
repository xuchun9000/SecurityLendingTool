namespace SecurityLendingTool.Tasks.Core;

/// <summary>Result of <see cref="ISLTask.CheckPreCondition"/>: whether the task may run, and why not.</summary>
public sealed record SLPreConditionResult(bool IsMet, string Reason)
{
    /// <summary>The precondition is met; the task runs.</summary>
    public static SLPreConditionResult Met { get; } = new(true, string.Empty);

    /// <summary>The precondition is not met; the task is skipped with this reason.</summary>
    public static SLPreConditionResult NotMet(string reason) => new(false, reason);
}