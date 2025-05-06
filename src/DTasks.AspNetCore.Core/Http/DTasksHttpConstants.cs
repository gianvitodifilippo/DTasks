namespace DTasks.AspNetCore.Http;

internal static class DTasksHttpConstants
{
    public const string OperationIdParameterName = "operationId";
    
    public const string TypeIdParameterName = "typeId";

    public const string DTasksStatusEndpointTemplate = $"/async/{{{OperationIdParameterName}}}/status";

    public const string DTasksDefaultResumptionEndpointTemplate = $"/async/{{{OperationIdParameterName}}}/resume/{{{TypeIdParameterName}}}";
    
    public const string DTasksGetStatusEndpointName = "DTasks_GetStatus";
}