using System.Text.Json;
using System.Text.Json.Serialization;

namespace SonicRelay.Windows.Signaling;

public sealed record SignalingMessageEnvelope(
    string Type,
    string? SessionId = null,
    string? ViewerId = null,
    JsonElement? Payload = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Serialize()
    {
        Validate(this);
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static SignalingMessageEnvelope Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new SignalingProtocolException("The signaling message is empty.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<SignalingMessageEnvelope>(json, JsonOptions)
                ?? throw new SignalingProtocolException("The signaling message is empty.");
            Validate(envelope);
            return envelope;
        }
        catch (JsonException exception)
        {
            throw new SignalingProtocolException("The signaling message is not a valid envelope.", exception);
        }
    }

    public string ToSafeDiagnosticString()
    {
        if (!SignalingMessageTypes.HasSensitivePayload(Type))
        {
            return Serialize();
        }

        return JsonSerializer.Serialize(new
        {
            type = Type,
            sessionId = SessionId,
            viewerId = ViewerId,
            payload = "[REDACTED]"
        }, JsonOptions);
    }

    private static void Validate(SignalingMessageEnvelope envelope)
    {
        if (!SignalingMessageTypes.IsSupported(envelope.Type))
        {
            throw new SignalingProtocolException($"Unsupported signaling message type '{envelope.Type}'.");
        }
    }
}

public sealed class SignalingProtocolException(string message, Exception? innerException = null)
    : Exception(message, innerException);
