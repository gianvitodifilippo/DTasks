using System.Net;
using System.Text.Json;
using Approvals;
using Approvals.DTasks;
using DTasks;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Infrastructure.Execution;
using DTasks.Serialization.Configuration;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .UseAspNetCore(aspNetCore => aspNetCore
        .ConfigureSerialization(serialization => serialization
            .UseStackExchangeRedis()))
    .ConfigureServices(services => services
        .RegisterDAsyncService(typeof(AsyncEndpoints)))
    .ConfigureMarshaling(marshaling => marshaling
        .RegisterDAsyncType(typeof(AsyncEndpoints))
        .RegisterSurrogatableType(typeof(AsyncEndpoints)))
    .ConfigureExecution(execution => execution
        .UseSuspensionHandler(InfrastructureServiceProvider.GetRequiredService<IDAsyncSuspensionHandler>())));

builder.Services
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

#region In library

builder.Services
    .AddScoped<AsyncEndpoints>()
    .AddSingleton(ConnectionMultiplexer.Connect("localhost:6379").GetDatabase())
    .AddHttpClient()
    .AddSingleton<RedisDAsyncSuspensionHandler>()
    .AddHostedService(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<IDAsyncSuspensionHandler>(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>());

#endregion

var app = builder.Build();

#region Generated

app.MapPost("/approvals", async (
    HttpContext httpContext,
    [FromServices] AsyncEndpoints endpoints,
    [FromBody] NewApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateAsyncEndpointHost(httpContext);
    DTask<IResult> task = endpoints.NewApproval(request);

    await host.StartAsync(task, cancellationToken);
});

app.MapGet("/async/{operationId}", async (
    [FromServices] IDatabase redis,
    string operationId) =>
{
    operationId = WebUtility.UrlDecode(operationId);
    string? value = await redis.StringGetAsync($"{operationId}:info");
    if (value is null)
        return Results.NotFound();
    
    JsonElement obj = JsonSerializer.Deserialize<JsonElement>(value).GetProperty("Instance");
    string status = obj.GetProperty("Status").GetString()!;

    if (status is "succeeded")
        return Results.Ok(new
        {
            status,
            result = obj.GetProperty("Result")
        });

    return Results.Ok(new { status });
}).WithName("DTasksStatus");

app.MapGet("/approvals/{id}/{result}", async (
    IServiceProvider services,
    string id,
    ApprovalResult result,
    CancellationToken cancellationToken) =>
{
    id = WebUtility.UrlDecode(id);
    if (!DAsyncId.TryParse(id, out DAsyncId dAsyncId))
        return Results.NotFound();

    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(services);
    await host.ResumeAsync(dAsyncId, result, cancellationToken);
    return Results.Ok();
});

#endregion

app.Run();
