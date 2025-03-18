using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/approvals/start", async () =>
{
    using HttpClient http = new HttpClient();

    using HttpRequestMessage request = new(HttpMethod.Post, new Uri("http://localhost:5033/approvals"));
    request.Headers.Add("Async-CallbackType", "webhook");
    request.Headers.Add("Async-CallbackUrl", "http://localhost:5017/approvals/result");

    request.Content = JsonContent.Create(new
    {
        details = new
        {
            requestor = "Gianvito",
            approvalCategory = "Admin",
            resourceUri = "https://www.google.com"
        },
        approverId = "550e8400-e29b-41d4-a716-446655440000"
    });

    using HttpResponseMessage response = await http.SendAsync(request);
    return Results.Ok($"Go to {response.Headers.Location} to monitor the status of the approval");
});

app.MapPost("/approvals/result", ([FromBody] ApprovalResult result) =>
{
    return Results.Ok();
});

app.Run();

class ApprovalResult
{
    public string OperationId { get; set; } = default!;

    public int Result { get; set; } = default!;
}