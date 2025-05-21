namespace DTasks.Analyzer.Configuration;

internal readonly record struct InfrastructureMarshalingBuilderInvocation(
    InfrastructureMarshalingBuilderMethod Method,
    string TypeFullName);