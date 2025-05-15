using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Configuration;
using DTasks.Serialization.Json.Converters;

namespace DTasks.AspNetCore.Configuration;

internal sealed class AspNetCoreSerializationConfigurationBuilder(ISerializationConfigurationBuilder builder) : IAspNetCoreSerializationConfigurationBuilder
{
    private readonly List<Action<IJsonFormatConfigurationBuilder>> _jsonFormatConfigurationActions = [];

    public void Configure()
    {
        builder
            .UseJsonFormat(json =>
            {
                json.ConfigureSerializerOptions((rootScope, options) =>
                {
                    options.Converters.Add(new TypedInstanceJsonConverter<object>(rootScope.TypeResolver));
                    options.Converters.Add(new TypedInstanceJsonConverter<IDAsyncContinuationSurrogate>(rootScope.TypeResolver));
                });

                foreach (var action in _jsonFormatConfigurationActions)
                {
                    action(json);
                }
            });
    }

    IAspNetCoreSerializationConfigurationBuilder IAspNetCoreSerializationConfigurationBuilder.UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor)
    {
        builder.UseStateMachineSerializer(descriptor);
        return this;
    }

    IAspNetCoreSerializationConfigurationBuilder IAspNetCoreSerializationConfigurationBuilder.UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor)
    {
        builder.UseSerializer(descriptor);
        return this;
    }

    IAspNetCoreSerializationConfigurationBuilder IAspNetCoreSerializationConfigurationBuilder.UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor)
    {
        builder.UseStorage(descriptor);
        return this;
    }

    IAspNetCoreSerializationConfigurationBuilder IAspNetCoreSerializationConfigurationBuilder.ConfigureJsonFormat(Action<IJsonFormatConfigurationBuilder> configure)
    {
        _jsonFormatConfigurationActions.Add(configure);
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor)
    {
        builder.UseStateMachineSerializer(descriptor);
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor)
    {
        builder.UseSerializer(descriptor);
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor)
    {
        builder.UseStorage(descriptor);
        return this;
    }
}
