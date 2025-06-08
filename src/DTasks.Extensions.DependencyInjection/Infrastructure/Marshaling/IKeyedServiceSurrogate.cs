using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal interface IKeyedServiceSurrogate
{
    [DisallowNull]
    object? Key { get; set; }
}
