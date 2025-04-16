using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncSurrogatorExtensions
{
    public static bool TrySurrogate<T>(this IDAsyncSurrogator surrogator, in T value, ISurrogationAction action)
    {
        SurrogationActionWrapper wrapper = new(action);
        return surrogator.TrySurrogate(in value, ref wrapper);
    }

    public static bool TryRestore<T>(this IDAsyncSurrogator surrogator, TypeId typeId, IRestorationAction action)
    {
        RestorationActionWrapper wrapper = new(action);
        return surrogator.TryRestore<T, RestorationActionWrapper>(typeId, ref wrapper);
    }

    public static bool TryRestore<T>(this IDAsyncSurrogator surrogator, TypeId typeId, out RestorationResult result)
    {
        RestorationResultAction action = new();
        if (!surrogator.TryRestore<T, RestorationResultAction>(typeId, ref action))
        {
            result = default;
            return false;
        }

        result = action.ToResult();
        return true;
    }

    private readonly struct SurrogationActionWrapper(ISurrogationAction action) : ISurrogationAction
    {
        public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate) => action.SurrogateAs(typeId, surrogate);
    }

    private readonly struct RestorationActionWrapper(IRestorationAction action) : IRestorationAction
    {
        public void RestoreAs<TConverter>(Type surrogateType, ref TConverter converter) where TConverter : struct, ISurrogateConverter
            => action.RestoreAs(surrogateType, ref converter);

        public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
            => action.RestoreAs(surrogateType, converter);
    }

    private struct RestorationResultAction : IRestorationAction
    {
        public Type SurrogateType { get; private set; }

        public ISurrogateConverter Converter { get; private set; }

        public readonly RestorationResult ToResult() => new(SurrogateType, Converter);

        public void RestoreAs<TConverter>(Type surrogateType, ref TConverter converter)
            where TConverter : struct, ISurrogateConverter
        {
            SurrogateType = surrogateType;
            Converter = converter;
        }

        public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
        {
            SurrogateType = surrogateType;
            Converter = converter;
        }
    }
}
