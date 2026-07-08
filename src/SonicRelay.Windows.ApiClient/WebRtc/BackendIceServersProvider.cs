using SonicRelay.Windows.ApiClient.Errors;
using SonicRelay.Windows.WebRtc;

namespace SonicRelay.Windows.ApiClient.WebRtc;

/// <summary>
/// Supplies ICE servers to the WebRTC layer from the backend
/// <c>/api/webrtc/ice-servers</c> endpoint, caching them until shortly before
/// the returned TURN credentials expire. Never throws: on failure it returns
/// the last good result. With no cache to fall back to, it returns the
/// public-STUN <see cref="StunFallback"/> only when
/// <paramref name="allowGoogleStunDevFallback"/> is true (development
/// builds); otherwise it returns an empty list rather than silently
/// depending on Google's public STUN server in production. An empty (but
/// successful) backend response is returned as-is — it is not replaced with
/// the dev fallback, since that is a valid, authoritative answer (e.g. TURN
/// not configured and the server-side Google STUN fallback disabled).
/// </summary>
public sealed class BackendIceServersProvider(
    IWebRtcApiClient apiClient,
    TimeProvider? timeProvider = null,
    bool allowGoogleStunDevFallback = false) : IIceServersProvider
{
    private static readonly IReadOnlyList<WebRtcIceServer> StunFallback =
        [new WebRtcIceServer(["stun:stun1.google.com:19302"])];

    private readonly IWebRtcApiClient apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<WebRtcIceServer>? cached;
    private DateTimeOffset cacheExpiresAt = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<WebRtcIceServer>> GetIceServersAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (cached is not null && now < cacheExpiresAt) return cached;

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = timeProvider.GetUtcNow();
            if (cached is not null && now < cacheExpiresAt) return cached;

            var response = await apiClient.GetIceServersAsync(cancellationToken).ConfigureAwait(false);
            cached = response.IceServers
                .Where(server => server.Urls is { Count: > 0 })
                .Select(server => new WebRtcIceServer(server.Urls, server.Username, server.Credential))
                .ToArray();
            // Refresh a minute before the credentials lapse so a renegotiation
            // never starts with a stale TURN username.
            var ttl = Math.Max((response.ExpiresAt - now).TotalSeconds - 60, 30);
            cacheExpiresAt = now.AddSeconds(ttl);
            return cached;
        }
        catch (Exception exception) when (exception is not OperationCanceledException and (ApiClientException or HttpRequestException))
        {
            return cached ?? (allowGoogleStunDevFallback ? StunFallback : []);
        }
        finally
        {
            gate.Release();
        }
    }
}
