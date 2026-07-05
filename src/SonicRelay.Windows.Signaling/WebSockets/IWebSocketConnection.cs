using System.Net.WebSockets;

namespace SonicRelay.Windows.Signaling.WebSockets;

internal sealed record WebSocketInboundMessage(
    WebSocketMessageType MessageType,
    string? Text,
    WebSocketCloseStatus? CloseStatus);

internal interface IWebSocketConnection : IAsyncDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, string accessToken, CancellationToken cancellationToken);
    Task SendTextAsync(string message, CancellationToken cancellationToken);
    Task<WebSocketInboundMessage> ReceiveAsync(CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken);
}

internal interface IWebSocketConnectionFactory
{
    IWebSocketConnection Create();
}
