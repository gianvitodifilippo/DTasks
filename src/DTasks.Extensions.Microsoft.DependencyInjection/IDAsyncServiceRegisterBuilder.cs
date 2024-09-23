namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IDAsyncServiceRegisterBuilder
{
    ServiceTypeId AddServiceType(Type serviceType);

    IDAsyncServiceRegister Build();
}
