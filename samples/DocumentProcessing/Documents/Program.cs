using System.Net.WebSockets;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DTasks;
using DTasks.AspNetCore;
using DTasks.AspNetCore.Configuration;
using DTasks.AspNetCore.Http;
using DTasks.Configuration;
using DTasks.Serialization.Configuration;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(dTasks => dTasks
    .AutoConfigure()
    .UseAspNetCore(aspNetCore => aspNetCore
        .AutoConfigure()
        .ConfigureSerialization(serialization => serialization
            .UseStackExchangeRedis())));

builder.Services
    .AddSingleton(ConnectionMultiplexer.Connect("localhost:6379"));

#region TODO: In library

builder.Services
    .AddSingleton<WebSocketHandler>()
    .AddSingleton<IWebSocketHandler>(sp => sp.GetRequiredService<WebSocketHandler>());

#endregion

const string storageConnectionString = "UseDevelopmentStorage=true";
const string containerName = "documents";
BlobServiceClient serviceClient = new(storageConnectionString);
var getPropertiesResponse = await serviceClient.GetPropertiesAsync();
BlobServiceProperties properties = getPropertiesResponse.Value;
properties.Cors = [
    new BlobCorsRule
    {
        AllowedOrigins = "*",
        AllowedMethods = "put",
        AllowedHeaders = "*",
        ExposedHeaders = "*",
        MaxAgeInSeconds = 60
    }
];
await serviceClient.SetPropertiesAsync(properties);
BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);
await containerClient.CreateIfNotExistsAsync();

builder.Services.AddSingleton(containerClient);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

app.MapPost("/upload-request", (BlobContainerClient containerClient) =>
{
    string documentId = Guid.NewGuid().ToString();
    string blobName = $"{documentId}.pdf";
    BlobClient blobClient = containerClient.GetBlobClient(blobName);

    BlobSasBuilder sasBuilder = new()
    {
        BlobContainerName = containerName,
        BlobName = blobName,
        Resource = "b",
        ExpiresOn = DateTime.UtcNow.AddMinutes(10)
    };
    sasBuilder.SetPermissions(BlobSasPermissions.Write);

    Uri uploadUrl = blobClient.GenerateSasUri(sasBuilder);

    return Results.Ok(new
    {
        documentId,
        uploadUrl
    });
});

app.MapAsyncPost("/process-document/{documentId}", async (
    [FromServices] BlobContainerClient containerClient,
    string documentId) =>
{
    bool exists = await DocumentExistsAsync(containerClient, documentId);
    if (!exists)
        return Results.NotFound();

    await DTask.Yield();

    // Simulating the processing with a delay
    await Task.Delay(TimeSpan.FromSeconds(15));

    return AsyncResults.Success();

    static async Task<bool> DocumentExistsAsync(BlobContainerClient containerClient, string documentId)
    {
        string blobName = $"{documentId}.pdf";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }
});

#region TODO: In library

// If the client already maps its own WebSocket endpoint, then it needs to inject WebSocketHandler manually (some simplification is needed).
// Otherwise, we will generate the whole method.
app.Map("/ws", async (HttpContext context, [FromServices] WebSocketHandler handler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[1024 * 4];

    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var clientMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (clientMessage.StartsWith("connect:"))
            {
                var connectionId = clientMessage.Replace("connect:", "").Trim();
                handler.AddConnection(connectionId, webSocket);
            }
        }
    }
});

#endregion

app.MapDTasks();

app.Run();
