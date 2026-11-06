using TaskRunner.Models;
using TaskRunner.Services;
using TaskRunner.ViewModels;
using Xunit;

namespace TaskRunner.Tests;

// Fakes: no real delays, no device. This is why the ViewModel depends on ITaskExecutor.
file class InstantExecutor : ITaskExecutor
{
    public Task RunAsync(int s, IProgress<double> p, CancellationToken ct) => Task.CompletedTask;
}

file class WaitForeverExecutor : ITaskExecutor
{
    public Task RunAsync(int s, IProgress<double> p, CancellationToken ct) =>
        Task.Delay(Timeout.Infinite, ct);   // only ends when cancelled
}

public class MainViewModelTests
{
    [Fact]
    public void Run_is_disabled_when_nothing_is_selected()
    {
        var vm = new MainViewModel(new InstantExecutor()) { SelectedTask = null };
        Assert.False(vm.RunCommand.CanExecute(null));
    }

    [Fact]
    public void Save_is_disabled_when_name_is_empty()
    {
        var vm = new MainViewModel(new InstantExecutor()) { SelectedTask = null, EditName = "" };
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Running_a_task_marks_it_completed_and_puts_it_on_the_stack()
    {
        var vm = new MainViewModel(new InstantExecutor());
        var task = vm.Tasks[0];
        vm.SelectedTask = task;

        await vm.RunCommand.ExecuteAsync(null);

        Assert.Equal(WorkStatus.Completed, task.Status);
        Assert.Contains(task, vm.ExecutionStack);
    }

    [Fact]
    public async Task Cancelling_a_running_task_marks_it_cancelled()
    {
        var vm = new MainViewModel(new WaitForeverExecutor());
        var task = vm.Tasks[0];
        vm.SelectedTask = task;

        var run = vm.RunCommand.ExecuteAsync(null);   // don't await yet: it's running
        Assert.True(task.IsBusy);

        vm.CancelCommand.Execute(task);
        await run;

        Assert.Equal(WorkStatus.Cancelled, task.Status);
    }
}
