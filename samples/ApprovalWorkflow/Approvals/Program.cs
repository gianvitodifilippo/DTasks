using Approvals;
using Approvals.Generated;
using DTasks;
using DTasks.AspNetCore;
using DTasks.Hosting;
using DTasks.Marshaling;
using DTasks.Serialization.Json;
using DTasks.Serialization.StackExchangeRedis;
using DTasks.Serialization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(configuration => configuration
    .ConfigureDTasks(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            typeResolver.RegisterDAsyncType<DAsyncRunner>();
        })));
builder.Services.AddScoped<AsyncEndpoints>();

builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddSingleton(sp => new Func<IDAsyncMarshaler, IDAsyncSerializer>(marshaler => JsonDAsyncSerializer.Create(sp.GetRequiredService<ITypeResolver>(), marshaler, sp.GetRequiredService<JsonSerializerOptions>())))
    .AddSingleton<IDAsyncStorage, RedisDAsyncStorage>()
    .AddSingleton(new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
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

builder.Services
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

var app = builder.Build();

app.MapPost("/approvals/start", async (
    [FromServices] DAsyncRunner runner,
    [FromServices] AspNetCoreDAsyncHost host,
    [FromServices] IDatabase redis,
    [FromHeader(Name = "Async-CallbackType")] string callbackType,
    [FromHeader(Name = "Async-CallbackUrl")] string? callbackAddress,
    [FromBody] StartApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    string operationId = Guid.NewGuid().ToString();
    DTask<IResult> task;

    if (callbackType is "webhook")
    {
        if (callbackAddress is null)
            return Results.BadRequest();

        task = runner.StartApproval_Webhook(operationId, new Uri(callbackAddress), request);
    }
    else
    {
        task = runner.StartApproval(operationId, request);
    }

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

// This should be generated
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

app.Run();
