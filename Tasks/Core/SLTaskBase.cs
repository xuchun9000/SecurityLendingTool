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

    /// <inheritdoc/>
    public event EventHandler<SLTaskProgressEventArgs>? ProgressChanged;

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

        if (!CheckPreCondition())
        {
            Status = SLTaskStatus.Skipped;
            RaiseProgressChanged("Precondition not met, skipped");
            return;
        }

        Error = null;
        Result = null;
        Progress = 0d;
        Status = SLTaskStatus.Running;
        RaiseProgressChanged("Started");

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        runCancellation = cts;

        try
        {
            object? output = await ExecuteAsync(cts.Token);
            Complete(output);
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
    public abstract bool CheckPreCondition();

    /// <summary>Called by <see cref="Reset"/> after the base state is cleared; clear task-specific state here.</summary>
    protected abstract void OnReset();

    /// <summary>Updates progress (0-100, clamped) while running and raises <see cref="ProgressChanged"/>.</summary>
    protected void ReportProgress(double percent, string message = "")
    {
        EnsureRunning(nameof(ReportProgress));
        Progress = percent;
        RaiseProgressChanged(message);
    }

    private void RaiseProgressChanged(string message)
    {
        EventHandler<SLTaskProgressEventArgs>? handler = ProgressChanged;
        if (handler != null)
        {
            handler(this, new SLTaskProgressEventArgs(Status, Progress, message));
        }
    }

    // ---------- State changes (internal) ----------

    private void Complete(object? output)
    {
        EnsureRunning(nameof(Complete));
        Result = output;
        Progress = 100d;
        Status = SLTaskStatus.Completed;
        RaiseProgressChanged("Completed");
    }

    private void Fail(Exception exception)
    {
        EnsureRunning(nameof(Fail));
        Error = exception;
        Status = SLTaskStatus.Failed;
        RaiseProgressChanged("Failed: " + exception.Message);
    }

    private void Cancel()
    {
        EnsureRunning(nameof(Cancel));
        Status = SLTaskStatus.Canceled;
        RaiseProgressChanged("Canceled");
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