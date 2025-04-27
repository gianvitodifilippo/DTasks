using System.Collections.Immutable;

namespace DTasks.Infrastructure.Marshaling;

internal static class DAsyncSurrogator
{
    public static IDAsyncSurrogator Default = new DefaultDAsyncSurrogator();
    
    public static IDAsyncSurrogator Aggregate(IEnumerable<IDAsyncSurrogator> surrogators)
    {
        return new AggregateDAsyncSurrogator([..surrogators]);
    }
    
    private sealed class DefaultDAsyncSurrogator : IDAsyncSurrogator
    {
        bool IDAsyncSurrogator.TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        {
            return false;
        }

        bool IDAsyncSurrogator.TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        {
            return false;
        }
    }


    private sealed class AggregateDAsyncSurrogator(ImmutableArray<IDAsyncSurrogator> surrogators) : IDAsyncSurrogator
    {
        bool IDAsyncSurrogator.TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        {
             foreach (IDAsyncSurrogator surrogator in surrogators)
             {
                 if (surrogator.TrySurrogate(in value, ref action))
                     return true;
             }

             return false;
        }

        bool IDAsyncSurrogator.TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
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