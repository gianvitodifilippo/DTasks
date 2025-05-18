using DTasks.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DTasks.AspNetCore.Infrastructure.Execution;

internal sealed class StoreDAsyncSuspensionHandler : PollingDAsyncSuspensionHandler
{
    private readonly IReminderStore _store;
    private readonly ILogger<HeapDAsyncSuspensionHandler> _logger;
    
    public StoreDAsyncSuspensionHandler(IServiceProvider services, IReminderStore store)
    {
        Services = services;
        Configuration = services.GetRequiredService<DTasksConfiguration>();
        _store = store;
        _logger = services.GetRequiredService<ILogger<HeapDAsyncSuspensionHandler>>();
    }
    
    protected override IServiceProvider Services { get; }

    protected override DTasksConfiguration Configuration { get; }

    protected override ILogger<PollingDAsyncSuspensionHandler> Logger => _logger;

    protected override IAsyncEnumerable<SuspensionReminder> GetRemindersAsync(CancellationToken cancellationToken)
    {
        return _store.GetRemindersAsync(cancellationToken);
    }

    protected override Task AddReminderAsync(SuspensionReminder reminder, CancellationToken cancellationToken)
    {
        return _store.AddReminderAsync(reminder, cancellationToken);
    }

    protected override Task DeleteReminderAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        return _store.DeleteReminderAsync(id, cancellationToken);
    }
}