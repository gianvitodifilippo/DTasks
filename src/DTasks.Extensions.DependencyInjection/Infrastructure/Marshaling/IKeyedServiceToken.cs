using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal interface IKeyedServiceToken
{
    [DisallowNull]
    object? Key { get; set; }
}
