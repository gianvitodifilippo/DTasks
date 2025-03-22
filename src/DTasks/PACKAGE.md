## About

**DTasks** provides the default implementation of the DTasks pattern using an extensible snapshot-based persistence model.
It enables durable and distributed workflows while keeping the API as close as possible to standard C# `async`/`await` semantics.

## Usage

Define an async method returning `DTask` or `DTask<TResult>` and use the built-in awaitable methods or those defined in other libraries.
`DTask` exposes an API similar to `Task`.

```cs
public async DTask<int> ProcessAndSumDAsync(int left, int right)
{
    DTask<int> processLeftTask = Processor.ProcessDAsync(left);
    DTask<int> processRightTask = Processor.ProcessDAsync(right);

    DTask processTask = DTask.WhenAll(processLeftTask, processRightTask);
    DTask timeoutTask = DTask.Delay(TimeSpan.FromDays(1));

    DTask winner = await DTask.WhenAny(processTask, timeoutTask);
    if (winner == timeoutTask)
        return 0;

    return processLeftTask.Result + processRightTask.Result;
}
```