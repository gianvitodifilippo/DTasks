using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure.Marshaling;

public abstract class DAsyncSurrogator : IDAsyncSurrogator
{
    public static readonly IDAsyncSurrogator Default = new DefaultDAsyncSurrogator();

    private DAsyncSurrogator()
    {
    }

    public abstract bool TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    public abstract bool TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    public static IDAsyncSurrogator Aggregate(ImmutableArray<IDAsyncSurrogator> surrogators)
    {
        return new AggregateDAsyncSurrogator(surrogators);
    }

    public static IDAsyncSurrogator Aggregate(IEnumerable<IDAsyncSurrogator> surrogators)
    {
        return new AggregateDAsyncSurrogator([.. surrogators]);
    }

    private sealed class DefaultDAsyncSurrogator : DAsyncSurrogator
    {
        public override bool TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller) => false;

        public override bool TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        {
            value = default;
            return false;
        }
    }

    private sealed class AggregateDAsyncSurrogator(ImmutableArray<IDAsyncSurrogator> surrogators) : DAsyncSurrogator
    {
        public override bool TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
        {
            foreach (IDAsyncSurrogator surrogator in surrogators)
            {
                if (surrogator.TrySurrogate(in value, ref marshaller))
                    return true;
            }

            return false;
        }

        public override bool TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        {
            foreach (IDAsyncSurrogator surrogator in surrogators)
            {
                if (surrogator.TryRestore(typeId, ref unmarshaller, out value))
                    return true;
            }

            value = default;
            return false;
        }
    }
}
