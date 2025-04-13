using Approvals;
using DTasks;
using DTasks.Serialization;
using DTasks.Serialization.Json;
using DTasks.Serialization.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.AspNetCore;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Extensions.Hosting;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.AspNetCore.Infrastructure.Http;
using System.Net;
using DTasks.Serialization.Json.Converters;

JsonSerializerOptions options = new()
{
    Converters = { new MyConverterFactory() }
};

MyClass c = new();
string json = JsonSerializer.Serialize(c, options);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .Configure(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            AspNetCoreDAsyncHost.RegisterTypeIds(typeResolver);
        })));

#region In library
builder.Services.AddScoped<AsyncEndpoints>();
builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddScoped<IDAsyncSerializer>(sp => JsonDAsyncSerializer.Create(sp.GetRequiredService<IDAsyncTypeResolver>(), sp.GetRequiredService<JsonSerializerOptions>()))
    .AddSingleton<IDAsyncStorage, RedisDAsyncStorage>()
    .AddSingleton(sp => new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new TypeIdJsonConverter(),
            new DAsyncIdJsonConverter(),
            new TypedInstanceJsonConverter<object>(sp.GetRequiredService<IDAsyncTypeResolver>()),
            new TypedInstanceJsonConverter<IDAsyncContinuationMemento>(sp.GetRequiredService<IDAsyncTypeResolver>()),
        }
    })
    .AddHttpClient()
    .AddSingleton<RedisDAsyncSuspensionHandler>()
    .AddHostedService(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<IDAsyncSuspensionHandler>(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddScoped<IDAsyncStateManager, BinaryDAsyncStateManager>()
    .AddSingleton<IDAsyncContinuationFactory, DAsyncContinuationFactory>();
#endregion

builder.Services
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

var app = builder.Build();

#region Generated
app.MapPost("/approvals", async (
    HttpContext httpContext,
    [FromServices] AsyncEndpoints endpoints,
    [FromBody] NewApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateHttpHost(httpContext, "GetApprovalsStatus");
    DTask<IResult> task = endpoints.NewApproval(request);

    await host.StartAsync(task, cancellationToken);
});

app.MapGet("/approvals/{operationId}", async (
    [FromServices] IDatabase redis,
    string operationId) =>
{
    operationId = WebUtility.UrlDecode(operationId);
    string? value = await redis.StringGetAsync(operationId);
    if (value is null)
        return Results.NotFound();
    
    JsonElement obj = JsonSerializer.Deserialize<JsonElement>(value);
    string status = obj.GetProperty("status").GetString()!;

    if (status is "succeeded")
        return Results.Ok(new
        {
            status,
            value = obj.GetProperty("value")
        });

    return Results.Ok(new { status });
}).WithName("GetApprovalsStatus");

app.MapGet("/approvals/{id}/{result}", async (
    HttpContext httpContext,
    string id,
    ApprovalResult result,
    CancellationToken cancellationToken) =>
{
    if (!DAsyncId.TryParse(id, out DAsyncId dAsyncId))
        return Results.NotFound();

    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateHttpHost(httpContext, "GetApprovalsStatus");
    await host.ResumeAsync(dAsyncId, result, cancellationToken);
    return Results.Ok();
});
#endregion

app.Run();
