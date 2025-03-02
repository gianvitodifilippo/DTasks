using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal interface IKeyedServiceToken
{
    [DisallowNull]
    object? Key { get; set; }
}
