﻿using DTasks.Marshaling;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection;

internal sealed class DAsyncServiceRegister(FrozenSet<Type> types, ITypeResolver typeResolver) : IDAsyncServiceRegister
{
    public bool IsDAsyncService(Type serviceType) => types.Contains(serviceType);

    public bool IsDAsyncService(TypeId typeId, [NotNullWhen(true)] out Type? serviceType)
    {
        Type type = typeResolver.GetType(typeId);
        if (types.Contains(type))
        {
            serviceType = type;
            return true;
        }

        serviceType = null;
        return false;
    }
}