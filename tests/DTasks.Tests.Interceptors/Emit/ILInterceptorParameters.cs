using System.Collections.Immutable;

namespace DTasks.Tests.Interceptors.Emit;

internal record struct ILInterceptorParameters(
    ImmutableArray<InterceptionLocationInfo> MethodBuilderInterceptionLocations,
    ImmutableArray<InterceptionLocationInfo> ConstructorBuilderInterceptionLocations);
