using DTasks.AspNetCore.Analyzer.Utils;

namespace DTasks.AspNetCore.Analyzer.Routing;

internal sealed record MapMethodInvocation(
    MapMethod Method,
    string PatternType,
    string? ResultType,
    EquatableArray<ParameterInfo> Parameters);