using DTasks.Configuration;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DTasks.Analyzer.Configuration;

public class AutoConfigurationGeneratorTests
{
    [Fact]
    public async Task GenericDTaskLocalInDAsyncMethod_ProducesCallToSurrogateDTaskOf()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static async DTask M()
                    {
                        DTask<int> t = null!;
                    }
                }
                """,
            expectedInvocations: """
                SurrogateDTaskOf<global::System.Int32>()
                """);
    }
    
    [Fact]
    public async Task GenericDTaskParameterInDAsyncMethod_ProducesCallToSurrogateDTaskOf()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static async DTask M(DTask<int> t)
                    {
                    }
                }
                """,
            expectedInvocations: """
                SurrogateDTaskOf<global::System.Int32>()
                """);
    }
    
    [Fact]
    public async Task CallToGenericWhenAll_ProducesCallToAwaitWhenAllOf()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static void M()
                    {
                        DTask.WhenAll((DTask<int>)null);
                    }
                }
                """,
            expectedInvocations: """
                AwaitWhenAllOf<global::System.Int32>()
                """);
    }
    [Fact]
    public async Task GenericDTaskLocalAndCallToGenericWhenAllInDAsyncMethod_ProducesCallToSurrogateDTaskOfAndToAwaitWhenAllOf()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static async DTask M()
                    {
                        DTask<int> t = null!;
                        DTask.WhenAll(t);
                    }
                }
                """,
            expectedInvocations: """
                SurrogateDTaskOf<global::System.Int32>()
                AwaitWhenAllOf<global::System.Int32>()
                """);
    }
    
    [Fact]
    public async Task NonGenericDTaskLocalInDAsyncMethod_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static async DTask M()
                    {
                        DTask t = null!;
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task GenericDTaskLocalInNonDAsyncMethod_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static async System.Threading.Tasks.Task M()
                    {
                        DTask<int> t = null!;
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task GenericDTaskLocalInNonAsyncMethod_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                static class C
                {
                    static void M()
                    {
                        DTask<int> t = null!;
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task DAsyncMethodInNonStaticClass_ProducesCallToSurrogateDTaskOf()
    {
        await RunTestAsync(
            source: """
                class C
                {
                    async DTask M()
                    {
                    }
                }
                """,
            expectedInvocations: """
                SurrogateDTaskOf<global::DTasks.Analyzer.Tests.C>()
                """);
    }
    
    [Fact]
    public async Task StaticDAsyncMethod_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                class C
                {
                    static async DTask M()
                    {
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task NonDAsyncMethodInNonStaticClass_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                class C
                {
                    async System.Threading.Tasks.Task M()
                    {
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task DAsyncMethodInGenericClass_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                class C<T>
                {
                    async DTask M()
                    {
                    }
                }
                """,
            expectedInvocations: "");
    }
    
    [Fact]
    public async Task GenericDAsyncMethod_ProducesNoCalls()
    {
        await RunTestAsync(
            source: """
                class C
                {
                    async DTask M<T>()
                    {
                    }
                }
                """,
            expectedInvocations: "");
    }

    private static async Task RunTestAsync(string source, string expectedInvocations)
    {
        source = $"""
            using DTasks;
            
            namespace DTasks.Analyzer.Tests;
            
            {source}
            """;

        if (expectedInvocations != "")
        {
            expectedInvocations = string.Concat(expectedInvocations
                .Split(Environment.NewLine)
                .Select(invocation => $"{Environment.NewLine}                infrastructure.{invocation};"));
        }
            
        string expected = $$"""
            // <auto-generated />
            
            namespace DTasks.Configuration;
            
            [global::System.CodeDom.Compiler.GeneratedCodeAttribute("DTasks.Analyzer", "0.4.0.0")]
            internal static class DefaultMarshalingDTasksConfigurationBuilderExtensions
            {
                public static TBuilder AutoConfigure<TBuilder>(this TBuilder builder)
                    where TBuilder : global::DTasks.Configuration.IDTasksConfigurationBuilder
                {
                    builder.ConfigureMarshaling(marshaling => marshaling
                        .ConfigureInfrastructure(infrastructure =>
                        {{{expectedInvocations}}
                        }));
                    return builder;
                }
            }
            """;
        
        var test = new CSharpSourceGeneratorTest<AutoConfigurationGenerator, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState =
            {
                Sources = { source },
                GeneratedSources =
                {
                    (typeof(AutoConfigurationGenerator), "DTasks.Analyzer.Marshaling.g.cs", expected)
                },
                AdditionalReferences = { typeof(DTask).Assembly, typeof(DTasksConfiguration).Assembly }
            }
        };
        
        await test.RunAsync();
    }
}