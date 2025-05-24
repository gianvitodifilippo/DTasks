using DTasks.Analyzer.Utils;

namespace DTasks.Analyzer.Configuration;

internal sealed record InterceptableConfigurationBuilderMethodInfo(
    bool IsStatic,
    bool IsExtension,
    string Name,
    string ContainingTypeFullName,
    EquatableArray<string> ParameterTypes,
    string ReturnType,
    int ConfigurationBuilderParameterPosition);