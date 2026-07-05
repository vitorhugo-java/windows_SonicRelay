namespace SonicRelay.Windows.Signaling;

public static class SignalingMessageTypes
{
    public const string PublisherReady = "publisher.ready";
    public const string ViewerReady = "viewer.ready";
    public const string WebRtcOffer = "webrtc.offer";
    public const string WebRtcAnswer = "webrtc.answer";
    public const string WebRtcIceCandidate = "webrtc.ice_candidate";
    public const string SessionJoined = "session.joined";
    public const string SessionLeft = "session.left";
    public const string SessionEnded = "session.ended";
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Error = "error";

    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        PublisherReady,
        ViewerReady,
        WebRtcOffer,
        WebRtcAnswer,
        WebRtcIceCandidate,
        SessionJoined,
        SessionLeft,
        SessionEnded,
        Ping,
        Pong,
        Error
    };

    public static bool IsSupported(string? type) => type is not null && Supported.Contains(type);

    public static bool HasSensitivePayload(string? type) =>
        type is WebRtcOffer or WebRtcAnswer or WebRtcIceCandidate;
}
