namespace DTasks.AspNetCore.Execution;

public delegate Task WebSuspensionCallback(IWebSuspensionContext context, CancellationToken cancellationToken);

public delegate Task WebSuspensionCallback<in TState>(IWebSuspensionContext context, TState state, CancellationToken cancellationToken);