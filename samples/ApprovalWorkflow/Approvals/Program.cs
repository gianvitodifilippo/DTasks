using Approvals;
using DTasks;
using DTasks.AspNetCore;
using DTasks.AspNetCore.Infrastructure;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Extensions.Hosting;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Serialization;
using DTasks.Serialization.Json;
using DTasks.Serialization.Json.Converters;
using DTasks.Serialization.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .Configure(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            AspNetCoreDAsyncHost.RegisterTypeIds(typeResolver);
        })));

#region In library

JsonMarshalingConfiguration marshalingConfiguration = JsonMarshalingConfiguration.Create();


builder.Services.AddScoped<AsyncEndpoints>();
builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddSingleton(sp =>
    {
        IDAsyncTypeResolver typeResolver = sp.GetRequiredService<IDAsyncTypeResolver>();
        
        marshalingConfiguration.ConfigureSerializerOptions(options =>
        {
            options.Converters.Add(new TypedInstanceJsonConverter<object>(typeResolver));
            options.Converters.Add(new TypedInstanceJsonConverter<IDAsyncContinuationSurrogate>(typeResolver));
        });
        marshalingConfiguration.RegisterSurrogatableType(typeof(AsyncEndpoints));

        return marshalingConfiguration.CreateSerializerFactory(typeResolver);
    })
    .AddScoped<DAsyncFlowServices>()
    .AddScoped<IDAsyncFlowServices>(sp => sp.GetRequiredService<DAsyncFlowServices>())
    .AddScoped(sp => sp.GetRequiredService<JsonDAsyncSerializerFactory>().CreateSerializer(sp.GetRequiredService<IDAsyncFlowServices>().Surrogator))
    .AddSingleton<IDAsyncStorage, RedisDAsyncStorage>()
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
