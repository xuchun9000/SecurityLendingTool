using CommunityToolkit.Mvvm.ComponentModel;

namespace SecurityLendingTool.Tasks.Core;

/// <summary>
/// Base class for <see cref="ISLTask"/> implementations, built on CommunityToolkit.Mvvm.
/// <para>
/// Holds the name, description, status and progress, and raises PropertyChanged for
/// every change (including the derived <see cref="IsStarted"/> / <see cref="IsFinished"/>).
/// State can only be changed by the derived class through the protected methods:
/// <see cref="Start"/>, <see cref="ReportProgress"/>, <see cref="Complete"/>,
/// <see cref="Fail"/> and <see cref="Cancel"/>.
/// </para>
/// </summary>
public abstract partial class SLTaskBase : ObservableObject, ISLTask
{
    private SLTaskStatus status = SLTaskStatus.NotStarted;
    private double progress;
    private Exception? error;
    private object? result;
    private string message = string.Empty;
    private readonly List<SLTaskMessage> messageHistory = new List<SLTaskMessage>();
    private readonly object messageHistoryLock = new object();
    private CancellationTokenSource? runCancellation;

    protected SLTaskBase(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Task name is required.", nameof(name));
        }

        Name = name;
        Description = description ?? string.Empty;
    }

    // ---------- Identity ----------

    public string Name { get; }

    public string Description { get; }

    // ---------- Progress ----------

    public SLTaskStatus Status
    {
        get => status;
        private set
        {
            if (SetProperty(ref status, value))
            {
                OnPropertyChanged(nameof(IsStarted));
                OnPropertyChanged(nameof(IsFinished));
            }
        }
    }

    /// <summary>Progress in percent, from 0 to 100.</summary>
    public double Progress
    {
        get => progress;
        private set => SetProperty(ref progress, Math.Clamp(value, 0d, 100d));
    }

    public bool IsStarted => Status != SLTaskStatus.NotStarted;

    public bool IsFinished =>
        Status is SLTaskStatus.Completed or SLTaskStatus.Failed or SLTaskStatus.Canceled or SLTaskStatus.Skipped;

    /// <summary>The error that made the task fail, if any.</summary>
    public Exception? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>The output of the task, set when the task completes.</summary>
    public object? Result
    {
        get => result;
        private set => SetProperty(ref result, value);
    }

    /// <summary>
    /// The latest status text: progress message while running, the error when Failed,
    /// the reason when Skipped, the result (if any) when Completed.
    /// </summary>
    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    /// <inheritdoc/>
    public event EventHandler<SLTaskProgressEventArgs>? ProgressChanged;

    /// <inheritdoc/>
    public IReadOnlyList<SLTaskMessage> GetMessageHistory()
    {
        lock (messageHistoryLock)
        {
            return messageHistory.ToArray();
        }
    }

    // ---------- Run (public entry point) ----------

    /// <summary>
    /// Runs the task. This is the only way to run it from outside.
    /// A finished task is reset first, so it can be started again.
    /// Status ends as Completed (with <see cref="Result"/>), Failed (with <see cref="Error"/>)
    /// or Canceled; exceptions from the work are not rethrown.
    /// </summary>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        if (Status == SLTaskStatus.Running)
        {
            throw new InvalidOperationException($"Task '{Name}' is already running.");
        }

        if (IsFinished)
        {
            Reset();
        }

        SLPreConditionResult preCondition = CheckPreCondition();
        if (!preCondition.IsMet)
        {
            Status = SLTaskStatus.Skipped;
            RaiseProgressChanged(SLTaskProgressEvent.PreconditionNotMet, preCondition.Reason);
            return;
        }

        Error = null;
        Result = null;
        Progress = 0d;
        Status = SLTaskStatus.Running;
        RaiseProgressChanged(SLTaskProgressEvent.Started);

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        runCancellation = cts;
        bool completed = false;

        try
        {
            object? output = await ExecuteAsync(cts.Token);
            Complete(output);
            completed = true;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            Cancel();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
        finally
        {
            runCancellation = null;
            cts.Dispose();
        }

        if (completed)
        {
            OnFinished();
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        CancellationTokenSource? cts = runCancellation;
        if (Status != SLTaskStatus.Running || cts == null || cts.IsCancellationRequested)
        {
            return;
        }

        OnStop();
        cts.Cancel();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        if (Status == SLTaskStatus.Running)
        {
            throw new InvalidOperationException($"Task '{Name}' cannot be reset while running. Call Stop() first.");
        }

        Error = null;
        Result = null;
        Progress = 0d;
        Message = string.Empty;
        Status = SLTaskStatus.NotStarted;
        OnReset();
    }

    // ---------- For derived classes ----------

    /// <summary>
    /// The actual work. Return the value that should become <see cref="Result"/>.
    /// Throw to fail; honor the token to cancel. Call <see cref="ReportProgress"/> along the way.
    /// </summary>
    protected abstract Task<object?> ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>Called by <see cref="Stop"/> while running, just before the token is canceled.</summary>
    protected abstract void OnStop();

    /// <inheritdoc/>
    public abstract SLPreConditionResult CheckPreCondition();

    /// <inheritdoc/>
    public abstract void OnFinished();

    /// <summary>Called by <see cref="Reset"/> after the base state is cleared; clear task-specific state here.</summary>
    protected abstract void OnReset();

    /// <summary>Updates progress (0-100, clamped) while running and raises <see cref="ProgressChanged"/>.</summary>
    protected void ReportProgress(double percent, string message = "")
    {
        EnsureRunning(nameof(ReportProgress));
        Progress = percent;
        RaiseProgressChanged(SLTaskProgressEvent.Progress, message);
    }

    private void RaiseProgressChanged(SLTaskProgressEvent progressEvent, string message = "")
    {
        // Description from the enum, plus the extra detail (if any): e.g. "Failed: timeout".
        string description = progressEvent.ToDescription();
        string text;
        if (string.IsNullOrEmpty(message))
        {
            text = description;
        }
        else if (progressEvent == SLTaskProgressEvent.Progress)
        {
            text = message;
        }
        else
        {
            text = $"{description}: {message}";
        }

        Message = text;
        lock (messageHistoryLock)
        {
            messageHistory.Add(new SLTaskMessage(DateTime.Now, progressEvent, Status, Progress, text));
        }

        EventHandler<SLTaskProgressEventArgs>? handler = ProgressChanged;
        if (handler == null)
        {
            return;
        }

        handler(this, new SLTaskProgressEventArgs(progressEvent, Status, Progress, text));
    }

    // ---------- State changes (internal) ----------

    private void Complete(object? output)
    {
        EnsureRunning(nameof(Complete));
        Result = output;
        Progress = 100d;
        Status = SLTaskStatus.Completed;
        RaiseProgressChanged(SLTaskProgressEvent.Completed, output?.ToString() ?? string.Empty);
    }

    private void Fail(Exception exception)
    {
        EnsureRunning(nameof(Fail));
        Error = exception;
        Status = SLTaskStatus.Failed;
        RaiseProgressChanged(SLTaskProgressEvent.Failed, exception.Message);
    }

    private void Cancel()
    {
        EnsureRunning(nameof(Cancel));
        Status = SLTaskStatus.Canceled;
        RaiseProgressChanged(SLTaskProgressEvent.Canceled);
    }

    private void EnsureRunning(string action)
    {
        if (Status != SLTaskStatus.Running)
        {
            throw new InvalidOperationException($"Task '{Name}': {action} requires state Running, but it is {Status}.");
        }
    }

    public override string ToString() => $"{Name} ({Status}, {Progress:0}%)";
}