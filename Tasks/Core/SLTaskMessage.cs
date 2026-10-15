namespace SecurityLendingTool.Tasks.Core;

/// <summary>One entry in an SLTask's message history: what was reported, and when.</summary>
public sealed record SLTaskMessage(DateTime Timestamp, SLTaskProgressEvent Event, SLTaskStatus Status, double Progress, string Text);
