using DTasks.AspNetCore.Infrastructure;
using DTasks.AspNetCore.State;
using DTasks.Infrastructure.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.AspNetCore.Execution;

internal sealed class DefaultDAsyncSuspensionHandler(IServiceProvider services, IStateStore stateStore) : BackgroundService, IDAsyncSuspensionHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken); // TODO: Make configurable
            await PollRemindersAsync(stoppingToken);
        }
    }

    private async Task PollRemindersAsync(CancellationToken cancellationToken)
    {
        // Naive, evaluate parallel execution
        IAsyncEnumerable<SuspensionReminder> reminders = stateStore.GetRemindersAsync(cancellationToken);
        await foreach (SuspensionReminder reminder in reminders)
        {
            if (reminder.DueDateTime > DateTimeOffset.UtcNow)
                continue;

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            IServiceProvider scopeServices = scope.ServiceProvider;
                    
            AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(scopeServices);
            await host.ResumeAsync(reminder.Id, cancellationToken);
                
            await stateStore.DeleteReminderAsync(reminder.Id, cancellationToken);
        }
    }

    public Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        SuspensionReminder reminder = new(id, DateTimeOffset.UtcNow);
        return stateStore.AddReminderAsync(reminder, cancellationToken);
    }

    public Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken)
    {
        SuspensionReminder reminder = new(id, DateTimeOffset.UtcNow.Add(delay));
        return stateStore.AddReminderAsync(reminder, cancellationToken);
    }
}