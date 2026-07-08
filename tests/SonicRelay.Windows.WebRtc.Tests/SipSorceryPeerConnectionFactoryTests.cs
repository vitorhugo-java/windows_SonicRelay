using SIPSorcery.Net;
using SonicRelay.Windows.WebRtc;

namespace SonicRelay.Windows.WebRtc.Tests;

public sealed class SipSorceryPeerConnectionFactoryTests
{
    private static readonly WebRtcIceServer[] Servers =
    [
        new(["turn:relay.example:3478"], "user", "secret"),
    ];

    [Fact]
    public void BuildConfigurationAllowsDirectIceByDefault()
    {
        var config = SipSorceryPeerConnectionFactory.BuildConfiguration(Servers, forceRelay: false);

        Assert.Equal(RTCIceTransportPolicy.all, config.iceTransportPolicy);
        Assert.Equal("turn:relay.example:3478", Assert.Single(config.iceServers).urls);
    }

    [Fact]
    public void BuildConfigurationForcesRelayOnlyIce()
    {
        var config = SipSorceryPeerConnectionFactory.BuildConfiguration(Servers, forceRelay: true);

        Assert.Equal(RTCIceTransportPolicy.relay, config.iceTransportPolicy);
        // TURN credentials are still carried so relay can actually connect.
        Assert.Equal("secret", Assert.Single(config.iceServers).credential);
    }
}
