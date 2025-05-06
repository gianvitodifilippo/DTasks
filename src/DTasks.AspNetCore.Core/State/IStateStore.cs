using System.ComponentModel;
using DTasks.AspNetCore.Execution;
using DTasks.Utils;

namespace DTasks.AspNetCore.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IStateStore
{
    Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull;

    Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull;

    Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull;

    IAsyncEnumerable<SuspensionReminder> GetRemindersAsync(CancellationToken cancellationToken = default);
    
    Task AddReminderAsync(SuspensionReminder reminder, CancellationToken cancellationToken = default);
    
    Task DeleteReminderAsync(DAsyncId id, CancellationToken cancellationToken = default);
}
