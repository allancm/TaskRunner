namespace TaskRunner.Models;

// Not called "TaskStatus" on purpose: that name already exists in
// System.Threading.Tasks and would collide (classic interview gotcha).
public enum WorkStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Cancelled
}
