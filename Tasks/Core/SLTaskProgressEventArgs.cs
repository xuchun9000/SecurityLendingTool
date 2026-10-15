namespace SecurityLendingTool.Tasks.Core;

/// <summary>Data for <see cref="ISLTask.ProgressChanged"/>.</summary>
public sealed class SLTaskProgressEventArgs : EventArgs
{
    public SLTaskProgressEventArgs(SLTaskStatus status, double progress, string message)
    {
        Status = status;
        Progress = progress;
        Message = message;
    }

    /// <summary>Status at the moment of the report.</summary>
    public SLTaskStatus Status { get; }

    /// <summary>Progress in percent, from 0 to 100.</summary>
    public double Progress { get; }

    /// <summary>What the task is doing now, e.g. "Downloading weather...".</summary>
    public string Message { get; }
}