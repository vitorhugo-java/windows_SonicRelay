namespace SonicRelay.Windows.Signaling;

public enum SignalingConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Closing,
    Closed,
    Faulted
}
