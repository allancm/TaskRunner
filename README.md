# TaskRunner — WPF / MVVM practice of basics and UI

## Run it
```
cd TaskRunner
dotnet run --project TaskRunner
```
Or open the folder in Visual Studio, set **TaskRunner** as the startup project, and press F5.

Tests:
```
dotnet test TaskRunner.Tests
```
To put both in one solution:
```
dotnet new sln -n TaskRunner
dotnet sln add TaskRunner/TaskRunner.csproj TaskRunner.Tests/TaskRunner.Tests.csproj
```

## What to try
1. Select a task → it appears on the left. Click **New** → empty editor. Leave Name empty and see **Save** disable itself.
2. Select a task and click **▶ Run** → it slides into the execution stack.
3. Run 3–4 tasks quickly → only 2 are **Running**, the rest wait as **Queued**.
4. **Cancel** a queued task and a running one.
5. While a task runs, select it → **Save / Delete / Run** are disabled.

## What each piece teaches (interview talking points)
| Piece | Concept |
|---|---|
| `WorkItemViewModel` with `[ObservableProperty]` | INotifyPropertyChanged on items, so the list updates when the status changes |
| `ObservableCollection` | The list updates when items are added or removed |
| `[RelayCommand(CanExecute = …)]` | Buttons enable and disable themselves |
| `[NotifyCanExecuteChangedFor]` + `RefreshCommands()` | When and why to re-query CanExecute |
| `AllowConcurrentExecutions = true` | By default an async command blocks itself while running |
| `SemaphoreSlim(2,2)` | Limit concurrency; a queue for free |
| `Progress<double>` | Reports from background work land on the UI thread |
| `CancellationTokenSource` | Cancel while queued or running; `OperationCanceledException` |
| `ITaskExecutor` + DI (Generic Host) | Swap in a fake for tests (see `TaskRunner.Tests`) |
| `WorkStatus` (not `TaskStatus`) | Avoids the name collision with `System.Threading.Tasks.TaskStatus` |
| DataTriggers + `x:Static` | Badge colors without converters |
| EventTrigger on `Loaded` | Slide-in animation; a DataTrigger with EnterActions for the pulse |

## Ideas to extend it later
- Persist tasks to a JSON file through an `ITaskRepository`.
- Validation with `ObservableValidator` / `INotifyDataErrorInfo`.
- A "Run all" button, and a slider for MaxParallel.
- Replace `Task.Delay` with CPU work to *see* why `Task.Run` is needed.
