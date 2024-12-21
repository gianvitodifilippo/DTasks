using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

internal interface IKeyedServiceToken
{
    [DisallowNull]
    object? Key { get; set; }
}
