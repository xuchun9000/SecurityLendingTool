namespace SecurityLendingTool.Tasks.Core;

/// <summary>Data for <see cref="ISLTask.ProgressChanged"/>.</summary>
public sealed class SLTaskProgressEventArgs : EventArgs
{
    public SLTaskProgressEventArgs(SLTaskProgressEvent progressEvent, SLTaskStatus status, double progress, string message)
    {
        Event = progressEvent;
        Status = status;
        Progress = progress;
        Message = message;
    }

    /// <summary>What happened: Started, Progress, Completed, Failed, Canceled or PreconditionNotMet.</summary>
    public SLTaskProgressEvent Event { get; }

    /// <summary>Status at the moment of the report.</summary>
    public SLTaskStatus Status { get; }

    /// <summary>Progress in percent, from 0 to 100.</summary>
    public double Progress { get; }

    /// <summary>Readable text: the enum's [Description], plus details (e.g. "Failed: timeout"), or the task's own message for Progress.</summary>
    public string Message { get; }
}