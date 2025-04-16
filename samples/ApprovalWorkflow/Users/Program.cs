using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/approvals/start", async ([FromServices] ILogger<Program> logger) =>
{
    using HttpClient http = new HttpClient();
    http.Timeout = Timeout.InfiniteTimeSpan;

    using HttpRequestMessage request = new(HttpMethod.Post, new Uri("http://localhost:5033/approvals"));
    request.Headers.Add("Async-CallbackType", "webhook");
    request.Headers.Add("Async-CallbackUrl", "http://localhost:5017/approvals/result");

    request.Content = JsonContent.Create(new
    {
        details = new
        {
            requestor = "Eric L. Atksy",
            approvalCategory = "Admin",
            resourceUri = "https://r.mtdv.me/iCZItCHl7v" // safe to click :)
        },
        approverId = "550e8400-e29b-41d4-a716-446655440000"
    });

    using HttpResponseMessage response = await http.SendAsync(request);
    StartApprovalResult? result = await response.Content.ReadFromJsonAsync<StartApprovalResult>();

    logger.LogInformation("Started a new approval with id {OperationId}", result!.OperationId);

    return Results.Ok($"Go to {response.Headers.Location} to monitor the status of the approval");
});

app.MapPost("/approvals/result", (
    [FromServices] ILogger<Program> logger,
    [FromBody] ApprovalResult result) =>
{
    logger.LogInformation("Approval {OperationId} finished with result {Result}", result.OperationId, result.Result == 0 ? "Rejected" : "Approved");
    return Results.Ok();
});

app.Run();

class StartApprovalResult
{
    public string OperationId { get; set; } = default!;
}

class ApprovalResult
{
    public string OperationId { get; set; } = default!;

    public int Result { get; set; } = default!;
}