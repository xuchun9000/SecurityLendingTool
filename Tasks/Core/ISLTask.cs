using System.ComponentModel;

namespace SecurityLendingTool.Tasks.Core;

/// <summary>
/// A unit of work in the app, with a name, a description and observable progress.
/// <para>
/// Extends <see cref="INotifyPropertyChanged"/> so the UI can bind directly to
/// <see cref="Status"/>, <see cref="Progress"/>, <see cref="IsStarted"/> and <see cref="IsFinished"/>.
/// </para>
/// </summary>
public interface ISLTask : INotifyPropertyChanged
{
    // ---------- Identity ----------

    /// <summary>Short display name of the task.</summary>
    string Name { get; }

    /// <summary>What the task does.</summary>
    string Description { get; }

    // ---------- Progress ----------

    /// <summary>Current lifecycle state.</summary>
    SLTaskStatus Status { get; }

    /// <summary>Progress in percent, from 0 to 100.</summary>
    double Progress { get; }

    /// <summary>True once the task has started (Running or any finished state).</summary>
    bool IsStarted { get; }

    /// <summary>True when the task has ended: Completed, Failed or Canceled.</summary>
    bool IsFinished { get; }

    // ---------- Result ----------

    /// <summary>
    /// The output other code should read once the task has finished.
    /// Null until the task completes; its actual type depends on the task.
    /// </summary>
    object? Result { get; }

    /// <summary>
    /// Raised on every progress report and on every status change (start, complete, fail, cancel).
    /// May be raised on a background thread; dispatch to the UI thread if needed.
    /// </summary>
    event EventHandler<SLTaskProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// A snapshot of every message raised through <see cref="ProgressChanged"/>, oldest first.
    /// Kept across runs (not cleared by <see cref="Reset"/>), so retries are visible too. Thread-safe.
    /// </summary>
    IReadOnlyList<SLTaskMessage> GetMessageHistory();

    // ---------- Run ----------

    /// <summary>
    /// Runs the task. A finished task is reset and run again; calling it while Running throws.
    /// Ends as Completed (with <see cref="Result"/>), Failed or Canceled; work exceptions are not rethrown.
    /// </summary>
    Task Start(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checked by <see cref="Start"/> before every run. True: the task runs.
    /// False: the task is not run and ends as <see cref="SLTaskStatus.Skipped"/>; <see cref="SLPreConditionResult.Reason"/> says why.
    /// </summary>
    SLPreConditionResult CheckPreCondition();

    /// <summary>
    /// Called by <see cref="Start"/> once, right after the task finished normally (Status = Completed,
    /// <see cref="Result"/> already set). Not called when the task fails, is canceled or is skipped.
    /// </summary>
    void OnFinished();

    /// <summary>Asks a running task to stop; it ends as Canceled. Does nothing if it is not running.</summary>
    void Stop();

    /// <summary>Puts a finished (or not started) task back to NotStarted and clears its result. Throws while running.</summary>
    void Reset();
}
