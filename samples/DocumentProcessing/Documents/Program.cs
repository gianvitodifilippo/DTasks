using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Documents;
using Documents.Generated;
using DTasks;
using DTasks.AspNetCore;
using DTasks.Marshaling;
using DTasks.Serialization;
using DTasks.Serialization.Json;
using DTasks.Serialization.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDTasks(configuration => configuration
    .ConfigureDTasks(configuration => configuration
        .ConfigureTypeResolver(typeResolver =>
        {
            typeResolver.RegisterDAsyncType<AsyncEndpoints>();
            typeResolver.RegisterDAsyncType<DAsyncRunner>();
        })));
builder.Services.AddScoped<AsyncEndpoints>();

builder.Services
    .AddSingleton(sp => ConnectionMultiplexer.Connect("localhost:6379"))
    .AddSingleton(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services
    .AddSingleton(sp => new Func<IDAsyncMarshaler, IDAsyncSerializer>(marshaler => JsonDAsyncSerializer.Create(sp.GetRequiredService<ITypeResolver>(), marshaler, sp.GetRequiredService<JsonSerializerOptions>())))
    .AddSingleton<IDAsyncStorage, RedisDAsyncStorage>()
    .AddSingleton(new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new TypeIdJsonConverter(),
            new DAsyncIdJsonConverter()
        }
    })
    .AddScoped<AspNetCoreDAsyncHost>()
    .AddSingleton<RedisWorkQueue>()
    .AddHostedService(sp => sp.GetRequiredService<RedisWorkQueue>())
    .AddSingleton<IWorkQueue>(sp => sp.GetRequiredService<RedisWorkQueue>())
    .AddScoped<DAsyncRunner>();

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
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<IWebSocketHandler>(sp => sp.GetRequiredService<WebSocketHandler>());
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

app.MapPost("/upload-request", (HttpContext context, BlobContainerClient containerClient) =>
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

app.MapPost("/process-document/{documentId}", async (
    [FromServices] DAsyncRunner runner,
    [FromServices] AspNetCoreDAsyncHost host,
    [FromServices] IDatabase redis,
    [FromHeader(Name = "Async-CallbackType")] string callbackType,
    [FromHeader(Name = "Async-ConnectionId")] string? connectionId,
    string documentId,
    CancellationToken cancellationToken) =>
{
    string operationId = Guid.NewGuid().ToString();
    DTask<IResult> task;

    if (callbackType is "websockets")
    {
        if (connectionId is null)
            return Results.BadRequest();

        task = runner.ProcessDocument_Websockets(operationId, connectionId, documentId);
    }
    else
    {
        task = runner.ProcessDocument(operationId, documentId);
    }

    host.IsSyncContext = true;
    await host.StartAsync(task, cancellationToken);

    if (host.Result is IResult result)
        return result;

    await redis.StringSetAsync(operationId, $$"""
    {
      "type": "void",
      "status": "pending"
    }
    """);

    object value = new { operationId };
    return Results.AcceptedAtRoute("GetDocumentStatus", value, value);
});

app.MapGet("/process-document/{operationId}", async (
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
}).WithName("GetDocumentStatus");

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

app.Run();
