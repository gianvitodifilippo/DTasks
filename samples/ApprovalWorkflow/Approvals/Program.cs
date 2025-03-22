using Approvals;
using Approvals.Generated;
using DTasks;
using DTasks.AspNetCore;
using DTasks.Hosting;
using DTasks.Marshaling;
using DTasks.Serialization;
using DTasks.Serialization.Json;
using DTasks.Serialization.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(configuration => configuration
    .ConfigureDTasks(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            typeResolver.RegisterDAsyncType<DAsyncRunner>();
        })));

#region In library
builder.Services.AddScoped<AsyncEndpoints>();
builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddSingleton(sp => new Func<IDAsyncMarshaler, IDAsyncSerializer>(marshaler => JsonDAsyncSerializer.Create(sp.GetRequiredService<ITypeResolver>(), marshaler, sp.GetRequiredService<JsonSerializerOptions>())))
    .AddSingleton<IDAsyncStorage, RedisDAsyncStorage>()
    .AddSingleton(new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new TypeIdJsonConverter(),
            new DAsyncIdJsonConverter()
        }
    })
    .AddHttpClient()
    .AddScoped<AspNetCoreDAsyncHost>()
    .AddSingleton<RedisWorkQueue>()
    .AddHostedService(sp => sp.GetRequiredService<RedisWorkQueue>())
    .AddSingleton<IWorkQueue>(sp => sp.GetRequiredService<RedisWorkQueue>())
    .AddScoped<DAsyncRunner>();
#endregion

builder.Services
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

var app = builder.Build();

#region Generated
app.MapPost("/approvals", async (
    [FromServices] DAsyncRunner runner,
    [FromServices] AspNetCoreDAsyncHost host,
    [FromServices] IDatabase redis,
    [FromHeader(Name = "Async-CallbackType")] string callbackType,
    [FromHeader(Name = "Async-CallbackUrl")] string? callbackAddress,
    [FromBody] NewApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    string operationId = Guid.NewGuid().ToString();
    DTask<IResult> task;

    if (callbackType is "webhook")
    {
        if (callbackAddress is null)
            return Results.BadRequest();

        task = runner.NewApproval_Webhook(operationId, new Uri(callbackAddress), request);
    }
    else
    {
        task = runner.NewApproval(operationId, request);
    }

    host.IsSyncContext = true;
    await host.StartAsync(task, cancellationToken);

    if (host.Result is IResult result)
        return result;

    await redis.StringSetAsync(operationId, $$"""
    {
      "type": "object",
      "status": "pending"
    }
    """);

    object value = new { operationId };
    return Results.AcceptedAtRoute("GetApprovalsStatus", value, value);
});

app.MapGet("/approvals/{operationId}", async (
    [FromServices] IDatabase redis,
    string operationId) =>
{
    string? value = await redis.StringGetAsync(operationId);
    if (value is null)
        return Results.NotFound();
    
    JsonElement obj = JsonSerializer.Deserialize<JsonElement>(value);
    string status = obj.GetProperty("status").GetString()!;

    if (status is "complete")
        return Results.Ok(new
        {
            status,
            value = obj.GetProperty("value")
        });

    return Results.Ok(new { status });
}).WithName("GetApprovalsStatus");

app.MapGet("/approvals/{id}/{result}", async (
    string id,
    ApprovalResult result,
    [FromServices] AspNetCoreDAsyncHost host,
    CancellationToken cancellationToken) =>
{
    if (!DAsyncId.TryParse(WebUtility.UrlDecode(id), out DAsyncId dasyncId))
        return Results.NotFound();

    await host.ResumeAsync(dasyncId, result, cancellationToken);
    return Results.Ok();
});
#endregion

app.Run();
