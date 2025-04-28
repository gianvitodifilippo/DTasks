using System.Diagnostics.CodeAnalysis;

namespace DTasks.AspNetCore.Http;

internal static class DTasksHttpConstants
{
    [StringSyntax("Route")]
    public const string DTasksEndpointPrefix = "async";

    [StringSyntax("Route")]
    public const string DTasksEndpoint = $"{DTasksEndpointPrefix}/{{operationId}}";

    public const string DTasksStatusEndpoint = DTasksEndpoint;

    public const string DTasksStatusEndpointName = "DTasksStatus";
}