using System.Diagnostics;

namespace TaskRunner.Services;

/// <summary>
/// Simulates long-running work (think: talking to a device).
/// Uses Task.Delay = a real asynchronous wait, so the UI thread stays free.
/// </summary>
public class SimulatedTaskExecutor : ITaskExecutor
{
    public async Task RunAsync(int durationSeconds, IProgress<double> progress, CancellationToken ct)
    {
        var total = TimeSpan.FromSeconds(durationSeconds);
        var clock = Stopwatch.StartNew();

        while (clock.Elapsed < total)
        {
            ct.ThrowIfCancellationRequested();                       // cancel at a safe point
            progress.Report(Math.Min(100, clock.Elapsed / total * 100));
            await Task.Delay(100, ct);                               // UI thread is free here
        }

        progress.Report(100);
    }
}
