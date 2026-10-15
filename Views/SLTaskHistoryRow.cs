using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Views;

/// <summary>One row of the history window: the message, its 1-based attempt number, and whether it starts a later attempt.</summary>
public sealed record SLTaskHistoryRow(int Attempt, bool IsNewAttempt, SLTaskMessage Message);
