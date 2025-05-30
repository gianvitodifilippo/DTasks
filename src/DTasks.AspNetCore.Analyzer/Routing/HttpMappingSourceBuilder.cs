using System.Diagnostics;
using System.Text;
using DTasks.AspNetCore.Analyzer.Utils;

namespace DTasks.AspNetCore.Analyzer.Routing;

internal ref struct HttpMappingSourceBuilder(StringBuilder sb)
{
    public void Begin()
    {
        sb.AppendLine("""
            // <auto-generated />
            
            namespace Microsoft.AspNetCore.Routing;
            
            [global::System.CodeDom.Compiler.GeneratedCodeAttribute("DTasks.AspNetCore.Analyzer", "0.4.0.0")]
            internal static class DTasksEndpointBuilderExtensions
            {
            """);
    }

    public void End()
    {
        sb.Append("""
            }
            """);
    }

    public void EmitMapMethod(MapMethodInvocation invocation)
    {
        (string methodName, string mapMethodName) = invocation.Method switch
        {
            MapMethod.All => ("MapAsync", "Map"),
            MapMethod.Get => ("MapAsyncGet", "MapGet"),
            MapMethod.Post => ("MapAsyncPost", "MapPost"),
            MapMethod.Put => ("MapAsyncPut", "MapPut"),
            MapMethod.Delete => ("MapAsyncDelete", "MapDelete"),
            MapMethod.Patch => ("MapAsyncPatch", "MapPatch"),
            _ => DefaultMethodNames(invocation.Method)
        };
        
        sb
            .Append("    public static global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder ")
            .Append(methodName)
            .Append("(this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, ")
            .Append(invocation.PatternType)
            .Append(" pattern, global::System.Func<");

        for (int i = 0; i < invocation.Parameters.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(", ");
            }
            
            sb.Append(invocation.Parameters[i].Type);
        }

        sb.Append(", global::DTasks.DTask");
        if (invocation.ResultType is not null)
        {
            sb
                .Append('<')
                .Append(invocation.ResultType)
                .Append('>');
        }

        sb
            .AppendLine("> handler)")
            .Append("""
                    {
                        return global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.
                """)
            .Append(mapMethodName)
            .Append("(endpoints, pattern, (global::Microsoft.AspNetCore.Http.HttpContext httpContext");

        foreach (ParameterInfo parameter in invocation.Parameters)
        {
            sb.Append(", ");
            
            if (parameter.Binding is not null)
            {
                sb
                    .Append('[')
                    .Append(parameter.Binding)
                    .Append("] ");
            }

            sb
                .Append(parameter.Type)
                .Append(' ')
                .Append(parameter.Name);
        }

        sb.Append(") => global::Microsoft.AspNetCore.Http.DTasksHttpContextExtensions.RunAsync(httpContext, handler(");
        
        for (var i = 0; i < invocation.Parameters.Length; i++)
        {
            var parameter = invocation.Parameters[i];

            if (i != 0)
            {
                sb.Append(", ");
            }
            
            sb.Append(parameter.Name);
        }

        sb
            .AppendLine(")));")
            .AppendLine("    }");

        static (string, string) DefaultMethodNames(MapMethod method)
        {
            Debug.Fail($"Unhandled method ('{method}).");
            return ("MapAsync", "Map");
        }
    }
}