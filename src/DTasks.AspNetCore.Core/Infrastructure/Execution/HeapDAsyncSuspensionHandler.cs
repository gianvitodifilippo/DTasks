using System.Runtime.CompilerServices;
using DTasks.Configuration;
using DTasks.Infrastructure.State;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Infrastructure.Execution;

internal sealed class HeapDAsyncSuspensionHandler : PollingDAsyncSuspensionHandler
{
    private static readonly string s_remindersKey = "dtasks_aspnetcore_reminders";
    
    private readonly AspNetCoreDAsyncHost _host;
    private readonly IDAsyncHeap _heap;
    
    public HeapDAsyncSuspensionHandler(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<DTasksConfiguration>();
        _host = AspNetCoreDAsyncHost.Create(services);
        _heap = Configuration.CreateHostInfrastructure(_host).GetHeap();
    }

    protected override IServiceProvider Services { get; }

    protected override DTasksConfiguration Configuration { get; }

    protected override async IAsyncEnumerable<SuspensionReminder> GetRemindersAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Option<List<SuspensionReminder>> remindersOption = await _heap.LoadAsync<string, List<SuspensionReminder>>(s_remindersKey, cancellationToken);
        if (!remindersOption.HasValue)
            yield break;

        foreach (SuspensionReminder reminder in remindersOption.Value)
        {
            yield return reminder;
        }
    }

    protected override async Task AddReminderAsync(SuspensionReminder reminder, CancellationToken cancellationToken)
    {
        Option<List<SuspensionReminder>> remindersOption = await _heap.LoadAsync<string, List<SuspensionReminder>>(s_remindersKey, cancellationToken);
        List<SuspensionReminder> reminders = remindersOption.UnwrapOrElse(static () => []);
        reminders.Add(reminder);
        
        await _heap.SaveAsync(s_remindersKey, reminders, cancellationToken);
    }

    protected override async Task DeleteReminderAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        Option<List<SuspensionReminder>> remindersOption = await _heap.LoadAsync<string, List<SuspensionReminder>>(s_remindersKey, cancellationToken);
        if (!remindersOption.HasValue)
            return;
        
        List<SuspensionReminder> reminders = remindersOption.Value;
        
        SuspensionReminder? toRemove = null;
        foreach (SuspensionReminder reminder in reminders)
        {
            if (reminder.Id == id)
            {
                toRemove = reminder;
                break;
            }
        }

        if (toRemove is null)
            return;
        
        reminders.Remove(toRemove.Value);
        await _heap.SaveAsync(s_remindersKey, reminders, cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
        _host.Dispose();
    }
}