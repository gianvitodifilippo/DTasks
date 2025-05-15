namespace DTasks.Configuration.DependencyInjection;

public interface IComponentProvider
{
    TComponent GetComponent<TComponent>(IComponentToken<TComponent> token);
}