using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Documents;
using DTasks;
using DTasks.AspNetCore;
using DTasks.Serialization;
using DTasks.Serialization.Json;
using DTasks.Serialization.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.AspNetCore.Infrastructure;
using DTasks.Extensions.Hosting;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Infrastructure.State;
using DTasks.Serialization.Json.Converters;

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
    .AddSingleton<RedisDAsyncSuspensionHandler>()
    .AddHostedService(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<IDAsyncSuspensionHandler>(sp => sp.GetRequiredService<RedisDAsyncSuspensionHandler>())
    .AddSingleton<WebSocketHandler>()
    .AddSingleton<IWebSocketHandler>(sp => sp.GetRequiredService<WebSocketHandler>())
    .AddScoped<IDAsyncStateManager, BinaryDAsyncStateManager>()
    .AddSingleton<IDAsyncContinuationFactory, DAsyncContinuationFactory>(); ;
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

#region Generated
app.MapPost("/process-document/{documentId}", (
    HttpContext httpContext,
    [FromServices] AsyncEndpoints endpoints,
    string documentId,
    CancellationToken cancellationToken) =>
{
    AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.CreateHttpHost(httpContext, "GetDocumentStatus");
    DTask<IResult> task = endpoints.ProcessDocument(documentId);

    return host.StartAsync(task, cancellationToken);
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

    if (status is "succeeded")
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
#endregion

app.Run();
