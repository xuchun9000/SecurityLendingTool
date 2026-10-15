namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>
/// TEST ONLY: simulates work so the Scan / retry loop can be tested.
/// Each run takes a random 1-8 seconds (reporting progress every 0.5 s) and
/// fails with an exception about 30% of the time. Remove when real work is implemented.
/// </summary>
public static class SLTaskTestSimulator
{
    private static readonly Random random = new Random();

    public static async Task SimulateAsync(string slTaskName, Action<double, string> reportProgress, CancellationToken cancellationToken)
    {
        int durationMs = random.Next(1000, 8001);
        bool willFail = random.NextDouble() < 0.3;
        int failAtMs = random.Next(500, durationMs);
        int elapsedMs = 0;
        const int stepMs = 500;

        while (elapsedMs < durationMs)
        {
            await Task.Delay(stepMs, cancellationToken);
            elapsedMs += stepMs;

            if (willFail && elapsedMs >= failAtMs)
            {
                throw new InvalidOperationException($"Simulated failure in '{slTaskName}' after {elapsedMs / 1000.0:0.0}s");
            }

            double percent = Math.Min(100d, elapsedMs * 100d / durationMs);
            reportProgress(percent, $"Working... {elapsedMs / 1000.0:0.0}s / {durationMs / 1000.0:0.0}s");
        }
    }
}