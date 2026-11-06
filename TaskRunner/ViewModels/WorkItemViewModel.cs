using CommunityToolkit.Mvvm.ComponentModel;
using TaskRunner.Models;

namespace TaskRunner.ViewModels;

/// <summary>
/// One task. It raises PropertyChanged, so the list and the execution stack
/// update by themselves when Status or Progress change.
/// </summary>
public partial class WorkItemViewModel : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private int durationSeconds = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private WorkStatus status = WorkStatus.Pending;

    [ObservableProperty] private double progress;
    [ObservableProperty] private int remainingSeconds;

    /// <summary>Queued or running: can't edit, delete or run again.</summary>
    public bool IsBusy => Status is WorkStatus.Queued or WorkStatus.Running;

    /// <summary>Set while the task is queued/running, so it can be cancelled.</summary>
    internal CancellationTokenSource? Cancellation { get; set; }
}
