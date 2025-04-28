namespace DTasks.AspNetCore.Infrastructure.Http;

internal enum AsyncEndpointStatus
{
    Running,
    Succeeded,
    Faulted,
    Canceled
}