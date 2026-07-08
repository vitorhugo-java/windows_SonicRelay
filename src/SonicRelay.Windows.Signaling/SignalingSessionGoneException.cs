using System.Net;

namespace SonicRelay.Windows.Signaling;

/// <summary>
/// Thrown when the signaling server reports that the session no longer exists
/// (HTTP 410 Gone / 404 Not Found on the WebSocket upgrade). This is terminal:
/// reconnecting to the same session is futile and, worse, keeps the client
/// "active" so a new session cannot be started. The client stops retrying and
/// releases the session instead of looping on the dead one forever.
/// </summary>
public sealed class SignalingSessionGoneException : Exception
{
    public SignalingSessionGoneException(HttpStatusCode statusCode)
        : base($"The signaling session is gone (HTTP {(int)statusCode} {statusCode}).") =>
        StatusCode = statusCode;

    public HttpStatusCode StatusCode { get; }
}
