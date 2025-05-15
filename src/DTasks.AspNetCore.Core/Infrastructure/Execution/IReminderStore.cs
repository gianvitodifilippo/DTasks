using System.ComponentModel;

namespace DTasks.AspNetCore.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IReminderStore
{
    IAsyncEnumerable<SuspensionReminder> GetRemindersAsync(CancellationToken cancellationToken = default);
    
    Task AddReminderAsync(SuspensionReminder reminder, CancellationToken cancellationToken = default);
    
    Task DeleteReminderAsync(DAsyncId id, CancellationToken cancellationToken = default);
}
