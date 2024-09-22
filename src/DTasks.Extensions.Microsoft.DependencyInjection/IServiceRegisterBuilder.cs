namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceRegisterBuilder
{
    ServiceTypeId AddServiceType(Type serviceType);

    IServiceRegister Build();
}
