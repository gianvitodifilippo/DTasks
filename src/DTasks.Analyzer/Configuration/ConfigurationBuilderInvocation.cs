namespace DTasks.Analyzer.Configuration;

internal readonly record struct ConfigurationBuilderInvocation(
    ConfigurationBuilderMethod Method,
    string Argument);