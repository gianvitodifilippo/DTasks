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

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .ConfigureDTasks(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
        })));

#region In library
builder.Services.AddScoped<AsyncEndpoints>();
builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddSingleton<IDAsyncSerializer>(sp => JsonDAsyncSerializer.Create(sp.GetRequiredService<IDAsyncTypeResolver>(), sp.GetRequiredService<JsonSerializerOptions>()))
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
    .AddSingleton<RedisDAsyncSuspensionHandler>()
    .AddHostedService(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<IDAsyncSuspensionHandler>(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<IDAsyncStateManager, BinaryDAsyncStateManager>();
#endregion

builder.Services
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

var app = builder.Build();

#region Generated
app.MapPost("/approvals", (
    HttpContext httpContext,
    [FromServices] AsyncEndpoints endpoints,
    [FromBody] NewApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateHttpHost(httpContext, "GetApprovalsStatus");
    DTask<IResult> task = endpoints.NewApproval(request);

    return host.StartAsync(task, cancellationToken);
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
