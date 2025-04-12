using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

internal interface IDAsyncServiceRegisterBuilder
{
    TypeId AddServiceType(Type serviceType);

    IDAsyncServiceRegister Build(IDAsyncTypeResolver typeResolver);
}

