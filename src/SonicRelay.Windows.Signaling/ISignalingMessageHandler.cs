namespace SonicRelay.Windows.Signaling;

public interface ISignalingMessageHandler
{
    Task HandleAsync(SignalingMessageEnvelope message, CancellationToken cancellationToken = default);
}
