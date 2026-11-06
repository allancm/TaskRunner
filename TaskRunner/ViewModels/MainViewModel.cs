using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskRunner.Models;
using TaskRunner.Services;

namespace TaskRunner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int MaxParallel = 2;

    private readonly ITaskExecutor _executor;
    private readonly SemaphoreSlim _slots = new(MaxParallel, MaxParallel); // at most 2 running

    public MainViewModel(ITaskExecutor executor)
    {
        _executor = executor;

        Tasks.Add(new WorkItemViewModel { Name = "Read device", Description = "Read current settings", DurationSeconds = 4 });
        Tasks.Add(new WorkItemViewModel { Name = "Program therapy", Description = "Send new parameters", DurationSeconds = 8 });
        Tasks.Add(new WorkItemViewModel { Name = "Verify", Description = "Read back and compare", DurationSeconds = 3 });
    }

    public ObservableCollection<WorkItemViewModel> Tasks { get; } = new();
    public ObservableCollection<WorkItemViewModel> ExecutionStack { get; } = new();

    // ---------- Selection and editor (left panel) ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewMode))]
    [NotifyCanExecuteChangedFor(nameof(RunCommand), nameof(DeleteCommand), nameof(SaveCommand))]
    private WorkItemViewModel? selectedTask;

    /// <summary>No selection = the editor is creating a new task.</summary>
    public bool IsNewMode => SelectedTask is null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string editName = "";

    [ObservableProperty]
    private string editDescription = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int editDuration = 5;

    // Generated hook: runs whenever SelectedTask changes → load it into the editor.
    partial void OnSelectedTaskChanged(WorkItemViewModel? value)
    {
        EditName = value?.Name ?? "";
        EditDescription = value?.Description ?? "";
        EditDuration = value?.DurationSeconds ?? 5;
    }

    // ---------- Commands ----------

    [RelayCommand]
    private void New() => SelectedTask = null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (SelectedTask is null)
        {
            var item = new WorkItemViewModel
            {
                Name = EditName.Trim(),
                Description = EditDescription,
                DurationSeconds = EditDuration
            };
            Tasks.Add(item);
            SelectedTask = item;
        }
        else
        {
            SelectedTask.Name = EditName.Trim();
            SelectedTask.Description = EditDescription;
            SelectedTask.DurationSeconds = EditDuration;
        }
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(EditName)
        && EditDuration is > 0 and <= 600
        && SelectedTask is not { IsBusy: true };      // can't edit a running task

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        Tasks.Remove(SelectedTask!);
        SelectedTask = null;
    }

    private bool CanDelete() => SelectedTask is { IsBusy: false };

    // AllowConcurrentExecutions = true: by default an AsyncRelayCommand disables
    // itself while running, which would block starting a SECOND task.
    [RelayCommand(CanExecute = nameof(CanRun), AllowConcurrentExecutions = true)]
    private async Task RunAsync()
    {
        var item = SelectedTask!;
        using var cts = new CancellationTokenSource();
        item.Cancellation = cts;

        item.Progress = 0;
        item.RemainingSeconds = item.DurationSeconds;
        item.Status = WorkStatus.Queued;

        ExecutionStack.Remove(item);      // if it was run before
        ExecutionStack.Insert(0, item);   // newest on top: a stack
        RefreshCommands();

        try
        {
            await _slots.WaitAsync(cts.Token);    // wait for a free slot (UI stays free)
            try
            {
                item.Status = WorkStatus.Running;
                RefreshCommands();

                var total = item.DurationSeconds;

                // Created on the UI thread → Report() callbacks come back to the UI thread.
                var progress = new Progress<double>(p =>
                {
                    item.Progress = p;
                    item.RemainingSeconds = (int)Math.Ceiling(total * (100 - p) / 100);
                });

                await _executor.RunAsync(total, progress, cts.Token);
                item.Status = WorkStatus.Completed;
            }
            finally
            {
                _slots.Release();                 // only release a slot we actually got
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = WorkStatus.Cancelled;   // cancelled while queued or running
        }
        finally
        {
            item.Cancellation = null;
            RefreshCommands();
        }
    }

    private bool CanRun() => SelectedTask is { IsBusy: false };

    // Command with a parameter: each card in the stack passes its own item.
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel(WorkItemViewModel? item) => item?.Cancellation?.Cancel();

    private bool CanCancel(WorkItemViewModel? item) => item is { IsBusy: true };

    [RelayCommand]
    private void ClearFinished()
    {
        foreach (var done in ExecutionStack.Where(t => !t.IsBusy).ToList())
            ExecutionStack.Remove(done);
    }

    /// <summary>
    /// Status changes don't go through the editor, so tell WPF to re-query CanExecute.
    /// </summary>
    private void RefreshCommands()
    {
        RunCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}
