using System.Collections.Immutable;

namespace DTasks.Infrastructure.Marshaling;

public abstract class DAsyncSurrogator : IDAsyncSurrogator
{
    public static readonly IDAsyncSurrogator Default = new DefaultDAsyncSurrogator();

    private DAsyncSurrogator()
    {
    }

    public abstract bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, ISurrogationAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    public abstract bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IRestorationAction
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
        public override bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action) => false;

        public override bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action) => false;
    }

    private sealed class AggregateDAsyncSurrogator(ImmutableArray<IDAsyncSurrogator> surrogators) : DAsyncSurrogator
    {
        public override bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        {
            foreach (IDAsyncSurrogator surrogator in surrogators)
            {
                if (surrogator.TrySurrogate(in value, ref action))
                    return true;
            }

            return false;
        }

        public override bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        {
            foreach (IDAsyncSurrogator surrogator in surrogators)
            {
                if (surrogator.TryRestore<T, TAction>(typeId, ref action))
                    return true;
            }

            return false;
        }
    }
}