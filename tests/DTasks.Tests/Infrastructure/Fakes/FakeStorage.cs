using DTasks.Utils;

namespace DTasks.Infrastructure.Fakes;

internal class FakeStorage
{
    private readonly List<DAsyncId> _ids = [];
    private readonly Dictionary<DAsyncId, DehydratedRunnable> _runnables = [];
    private readonly Dictionary<object, object?> _heap = [];
    
    public IReadOnlyList<DAsyncId> Ids => _ids;

    public void WriteRunnable(DAsyncId id, DehydratedRunnable runnable)
    {
        _ids.Add(id);
        _runnables.Add(id, runnable);
    }

    public DehydratedRunnable ReadRunnable(DAsyncId id)
    {
        return _runnables[id];
    }

    public void DeleteRunnable(DAsyncId id)
    {
        _runnables.Remove(id);
        _ids.Remove(id);
    }

    public void Save<TValue>(object key, TValue value)
    {
        _heap[key] = value;
    }

    public Option<TValue> Load<TValue>(object key)
    {
        if (!_heap.TryGetValue(key, out object? value))
            return Option<TValue>.None;
        
        return Option.Some((TValue)value!);
    }

    public void Delete(object key)
    {
        _heap.Remove(key);
    }
}