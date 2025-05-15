using DTasks.Configuration;
using DTasks.Infrastructure.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.AspNetCore.Infrastructure.Execution;

internal abstract class PollingDAsyncSuspensionHandler : BackgroundService, IDAsyncSuspensionHandler
{
    protected abstract IServiceProvider Services { get; }
    
    protected abstract DTasksConfiguration Configuration { get; }
    
    protected abstract IAsyncEnumerable<SuspensionReminder> GetRemindersAsync(CancellationToken cancellationToken);
    
    protected abstract Task AddReminderAsync(SuspensionReminder reminder, CancellationToken cancellationToken);
    
    protected abstract Task DeleteReminderAsync(DAsyncId id, CancellationToken cancellationToken);
    
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
        IAsyncEnumerable<SuspensionReminder> reminders = GetRemindersAsync(cancellationToken);
        await foreach (SuspensionReminder reminder in reminders)
        {
            if (reminder.DueDateTime > DateTimeOffset.UtcNow)
                continue;

            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            IServiceProvider scopeServices = scope.ServiceProvider;
                    
            AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(scopeServices);
            await Configuration.ResumeAsync(host, reminder.Id, cancellationToken);
                
            await DeleteReminderAsync(reminder.Id, cancellationToken);
        }
    }

    public Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        SuspensionReminder reminder = new(id, DateTimeOffset.UtcNow);
        return AddReminderAsync(reminder, cancellationToken);
    }

    public Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken)
    {
        SuspensionReminder reminder = new(id, DateTimeOffset.UtcNow.Add(delay));
        return AddReminderAsync(reminder, cancellationToken);
    }
}