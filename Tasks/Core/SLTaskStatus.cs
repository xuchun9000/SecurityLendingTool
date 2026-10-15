namespace SecurityLendingTool.Tasks.Core;

/// <summary>
/// Lifecycle state of an <see cref="ISLTask"/>.
/// </summary>
public enum SLTaskStatus
{
    /// <summary>Created but not started yet.</summary>
    NotStarted,

    /// <summary>Started and still running.</summary>
    Running,

    /// <summary>Finished successfully.</summary>
    Completed,

    /// <summary>Finished because of an error.</summary>
    Failed,

    /// <summary>Finished because it was canceled.</summary>
    Canceled,

    /// <summary>Not run because <see cref="ISLTask.CheckPreCondition"/> was not met.</summary>
    Skipped,
}