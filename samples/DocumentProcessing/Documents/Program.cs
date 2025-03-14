using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DocumentProcessor>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin()));
var app = builder.Build();
app.UseCors();
app.UseWebSockets();

// In-memory storage for document status
var documentStatus = new ConcurrentDictionary<string, bool>();
var webSocketClients = new ConcurrentDictionary<string, WebSocket>();

// Blob Storage Configuration (Using Azurite local storage)
const string storageConnectionString = "UseDevelopmentStorage=true";
const string containerName = "documents";
var blobServiceClient = new BlobServiceClient(storageConnectionString);
var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
await blobContainerClient.CreateIfNotExistsAsync();

app.MapPost("/upload-request", async (HttpContext context) =>
{
    var documentId = Guid.NewGuid().ToString();
    var blobName = $"{documentId}.pdf";
    var blobClient = blobContainerClient.GetBlobClient(blobName);

    // Generate SAS token for upload
    var sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = containerName,
        BlobName = blobName,
        Resource = "b",
        ExpiresOn = DateTime.UtcNow.AddMinutes(10)
    };
    sasBuilder.SetPermissions(BlobSasPermissions.Write);

    var sasToken = blobClient.GenerateSasUri(sasBuilder);

    documentStatus[documentId] = false;

    await context.Response.WriteAsJsonAsync(new
    {
        documentId,
        uploadUrl = sasToken.ToString()
    });
});

app.MapPost("/process-document/{id}", async (HttpContext context, string id) =>
{
    if (!documentStatus.ContainsKey(id))
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Document not found");
        return;
    }

    documentStatus[id] = true; // Mark document as processing

    // Simulate processing
    var docProcessor = context.RequestServices.GetRequiredService<DocumentProcessor>();
    _ = docProcessor.ProcessDocumentAsync(id, webSocketClients); // Fire and forget

    context.Response.StatusCode = 202;
    await context.Response.WriteAsync("Processing started");
});

// WebSocket endpoint for notifications
app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[1024 * 4];

    while (ws.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var clientMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (clientMessage.StartsWith("subscribe:"))
            {
                var documentId = clientMessage.Replace("subscribe:", "").Trim();
                webSocketClients[documentId] = ws;
                Console.WriteLine($"Client subscribed to {documentId}");
            }
        }
    }
});

app.Run();

class DocumentProcessor
{
    public async Task ProcessDocumentAsync(string documentId, ConcurrentDictionary<string, WebSocket> clients)
    {
        Console.WriteLine($"Processing document {documentId}...");
        await Task.Delay(5000); // Simulate document processing (5 seconds)

        if (clients.TryGetValue(documentId, out var ws) && ws.State == WebSocketState.Open)
        {
            var message = Encoding.UTF8.GetBytes($"Processing complete for document {documentId}");
            await ws.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        Console.WriteLine($"Document {documentId} processed.");
    }
}
