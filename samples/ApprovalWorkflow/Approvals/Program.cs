using Approvals;
using DTasks;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration;
using DTasks.Serialization.Configuration;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .AutoConfigure()
    .UseAspNetCore(aspNetCore => aspNetCore
        .AddResumptionEndpoint(ApprovalService.ResumptionEndpoint)
        .ConfigureSerialization(serialization => serialization
            .UseStackExchangeRedis()))
#region TODO: In library
    .ConfigureServices(services => services
        .RegisterDAsyncService<AsyncEndpoints>())
    .ConfigureMarshaling(marshaling => marshaling
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

app.MapAsyncPost("/approvals", async DTask<IResult> (
    [FromServices] ApproverRepository repository,
    [FromServices] ApprovalService service,
    [FromBody] NewApprovalRequest request) =>
{
    string? email = await repository.GetEmailByIdAsync(request.ApproverId);
    if (email is null)
        return Results.BadRequest("Invalid approver id");

    DTask<ApprovalResult> approvalTask = service.SendApprovalRequestDAsync(request.Details, email);
    DTask timeout = DTask.Delay(TimeSpan.FromDays(7));

    DTask winner = await DTask.WhenAny(timeout, approvalTask);
    ApprovalResult result = winner == timeout
        ? ApprovalResult.Reject
        : approvalTask.Result;

    return AsyncResults.Success(result);
});

#endregion

app.Run();
