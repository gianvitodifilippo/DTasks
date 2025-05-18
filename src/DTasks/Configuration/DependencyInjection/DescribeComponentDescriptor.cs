namespace DTasks.Configuration.DependencyInjection;

internal sealed class DescribeComponentDescriptor<TComponent>(
    Func<IComponentProvider, TComponent> createComponent,
    bool transient) : IComponentDescriptor<TComponent>
{
    public void Accept(IComponentDescriptorVisitor<TComponent> visitor)
    {
        visitor.VisitDescribe(createComponent, transient);
    }

    public TReturn Accept<TReturn>(IComponentDescriptorVisitor<TComponent, TReturn> visitor)
    {
        return visitor.VisitDescribe(createComponent, transient);
    }
}
