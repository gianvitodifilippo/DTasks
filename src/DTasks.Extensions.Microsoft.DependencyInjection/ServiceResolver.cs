using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal delegate bool ServiceResolver(ServiceTypeId typeId, [NotNullWhen(true)] out Type? serviceType);
