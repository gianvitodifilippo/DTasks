using DTasks.Infrastructure;
using DTasks.Marshaling;
using DTasks.Serialization;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System.Text.Json;

namespace DTasks.AspNetCore;

public class AspNetCoreDAsyncHost(
    ITypeResolver typeResolver,
    Func<IDAsyncMarshaler, IDAsyncSerializer> serializerFactory,
    IDAsyncMarshaler marshaler,
    IDAsyncStorage storage,
    IDatabase redis,
    IWorkQueue workQueue) : DAsyncHost, IDAsyncContext
{
    private IResult? _result;
    private string? _operationId;
    private IDAsyncCallback? _callback;

    public IResult? Result => _result;

    public bool IsSyncContext { get; set; }

    protected override ITypeResolver TypeResolver => typeResolver;

    protected override IDAsyncMarshaler CreateMarshaler()
    {
        return marshaler;
    }

    protected override IDAsyncStateManager CreateStateManager(IDAsyncMarshaler marshaler)
    {
        IDAsyncSerializer serializer = serializerFactory(marshaler);
        return new BinaryDAsyncStateManager(serializer, storage);
    }

    protected override async Task OnSucceedAsync(CancellationToken cancellationToken = default)
    {
        if (_operationId is null)
        {
            // Sync path
            _result = Results.Ok();
            return;
        }
        else
        {
            // Async path
            await ExecuteSucceedAsync(cancellationToken);
        }
    }

    protected override async Task OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        if (_operationId is null)
        {
            // Sync path
            _result = result as IResult ?? Results.Ok(result);
            return;
        }
        else
        {
            // Async path
            if (result is IResult)
            {
                if (result is not IDAsyncHttpResult httpResult)
                    throw new InvalidOperationException("Unsupported result type returned from a d-async endpoint. Use DAsyncResults to return from a d-async method.");

                await httpResult.ExecuteAsync(this, cancellationToken);
                return;
            }

            await ExecuteSucceedAsync(result, cancellationToken);
        }
    }

    protected override Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return workQueue.DelayAsync(id, delay, cancellationToken);
    }

    protected override Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return workQueue.YieldAsync(id, cancellationToken);
    }

    public void SetCallback(string operationId, IDAsyncCallback? callback)
    {
        if (IsSyncContext)
            return;

        _operationId = operationId;
        _callback = callback;
    }

    private async Task ExecuteSucceedAsync(CancellationToken cancellationToken = default)
    {
        await redis.StringSetAsync(_operationId, """
        {
          "type": "void",
          "status": "complete"
        }
        """);

        if (_callback is not null)
        {
            await _callback.SucceedAsync(cancellationToken);
        }
    }

    private async Task ExecuteSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        await redis.StringSetAsync(_operationId, $$"""
        {
          "type": "object",
          "status": "complete",
          "value": {{JsonSerializer.Serialize(result)}}
        }
        """);

        if (_callback is not null)
        {
            await _callback.SucceedAsync(result, cancellationToken);
        }
    }

    Task IDAsyncContext.SucceedAsync(CancellationToken cancellationToken) => ExecuteSucceedAsync(cancellationToken);

    Task IDAsyncContext.SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => ExecuteSucceedAsync(result, cancellationToken);
}
