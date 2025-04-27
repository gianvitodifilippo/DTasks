using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncScope
{
    DTasksConfiguration IDAsyncFlow.Configuration => Configuration;

    IDAsyncSurrogator IDAsyncFlow.Surrogator => this;

    bool IDAsyncFlow.TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetProperty(key, out value);
    }

    TComponent IDAsyncScope.GetScopedComponent<TComponent>(IComponentDescriptor<TComponent> descriptor, FlowImplementationFactory<TComponent> createComponent)
    {
        if (TryGetComponent(descriptor, out TComponent? component))
            return component;

        Debug.Assert(!_usedPropertyInScopedComponent);
        
        component = createComponent(this);
        AddComponent(descriptor, component);
        _usedPropertyInScopedComponent = false;

        return component;
    }

    TComponent IDAsyncScope.GetBoundComponent<TComponent, TDependency>(
        IComponentProvider<TDependency> dependencyProvider,
        IComponentDescriptor<TComponent> descriptor,
        DescriptorResolver<TComponent, TDependency> resolve)
    {
        return GetBoundComponent(dependencyProvider, descriptor, resolve);
    }

    private bool TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        if (!_properties.TryGetValue(key.Key, out object? untypedValue) || untypedValue is not TProperty typedValue)
        {
            value = default;
            return false;
        }

        _usedPropertyInScopedComponent = _isCreatingComponent;
        value = typedValue;
        return true;
    }

    private TComponent GetBoundComponent<TComponent, TDependency>(
        IComponentProvider<TDependency> dependencyProvider,
        IComponentDescriptor<TComponent> descriptor,
        DescriptorResolver<TComponent, TDependency> resolve)
        where TComponent : notnull
        where TDependency : notnull
    {
        if (TryGetComponent(descriptor, out TComponent? component))
            return component;

        Debug.Assert(!_usedPropertyInScopedComponent);

        TDependency dependency = dependencyProvider.GetComponent(this);
        IComponentDescriptor<TComponent> resolvedDescriptor = resolve(dependency);

        component = resolvedDescriptor.Build(new BoundComponentBuilder<TComponent>(descriptor, this));
        _usedPropertyInScopedComponent = false;

        return component;
    }

    private bool TryGetComponent<TComponent>(IComponentDescriptor<TComponent> descriptor, [NotNullWhen(true)] out TComponent? component)
        where TComponent : notnull
    {
        if (_components.TryGetValue(descriptor, out object? untypedComponent))
        {
            component = (TComponent)untypedComponent;
            return true;
        }

        if (_scopedComponents.TryGetValue(descriptor, out untypedComponent))
        {
            _usedPropertyInScopedComponent = true;
            
            component = (TComponent)untypedComponent;
            return true;
        }
    
        component = default;
        return false;
    }

    private void AddComponent<TComponent>(IComponentDescriptor<TComponent> descriptor, TComponent component)
        where TComponent : notnull
    {
        Dictionary<object, object> components = _usedPropertyInScopedComponent
            ? _scopedComponents
            : _components;

        components.Add(descriptor, component);
    }

    [method: DebuggerStepThrough]
    private sealed class BoundComponentBuilder<TComponent>(
        IComponentDescriptor<TComponent> descriptor,
        DAsyncFlow flow) : IDAsyncInfrastructureBuilder<TComponent, TComponent>
        where TComponent : notnull
    {
        public TComponent Unit(TComponent component)
        {
            flow.AddComponent(descriptor, component);
            return component;
        }

        public TComponent Singleton(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            TComponent component = createComponent(flow.Configuration);
            flow.AddComponent(descriptor, component);
            return component;
        }

        public TComponent Scoped(FlowImplementationFactory<TComponent> createComponent)
        {
            TComponent component = createComponent(flow);
            flow.AddComponent(descriptor, component);
            return component;
        }

        public TComponent RootTransient(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            return createComponent(flow.Configuration);
        }

        public TComponent FlowTransient(FlowImplementationFactory<TComponent> createComponent)
        {
            return createComponent(flow);
        }

        public TComponent Bind<TDependency>(IComponentDescriptor<TDependency> dependencyDescriptor, DescriptorResolver<TComponent, TDependency> resolve)
            where TDependency : notnull
        {
            IComponentProvider<TDependency> dependencyProvider = flow.Configuration.CreateProvider(dependencyDescriptor);
            return flow.GetBoundComponent(dependencyProvider, descriptor, resolve);
        }
    }
}
