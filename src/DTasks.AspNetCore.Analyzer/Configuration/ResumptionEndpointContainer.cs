using DTasks.AspNetCore.Analyzer.Utils;

namespace DTasks.AspNetCore.Analyzer.Configuration;

internal readonly record struct ResumptionEndpointContainer(string Type, EquatableArray<string> Members);