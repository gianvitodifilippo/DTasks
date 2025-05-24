namespace DTasks.AspNetCore.Analyzer.Routing;

public readonly record struct ParameterInfo(
    string Name,
    string Type,
    string? Binding);