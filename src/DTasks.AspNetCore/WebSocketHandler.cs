using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DTasks.AspNetCore;

public class WebSocketHandler : IWebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _webSockets = [];

    public void AddConnection(string connectionId, WebSocket webSocket)
    {
        _webSockets[connectionId] = webSocket;
    }

    public void RemoveConnection(string connectionId)
    {
        _webSockets.Remove(connectionId, out _);
    }

    public async Task SendAsync<TMessage>(string connectionId, TMessage message, CancellationToken cancellationToken = default)
    {
        WebSocket webSocket = _webSockets[connectionId];

        byte[] utf8Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await webSocket.SendAsync(new ArraySegment<byte>(utf8Message), WebSocketMessageType.Text, true, cancellationToken);
    }
}
