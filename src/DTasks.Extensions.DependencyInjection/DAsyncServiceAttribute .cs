namespace DTasks.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class DAsyncServiceAttribute : Attribute;