namespace DTasks.Configuration.DependencyInjection;

public interface IComponentDescriptor<out TComponent>
{
    void Accept(IComponentDescriptorVisitor<TComponent> visitor);
    
    TReturn Accept<TReturn>(IComponentDescriptorVisitor<TComponent, TReturn> visitor);
}