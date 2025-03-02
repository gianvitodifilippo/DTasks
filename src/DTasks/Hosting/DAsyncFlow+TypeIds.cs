using DTasks.Marshaling;
using DTasks.Utils;
using System.Reflection;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private static readonly MethodInfo s_whenAllDAsyncMethod = typeof(DAsyncFlow).GetRequiredMethod(
        name: nameof(WhenAllDAsync),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
        parameterTypes: [typeof(int)]);

    private static readonly MethodInfo s_whenAllDAsyncGenericMethod = typeof(DAsyncFlow).GetRequiredMethod(
        name: nameof(WhenAllDAsync),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
        parameterTypes: [typeof(Dictionary<,>).MakeGenericType(typeof(int), Type.MakeGenericMethodParameter(0)), typeof(int)]);

    public static void RegisterTypeIds(ITypeResolverBuilder builder)
    {
        builder.Register(typeof(HostIndirectionStateMachine));
        builder.Register(typeof(HandleStateMachine));
        builder.Register(typeof(CompletedHandleStateMachine));
        builder.Register(typeof(WhenAllResultBranchStateMachine));
        builder.Register(typeof(WhenAnyStateMachine));
        builder.RegisterDAsyncMethod(s_whenAllDAsyncMethod);
    }

    public static void RegisterGenericTypeIds(ITypeResolverBuilder builder, Type resultType)
    {
        builder.RegisterDAsyncMethod(s_whenAllDAsyncGenericMethod.MakeGenericMethod(resultType));
    }
}
