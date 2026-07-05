using System.Text.Json;

namespace SonicRelay.Windows.Signaling.Tests;

public sealed class SignalingMessageEnvelopeTests
{
    public static TheoryData<string> SupportedTypes => new()
    {
        SignalingMessageTypes.PublisherReady,
        SignalingMessageTypes.ViewerReady,
        SignalingMessageTypes.WebRtcOffer,
        SignalingMessageTypes.WebRtcAnswer,
        SignalingMessageTypes.WebRtcIceCandidate,
        SignalingMessageTypes.SessionJoined,
        SignalingMessageTypes.SessionLeft,
        SignalingMessageTypes.SessionEnded,
        SignalingMessageTypes.Ping,
        SignalingMessageTypes.Pong,
        SignalingMessageTypes.Error
    };

    [Theory]
    [MemberData(nameof(SupportedTypes))]
    public void SerializeAndDeserializeRoundTripsSupportedTypes(string type)
    {
        using var payload = JsonDocument.Parse("{\"candidate\":\"value\"}");
        var envelope = new SignalingMessageEnvelope(type, "session-1", "viewer-2", payload.RootElement.Clone());

        var json = envelope.Serialize();
        var parsed = SignalingMessageEnvelope.Deserialize(json);

        Assert.Equal(type, parsed.Type);
        Assert.Equal("session-1", parsed.SessionId);
        Assert.Equal("viewer-2", parsed.ViewerId);
        Assert.Equal("value", parsed.Payload?.GetProperty("candidate").GetString());
        Assert.Contains("\"sessionId\":\"session-1\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"type\":\"unknown\"}")]
    [InlineData("{\"type\":42}")]
    public void DeserializeRejectsInvalidMessages(string json)
    {
        Assert.Throws<SignalingProtocolException>(() => SignalingMessageEnvelope.Deserialize(json));
    }

    [Theory]
    [InlineData(SignalingMessageTypes.WebRtcOffer)]
    [InlineData(SignalingMessageTypes.WebRtcAnswer)]
    [InlineData(SignalingMessageTypes.WebRtcIceCandidate)]
    public void ToSafeDiagnosticStringRedactsSensitivePayloads(string type)
    {
        using var payload = JsonDocument.Parse("{\"sdp\":\"secret-sdp\",\"candidate\":\"secret-ice\"}");
        var envelope = new SignalingMessageEnvelope(type, "session-1", "viewer-1", payload.RootElement.Clone());

        var diagnostic = envelope.ToSafeDiagnosticString();

        Assert.Contains("[REDACTED]", diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-sdp", diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-ice", diagnostic, StringComparison.Ordinal);
        Assert.Contains("viewer-1", diagnostic, StringComparison.Ordinal);
    }
}
