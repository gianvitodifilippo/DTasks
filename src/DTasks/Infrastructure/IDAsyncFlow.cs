using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlow
{
    DTasksConfiguration Configuration { get; }

    IDAsyncSurrogator Surrogator { get; }

    bool TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
}

public static class DAsyncFlowExtensions
{
    public static TProperty GetProperty<TProperty>(this IDAsyncFlow flow, DAsyncFlowPropertyKey<TProperty> key)
    {
        if (!flow.TryGetProperty(key, out TProperty? value))
            throw new KeyNotFoundException($"Could not find property corresponding to key {key}.");

        return value;
    }
}