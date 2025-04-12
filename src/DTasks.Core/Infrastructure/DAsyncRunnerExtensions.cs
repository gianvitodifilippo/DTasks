using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncRunnerExtensions
{
    private static readonly IgnoreResultBuilder _ignoreResultBuilder = new();

    public static void Await(this IDAsyncRunner runner, Task task)
    {
        runner.Await(task, _ignoreResultBuilder);
    }

    public static void Continue(this IDAsyncRunner runner)
    {
        runner.Await(Task.CompletedTask, _ignoreResultBuilder);
    }

    private sealed class IgnoreResultBuilder : IDAsyncResultBuilder<Task>
    {
        public void SetException(Exception exception)
        {
            // Ignore exceptions
        }

        public void SetResult(Task result)
        {
            // Ignore result
        }
    }
}