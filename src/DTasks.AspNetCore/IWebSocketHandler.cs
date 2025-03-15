namespace DTasks.AspNetCore;

public interface IWebSocketHandler
{
    Task SendAsync<TMessage>(string connectionId, TMessage message, CancellationToken cancellationToken = default);
}
