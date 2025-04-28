namespace DTasks.Configuration.DependencyInjection;

public interface IComponentDescriptor<out TComponent>
    where TComponent : notnull
{
    TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder);
}
