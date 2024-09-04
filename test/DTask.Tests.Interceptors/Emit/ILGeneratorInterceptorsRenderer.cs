﻿using System.Collections.Immutable;
using System.Text;

namespace DTask.Tests.Interceptors.Emit;

internal readonly ref struct ILGeneratorInterceptorsRenderer(StringBuilder source)
{
    public void Render(ImmutableArray<InterceptionLocationInfo> locations)
    {
        AppendBaseline();

        foreach (var location in locations)
        {
            source.AppendLine();
            AppendInterceptsLocationAttribute(location);
        }

        AppendInterceptor();

        source.Append("""
            }
        }
        """);
    }

    private void AppendBaseline()
    {
        source.Append("""
            // <auto-generated />

            #nullable enable

            namespace System.Runtime.CompilerServices
            {
            #pragma warning disable CS9113 // Parameter is unread.
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute(string filePath, int line, int character) : Attribute { }
            #pragma warning restore CS9113 // Parameter is unread.
            }

            namespace DTasks.Generated.Emit
            {
                using System.Diagnostics;
                using System.Diagnostics.CodeAnalysis;
                using System.Reflection;
                using System.Reflection.Emit;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;

                public static class ILGeneratorInterceptors
                {
                    private static readonly ThreadLocal<ILGenerator?> _interceptorLocal = new();
                
                    public static IDisposable InterceptCalls(this ILGenerator il)
                    {
                        Debug.Assert(_interceptorLocal.Value is null);
                        
                        _interceptorLocal.Value = il;
                        return new InterceptionScope(il);
                    }
                
                    private class InterceptionScope(ILGenerator il) : IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            Debug.Assert(ReferenceEquals(il, _interceptorLocal.Value));
                            
                            _interceptorLocal.Value = null;
                        }
                    }
            """);
        source.AppendLine();
    }

    private void AppendInterceptsLocationAttribute(InterceptionLocationInfo location)
    {
        source
            .Append("        [InterceptsLocationAttribute(@\"")
            .Append(location.FilePath)
            .Append("\", ")
            .Append(location.Line)
            .Append(", ")
            .Append(location.Character)
            .AppendLine(")]");
    }

    private void AppendInterceptor()
    {
        source.AppendLine("""
                    public static ILGenerator GetILGenerator(this DynamicMethod method)
                    {
                        ILGenerator il = method.GetILGenerator();
                        if (_interceptorLocal.Value is not ILGenerator interceptor)
                            return il;

                        il.Emit(OpCodes.Ret);
                        return interceptor;
                    }
            """);
    }
}
