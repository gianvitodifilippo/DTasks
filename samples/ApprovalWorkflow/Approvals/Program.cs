using System.Net;
using System.Text.Json;
using Approvals;
using DTasks;
using Microsoft.Extensions.Hosting;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Serialization.Json.Converters;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .Configure(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            AspNetCoreDAsyncHost.RegisterTypeIds(typeResolver);
        })));

#region In library

IDatabase database = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();

builder.Services.AddScoped<AsyncEndpoints>();
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
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateAsyncEndpointHost(httpContext);
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
