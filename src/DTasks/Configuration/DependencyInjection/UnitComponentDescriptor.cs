namespace DTasks.Configuration.DependencyInjection;

internal sealed class UnitComponentDescriptor<TComponent>(IComponentToken<TComponent> token) : IComponentDescriptor<TComponent>
{
    public void Accept(IComponentDescriptorVisitor<TComponent> visitor)
    {
        visitor.VisitUnit(token);
    }

    public TReturn Accept<TReturn>(IComponentDescriptorVisitor<TComponent, TReturn> visitor)
    {
        return visitor.VisitUnit(token);
    }
}