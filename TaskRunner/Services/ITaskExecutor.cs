namespace TaskRunner.Services;

/// <summary>
/// Runs a unit of work. The ViewModel only knows this interface,
/// so tests can inject a fake (same idea as a fake device).
/// </summary>
public interface ITaskExecutor
{
    Task RunAsync(int durationSeconds, IProgress<double> progress, CancellationToken ct);
}
