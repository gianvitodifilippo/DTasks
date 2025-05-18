using Approvals;
using DTasks;
using DTasks.AspNetCore.Infrastructure;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration;
using DTasks.Serialization.Configuration;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .UseAspNetCore(aspNetCore => aspNetCore
        .AddResumptionEndpoint(ApprovalService.ResumptionEndpoint)
        .ConfigureSerialization(serialization => serialization
            .UseStackExchangeRedis()))
#region TODO: In library
    .ConfigureServices(services => services
        .RegisterDAsyncService(typeof(AsyncEndpoints)))
    .ConfigureMarshaling(marshaling => marshaling
        .RegisterDAsyncType(typeof(AsyncEndpoints))
        .RegisterSurrogatableType(typeof(AsyncEndpoints))
        .RegisterTypeId(typeof(AsyncEndpointInfo<ApprovalResult>))));
#endregion

builder.Services.AddRazorPages();

builder.Services
    .AddSingleton(ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton<ApproverRepository>()
    .AddSingleton<ApprovalService>();

#region TODO: In library

builder.Services.AddScoped<AsyncEndpoints>();

#endregion

var app = builder.Build();

app.MapDTasks();
app.MapRazorPages();

#region TODO: In library

app.MapPost("/approvals", async (
    HttpContext httpContext,
    [FromServices] AsyncEndpoints endpoints,
    [FromServices] DTasksConfiguration configuration,
    [FromBody] NewApprovalRequest request,
    CancellationToken cancellationToken) =>
{
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateAsyncEndpointHost(httpContext);
    DTask<IResult> task = endpoints.NewApproval(request);

    await configuration.StartAsync(host, task, cancellationToken);
});

#endregion

app.Run();
