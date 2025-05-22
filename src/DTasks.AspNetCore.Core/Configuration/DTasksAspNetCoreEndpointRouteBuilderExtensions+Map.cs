using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using DTasks;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Configuration;
using DTasks.Infrastructure;
using DTasks.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

public static partial class DTasksAspNetCoreEndpointRouteBuilderExtensions
{
    private const string DynamicAssemblyName = "DTasks.AspNetCore.Core.Dynamic";
    private const string MapEndpointUnreferencedCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may be trimmed if not directly referenced.";
    private const string MapEndpointDynamicCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may require generated code and aren't compatible with native AOT applications.";
    
    private static readonly ModuleBuilder s_dynamicModule = CreateDynamicModule();
    private static readonly MethodInfo s_runAsyncMethod = typeof(DTasksHttpContextExtensions).GetRequiredMethod(
        name: nameof(DTasksHttpContextExtensions.RunAsync),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(HttpContext), typeof(IDAsyncRunnable)]);

    public static IEndpointConventionBuilder MapAsyncGet(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapGet(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsyncPost(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapPost(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsyncPut(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapPut(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsyncDelete(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapDelete(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsyncPatch(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapPatch(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsyncMethods(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        IEnumerable<string> httpMethods,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.MapMethods(pattern, httpMethods, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsync(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.Map(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }
    
    public static IEndpointConventionBuilder MapAsync(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        AsyncRequestDelegate requestDelegate)
    {
        return endpoints.Map(pattern, httpContext => RunRequestDelegateAsync(httpContext, requestDelegate));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsyncGet(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.MapGet(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsyncPost(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        throw new NotImplementedException();
        return endpoints.MapPost(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsyncPut(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.MapPut(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsyncDelete(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.MapDelete(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsyncPatch(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.MapPatch(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsync(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.Map(pattern, TransformDAsyncHandler(handler));
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapAsync(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        Delegate handler)
    {
        return endpoints.Map(pattern, TransformDAsyncHandler(handler));
    }

    private static Task RunRequestDelegateAsync(HttpContext httpContext, AsyncRequestDelegate requestDelegate)
    {
        DTask task = requestDelegate(httpContext);
        return httpContext.RunAsync(task);
    }

    private static Delegate TransformDAsyncHandler(Delegate handler)
    {
        MethodInfo method = handler.Method;
        if (!method.ReturnType.IsAssignableTo(typeof(IDAsyncRunnable)))
            throw new ArgumentException($"The return type of the delegate should be assignable to '{nameof(IDAsyncRunnable)}'.", nameof(handler));

        Type closureType = CreateClosure(handler);
        Type delegateType = Expression.GetDelegateType([..method.GetParameters().Select(parameter => parameter.ParameterType), typeof(Task)]);
        
        MethodInfo invokeMethod = closureType.GetMethod(
            name: "Invoke",
            bindingAttr: BindingFlags.Static | BindingFlags.Public)!;
        
        return invokeMethod.CreateDelegate(delegateType, null);
    }

    private static Type CreateClosure(Delegate handler)
    {
        MethodInfo method = handler.Method;
        TypeBuilder closureType = s_dynamicModule.DefineType(method.Name + "Closure" + Guid.NewGuid().ToString("N"), TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);

        Type handlerType = handler.GetType();
        ParameterInfo[] parameters = method.GetParameters();
        
        MethodInfo handlerInvokeMethod = handlerType.GetRequiredMethod(
            name: "Invoke",
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: parameters.Map(parameter => parameter.ParameterType));
        
        FieldInfo handlerField = closureType.DefineField("handler", handlerType, FieldAttributes.Private | FieldAttributes.Static);

        MethodBuilder invokeMethod = closureType.DefineMethod(
            "Invoke",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(Task),
            [typeof(HttpContext), ..parameters
                .Where(parameter => parameter.ParameterType != typeof(HttpContext))
                .Select(parameter => parameter.ParameterType)]);

        invokeMethod.DefineParameter(1, ParameterAttributes.None, "httpContext");
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            ParameterBuilder transformedParameter = invokeMethod.DefineParameter(i + 2, parameter.Attributes, parameter.Name);

            foreach (CustomAttributeData attributeData in parameter.GetCustomAttributesData())
            {
                IEnumerable<object?> ctorArgs = attributeData.ConstructorArguments.Select(data => data.Value);

                IEnumerable<PropertyInfo> namedProps = attributeData.NamedArguments
                    .Where(data => !data.IsField)
                    .Select(data => (PropertyInfo)data.MemberInfo);

                IEnumerable<object?> propValues = attributeData.NamedArguments
                    .Where(a => !a.IsField)
                    .Select(a => a.TypedValue.Value);

                IEnumerable<FieldInfo> namedFields = attributeData.NamedArguments
                    .Where(data => data.IsField)
                    .Select(data => (FieldInfo)data.MemberInfo);

                IEnumerable<object?> fieldValues = attributeData.NamedArguments
                    .Where(data => data.IsField)
                    .Select(data => data.TypedValue.Value);

                CustomAttributeBuilder attributeBuilder = new(
                    attributeData.Constructor,
                    [.. ctorArgs],
                    [.. namedProps],
                    [.. propValues],
                    [.. namedFields],
                    [.. fieldValues]);

                transformedParameter.SetCustomAttribute(attributeBuilder);
            }
        }

        ILGenerator il = invokeMethod.GetILGenerator();
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldsfld, handlerField);
        
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(HttpContext))
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                EmitLoadArgument(il, i);
            }
        }

        il.Emit(OpCodes.Callvirt, handlerInvokeMethod);
        il.Emit(OpCodes.Call, s_runAsyncMethod);
        il.Emit(OpCodes.Ret);
        
        Type result = closureType.CreateType();
        result.GetRequiredField("handler", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, handler);
        return result;
    }

    private static ModuleBuilder CreateDynamicModule()
    {
        AssemblyName assemblyName = new(DynamicAssemblyName);

        AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        return dynamicAssembly.DefineDynamicModule(DynamicAssemblyName);
    }

    private static void EmitLoadArgument(ILGenerator il, int index)
    {
        switch (index)
        {
            case 0:
                il.Emit(OpCodes.Ldarg_1);
                break;
            
            case 1:
                il.Emit(OpCodes.Ldarg_2);
                break;
            
            case 2:
                il.Emit(OpCodes.Ldarg_3);
                break;
            
            default:
                il.Emit(OpCodes.Ldarg_S, (byte)index);
                break;
        }
    }
}