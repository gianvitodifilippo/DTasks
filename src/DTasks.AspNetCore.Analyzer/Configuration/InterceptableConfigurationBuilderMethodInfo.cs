using DTasks.AspNetCore.Analyzer.Utils;

namespace DTasks.AspNetCore.Analyzer.Configuration;

internal sealed record InterceptableConfigurationBuilderMethodInfo(
    bool IsStatic,
    bool IsExtension,
    string Name,
    string ContainingTypeFullName,
    EquatableArray<string> ParameterTypes,
    string ReturnType,
    int ConfigurationBuilderParameterPosition);