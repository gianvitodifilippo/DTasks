namespace DTasks.AspNetCore.Infrastructure.Http;

internal class AsyncEndpointInfo
{
    // public AsyncEndpointStatus Status { get; set; }
    public string? Status { get; set; }
}

internal class AsyncEndpointInfo<TResult> : AsyncEndpointInfo
{
    public TResult? Result { get; set; }
}