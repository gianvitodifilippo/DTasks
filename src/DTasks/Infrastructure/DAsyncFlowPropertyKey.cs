namespace DTasks.Infrastructure;

public readonly struct DAsyncFlowPropertyKey<TProperty>(object key)
{
    public DAsyncFlowPropertyKey()
        : this(new object())
    {
    }

    public object Key => key;

    public override string ToString()
    {
        if (key.GetType() == typeof(object))
            return typeof(TProperty).Name;

        return $"{key} ({typeof(TProperty).Name}))";
    }
}
