using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class DAsyncInfrastructureBuilder : IComponentProviderBuilder
{
    private readonly Dictionary<TokenKey, object> _tokens = [];
    private IComponentToken<IDAsyncHeap>? _heapToken;
    private IComponentToken<IDAsyncStack>? _stackToken;
    private ImmutableArray<IComponentToken<IDAsyncSurrogator>> _surrogatorTokens = [];
    private IComponentToken<IDAsyncCancellationProvider>? _cancellationProviderToken;
    private IComponentToken<IDAsyncSuspensionHandler>? _suspensionHandlerToken;

    public Func<IComponentProvider, IDAsyncHeap> HeapAccessor
    {
        get
        {
            if (_heapToken is null)
                return MissingRequiredComponent<IDAsyncHeap>("heap");

            return provider => provider.GetComponent(_heapToken);
        }
    }

    public Func<IComponentProvider, IDAsyncStack> StackAccessor
    {
        get
        {
            if (_stackToken is null)
                return MissingRequiredComponent<IDAsyncStack>("stack");

            return provider => provider.GetComponent(_stackToken);
        }
    }

    public Func<IComponentProvider, IDAsyncSurrogator> SurrogatorAccessor
    {
        get
        {
            return _surrogatorTokens.Length switch
            {
                0 => provider => DAsyncSurrogator.Default,
                1 => provider => provider.GetComponent(_surrogatorTokens[0]),
                _ => provider => provider.GetComponent(new AggregateComponentToken<IDAsyncSurrogator>(_surrogatorTokens, DAsyncSurrogator.Aggregate))
            };
        }
    }

    public Func<IComponentProvider, IDAsyncCancellationProvider> CancellationProviderAccessor
    {
        get
        {
            if (_cancellationProviderToken is null)
                return provider => DAsyncCancellationProvider.Default;

            return provider => provider.GetComponent(_cancellationProviderToken);
        }
    }

    public Func<IComponentProvider, IDAsyncSuspensionHandler> SuspensionHandlerAccessor
    {
        get
        {
            if (_suspensionHandlerToken is null)
                return provider => DAsyncSuspensionHandler.Default;

            return provider => provider.GetComponent(_suspensionHandlerToken);
        }
    }
    
    public IDAsyncInfrastructure Build(DTasksConfigurationBuilder configurationBuilder)
    {
        return new DAsyncInfrastructure(this, configurationBuilder);
    }

    public void UseHeap(IComponentDescriptor<IDAsyncHeap>? descriptor)
    {
        if (descriptor is null)
            return;
        
        _heapToken = UseComponent(descriptor);
    }

    public void UseStack(IComponentDescriptor<IDAsyncStack>? descriptor)
    {
        if (descriptor is null)
            return;

        _stackToken = UseComponent(descriptor);
    }

    public void UseSurrogators(IEnumerable<IComponentDescriptor<IDAsyncSurrogator>> descriptors)
    {
        _surrogatorTokens = [..descriptors.Select(UseComponent)];
    }

    public void UseCancellationProvider(IComponentDescriptor<IDAsyncCancellationProvider>? descriptor)
    {
        if (descriptor is null)
            return;

        _cancellationProviderToken = UseComponent(descriptor);
    }

    public void UseSuspensionHandler(IComponentDescriptor<IDAsyncSuspensionHandler>? descriptor)
    {
        if (descriptor is null)
            return;

        _suspensionHandlerToken = UseComponent(descriptor);
    }

    private IComponentToken<TComponent> UseComponent<TComponent>(IComponentDescriptor<TComponent> descriptor)
    {
        return ((IComponentProviderBuilder)this).GetTokenInRootScope(descriptor);
    }

    InfrastructureComponentToken<TComponent> IComponentProviderBuilder.GetTokenInRootScope<TComponent>(IComponentDescriptor<TComponent> descriptor)
    {
        return GetToken(descriptor, new RootScopeTokenFactory<TComponent>(this));
    }

    InfrastructureComponentToken<TComponent> IComponentProviderBuilder.GetTokenInHostScope<TComponent>(IComponentDescriptor<TComponent> descriptor)
    {
        return GetToken(descriptor, new HostScopeTokenFactory<TComponent>(this));
    }

    InfrastructureComponentToken<TComponent> IComponentProviderBuilder.GetTokenInFlowScope<TComponent>(IComponentDescriptor<TComponent> descriptor)
    {
        return GetToken(descriptor, new FlowScopeTokenFactory<TComponent>(this));
    }

    private InfrastructureComponentToken<TComponent> GetToken<TComponent>(IComponentDescriptor<TComponent> descriptor, TokenFactory<TComponent> factory)
    {
        TokenKey key = new(typeof(TComponent), descriptor);
        if (_tokens.TryGetValue(key, out object? untypedToken))
            return (InfrastructureComponentToken<TComponent>)untypedToken;

        InfrastructureComponentToken<TComponent> token = descriptor.Accept(factory);
        _tokens.Add(key, token);
        
        return token;
    }

    [DoesNotReturn]
    private static Func<IComponentProvider, TComponent> MissingRequiredComponent<TComponent>(string componentName)
    {
        throw new InvalidOperationException($"Required component '{componentName}' ({typeof(TComponent).Name}) was not configured.");
    }
    
    [method: DebuggerStepThrough]
    private abstract class TokenFactory<TComponent>(IComponentProviderBuilder builder) : IComponentDescriptorVisitor<TComponent, InfrastructureComponentToken<TComponent>>
    {
        public abstract InfrastructureComponentToken<TComponent> VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient);

        public InfrastructureComponentToken<TComponent> VisitUnit(IComponentToken<TComponent> token)
        {
            if (token == ComponentDescriptor.Tokens.Root)
                return (InfrastructureComponentToken<TComponent>)(object)RootScopeComponentToken.Instance;

            if (token == ComponentDescriptor.Tokens.Host)
                return (InfrastructureComponentToken<TComponent>)(object)HostScopeComponentToken.Instance;

            if (token == ComponentDescriptor.Tokens.Flow)
                return (InfrastructureComponentToken<TComponent>)(object)FlowScopeComponentToken.Instance;

            return token as InfrastructureComponentToken<TComponent> ?? throw new InvalidOperationException($"Unrecognized token of type '{token.GetType().FullName}'.");
        }

        public InfrastructureComponentToken<TComponent> VisitBound<TDependency>(IComponentDescriptor<TDependency> dependencyDescriptor, ComponentDescriptorResolver<TComponent, TDependency> resolve)
        {
            // Calling 'GetTokenInRootScope' here means that 'dependencyDescriptor' should either be a descriptor that was
            // previously constructed (e.g., a local variable) or it shouldn't capture tokens when constructed in place
            // (e.g., ComponentDescriptor.Describe). We can't detect if a token is captured, so misconfigured descriptor
            // may fail at runtime with a scope mismatch error when constructing the service.
            
            InfrastructureComponentToken<TDependency> dependencyToken = builder.GetTokenInRootScope(dependencyDescriptor);
            IComponentDescriptor<TComponent> resolvedDescriptor = resolve(dependencyToken);

            return dependencyToken.Bind(builder, resolvedDescriptor);
        }
    }

    [method: DebuggerStepThrough]
    private sealed class RootScopeTokenFactory<TComponent>(DAsyncInfrastructureBuilder builder) : TokenFactory<TComponent>(builder)
    {
        public override InfrastructureComponentToken<TComponent> VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient)
        {
            return new RootComponentToken<TComponent>(createComponent, transient);
        }
    }
    
    [method: DebuggerStepThrough]
    private sealed class HostScopeTokenFactory<TComponent>(DAsyncInfrastructureBuilder builder) : TokenFactory<TComponent>(builder)
    {
        public override InfrastructureComponentToken<TComponent> VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient)
        {
            return new HostComponentToken<TComponent>(createComponent, transient);
        }
    }
    
    [method: DebuggerStepThrough]
    private sealed class FlowScopeTokenFactory<TComponent>(DAsyncInfrastructureBuilder builder) : TokenFactory<TComponent>(builder)
    {
        public override InfrastructureComponentToken<TComponent> VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient)
        {
            return new FlowComponentToken<TComponent>(createComponent, transient);
        }
    }

    private readonly record struct TokenKey(Type ComponentType, object Descriptor);
}