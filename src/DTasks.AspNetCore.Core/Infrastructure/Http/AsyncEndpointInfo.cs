namespace DTasks.AspNetCore.Infrastructure.Http;

public class AsyncEndpointInfo // TODO: Make internal. This is only needed for the sample until we implement async endpoints
{
    // public AsyncEndpointStatus Status { get; set; }
    public string? Status { get; set; }
}

public class AsyncEndpointInfo<TResult> : AsyncEndpointInfo
{
    public TResult? Result { get; set; }
}