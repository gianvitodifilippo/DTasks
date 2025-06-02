using System.Reflection;
using DTasks.Configuration;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    // private static readonly MethodInfo s_whenAllDAsyncMethod = typeof(DAsyncFlow).GetRequiredMethod(
    //     name: nameof(WhenAllDAsync),
    //     genericParameterCount: 0,
    //     bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
    //     parameterTypes: [typeof(int)]);
    //
    // private static readonly MethodInfo s_whenAllDAsyncGenericMethod = typeof(DAsyncFlow).GetRequiredMethod(
    //     name: nameof(WhenAllDAsync),
    //     genericParameterCount: 1,
    //     bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
    //     parameterTypes: [typeof(Dictionary<,>).MakeGenericType(typeof(int), Type.MakeGenericMethodParameter(0)), typeof(int)]);

    internal static void ConfigureMarshaling(IMarshalingConfigurationBuilder builder) => builder
        .RegisterSurrogatableType<object>()
        .RegisterSurrogatableType<IDAsyncRunnable>()
        .RegisterSurrogatableType<DTask>()
        .RegisterTypeId(typeof(IndirectionStateMachine), nameof(IndirectionStateMachine));
        // .RegisterSurrogatableType<object>()
        // .RegisterSurrogatableType<DTask>()
        // .RegisterSurrogatableType<HandleRunnable>()
        // .RegisterTypeId(typeof(HandleStateMachine))
        // .RegisterTypeId(typeof(CompletedHandleStateMachine))
        // .RegisterTypeId(typeof(WhenAllResultBranchStateMachine))
        // .RegisterTypeId(typeof(WhenAnyStateMachine))
        // .RegisterDAsyncMethod(s_whenAllDAsyncMethod);

    public static void RegisterGenericTypeIds(IMarshalingConfigurationBuilder builder, Type resultType)
    {
        // builder.RegisterDAsyncMethod(s_whenAllDAsyncGenericMethod.MakeGenericMethod(resultType)); // TODO: This won't work with NativeAOT
    }
}
