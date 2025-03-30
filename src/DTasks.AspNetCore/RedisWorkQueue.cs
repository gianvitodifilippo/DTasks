using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DTasks.AspNetCore;

public class RedisWorkQueue(
    IDatabase redis,
    IServiceProvider services,
    ILogger<RedisWorkQueue> logger) : BackgroundService, IWorkQueue
{
    private static readonly RedisKey s_reminderKey = "reminders";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);

            HashEntry[] entries = await redis.HashGetAllAsync(s_reminderKey);
            if (entries.Length == 0)
                continue;

            await Task.WhenAll(entries.Select(async entry =>
            {
                try
                {
                    DateTime dueDate = DateTime.Parse(entry.Value.ToString());
                    if (dueDate > DateTime.UtcNow)
                        return;

                    DAsyncId id = DAsyncId.Parse(entry.Name.ToString());
                    await using AsyncServiceScope scope = services.CreateAsyncScope();
                    IServiceProvider scopeServices = scope.ServiceProvider;

                    AspNetCoreDAsyncHost host = scopeServices.GetRequiredService<AspNetCoreDAsyncHost>();
                    await host.ResumeAsync(id, stoppingToken);
                    await redis.HashDeleteAsync(s_reminderKey, entry.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error");
                }
            }));
        }
    }

    public Task YieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return AddReminderAsync(id, TimeSpan.Zero, cancellationToken);
    }

    public Task DelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return AddReminderAsync(id, delay, cancellationToken);
    }

    private async Task AddReminderAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await redis.HashSetAsync(s_reminderKey, id.ToString(), (DateTime.UtcNow.Add(delay)).ToString());
    }
}
