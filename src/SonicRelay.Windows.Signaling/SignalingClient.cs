using System.Net.WebSockets;
using SonicRelay.Windows.Core.Configuration;
using SonicRelay.Windows.Core.Storage;
using SonicRelay.Windows.Signaling.WebSockets;

namespace SonicRelay.Windows.Signaling;

internal interface IReconnectDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class ReconnectDelay : IReconnectDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}

public sealed class SignalingClient : ISignalingClient
{
    private static readonly TimeSpan[] ReconnectDelays =
        [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    private readonly PublisherConfiguration configuration;
    private readonly ITokenStore tokenStore;
    private readonly IReadOnlyList<ISignalingMessageHandler> handlers;
    private readonly IWebSocketConnectionFactory connectionFactory;
    private readonly IReconnectDelay reconnectDelay;
    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private CancellationTokenSource? lifecycleCancellation;
    private IWebSocketConnection? connection;
    private Task? receiveTask;
    private string? activeSessionId;
    private string? activeDeviceId;

    public SignalingClient(
        PublisherConfiguration configuration,
        ITokenStore tokenStore,
        IEnumerable<ISignalingMessageHandler> handlers)
        : this(configuration, tokenStore, handlers, new ClientWebSocketConnectionFactory(), new ReconnectDelay())
    {
    }

    internal SignalingClient(
        PublisherConfiguration configuration,
        ITokenStore tokenStore,
        IEnumerable<ISignalingMessageHandler> handlers,
        IWebSocketConnectionFactory connectionFactory,
        IReconnectDelay reconnectDelay)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        this.handlers = handlers?.ToArray() ?? throw new ArgumentNullException(nameof(handlers));
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        this.reconnectDelay = reconnectDelay ?? throw new ArgumentNullException(nameof(reconnectDelay));
    }

    public SignalingConnectionState State { get; private set; } = SignalingConnectionState.Disconnected;
    public event Action<SignalingConnectionState>? StateChanged;

    public async Task ConnectAsync(string sessionId, string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (IsActive())
            {
                if (string.Equals(activeSessionId, sessionId, StringComparison.Ordinal)
                    && string.Equals(activeDeviceId, deviceId, StringComparison.Ordinal))
                {
                    return;
                }
                throw new InvalidOperationException("A signaling connection is already active for another session or device.");
            }

            activeSessionId = sessionId;
            activeDeviceId = deviceId;
            lifecycleCancellation?.Dispose();
            lifecycleCancellation = new CancellationTokenSource();
            SetState(SignalingConnectionState.Connecting);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifecycleCancellation.Token);
                await OpenConnectionAsync(linked.Token);
                receiveTask = RunReceiveLoopAsync(lifecycleCancellation.Token);
            }
            catch
            {
                SetState(SignalingConnectionState.Faulted);
                await DisposeConnectionAsync();
                throw;
            }
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    public async Task SendAsync(SignalingMessageEnvelope message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var current = connection;
        if (State != SignalingConnectionState.Connected || current is null)
        {
            throw new InvalidOperationException("The signaling connection is not connected.");
        }

        await SendCoreAsync(current, message, cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        Task? pendingReceive;
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (State is SignalingConnectionState.Disconnected or SignalingConnectionState.Closed)
            {
                return;
            }

            SetState(SignalingConnectionState.Closing);
            lifecycleCancellation?.Cancel();
            var current = connection;
            if (current is not null)
            {
                await current.CloseAsync(WebSocketCloseStatus.NormalClosure, "Publisher closed signaling.", cancellationToken);
            }
            pendingReceive = receiveTask;
        }
        finally
        {
            lifecycleLock.Release();
        }

        if (pendingReceive is not null)
        {
            await IgnoreCancellationAsync(pendingReceive);
        }
        await DisposeConnectionAsync();
        ClearActiveIdentity();
        SetState(SignalingConnectionState.Closed);
    }

    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var tokenResult = await tokenStore.LoadAsync(cancellationToken);
        if (!tokenResult.Succeeded || string.IsNullOrWhiteSpace(tokenResult.Tokens?.AccessToken))
        {
            throw new InvalidOperationException(tokenResult.Message ?? "A current access token is required for signaling.");
        }

        var next = connectionFactory.Create();
        try
        {
            await next.ConnectAsync(BuildConnectionUri(), tokenResult.Tokens.AccessToken, cancellationToken);
            await SendCoreAsync(next, new SignalingMessageEnvelope(SignalingMessageTypes.PublisherReady, activeSessionId), cancellationToken);
        }
        catch
        {
            await next.DisposeAsync();
            throw;
        }

        var previous = connection;
        connection = next;
        if (previous is not null)
        {
            await previous.DisposeAsync();
        }
        SetState(SignalingConnectionState.Connected);
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var current = connection ?? throw new WebSocketException("The signaling socket is unavailable.");
                var inbound = await current.ReceiveAsync(cancellationToken);
                if (inbound.MessageType == WebSocketMessageType.Close)
                {
                    if (inbound.CloseStatus == WebSocketCloseStatus.NormalClosure)
                    {
                        await CloseFromReceiveLoopAsync();
                        return;
                    }
                    throw new WebSocketException("The signaling socket closed unexpectedly.");
                }
                if (inbound.MessageType != WebSocketMessageType.Text || inbound.Text is null)
                {
                    continue;
                }

                SignalingMessageEnvelope message;
                try
                {
                    message = SignalingMessageEnvelope.Deserialize(inbound.Text);
                }
                catch (SignalingProtocolException)
                {
                    continue;
                }

                if (message.Type == SignalingMessageTypes.Ping)
                {
                    await SendAsync(new SignalingMessageEnvelope(SignalingMessageTypes.Pong, activeSessionId, message.ViewerId), cancellationToken);
                }

                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(message, cancellationToken);
                }

                if (message.Type == SignalingMessageTypes.SessionEnded)
                {
                    await CloseFromReceiveLoopAsync();
                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                if (!await TryReconnectAsync(cancellationToken))
                {
                    SetState(SignalingConnectionState.Faulted);
                    return;
                }
            }
        }
    }

    private async Task<bool> TryReconnectAsync(CancellationToken cancellationToken)
    {
        SetState(SignalingConnectionState.Reconnecting);
        foreach (var delay in ReconnectDelays)
        {
            try
            {
                await reconnectDelay.DelayAsync(delay, cancellationToken);
                await OpenConnectionAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception) when (IsTransient(exception))
            {
            }
        }
        return false;
    }

    private async Task CloseFromReceiveLoopAsync()
    {
        SetState(SignalingConnectionState.Closing);
        lifecycleCancellation?.Cancel();
        var current = connection;
        if (current is not null)
        {
            await current.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended.", CancellationToken.None);
        }
        await DisposeConnectionAsync();
        ClearActiveIdentity();
        SetState(SignalingConnectionState.Closed);
    }

    private async Task SendCoreAsync(
        IWebSocketConnection target,
        SignalingMessageEnvelope message,
        CancellationToken cancellationToken)
    {
        await sendLock.WaitAsync(cancellationToken);
        try
        {
            await target.SendTextAsync(message.Serialize(), cancellationToken);
        }
        finally
        {
            sendLock.Release();
        }
    }

    private Uri BuildConnectionUri()
    {
        var builder = new UriBuilder(configuration.SignalingBaseUrl)
        {
            Scheme = configuration.SignalingBaseUrl.Scheme.ToLowerInvariant() switch
            {
                "http" => "ws",
                "https" => "wss",
                "ws" => "ws",
                "wss" => "wss",
                _ => throw new InvalidOperationException("The signaling URL must use HTTP(S) or WS(S).")
            }
        };
        var existingQuery = builder.Query.TrimStart('?');
        var identityQuery = $"sessionId={Uri.EscapeDataString(activeSessionId!)}&deviceId={Uri.EscapeDataString(activeDeviceId!)}";
        builder.Query = string.IsNullOrEmpty(existingQuery) ? identityQuery : $"{existingQuery}&{identityQuery}";
        return builder.Uri;
    }

    private bool IsActive() => State is SignalingConnectionState.Connecting
        or SignalingConnectionState.Connected
        or SignalingConnectionState.Reconnecting
        or SignalingConnectionState.Closing;

    private static bool IsTransient(Exception exception) =>
        exception is WebSocketException or IOException;

    private void SetState(SignalingConnectionState state)
    {
        if (State == state)
        {
            return;
        }
        State = state;
        StateChanged?.Invoke(state);
    }

    private async Task DisposeConnectionAsync()
    {
        var current = Interlocked.Exchange(ref connection, null);
        if (current is not null)
        {
            await current.DisposeAsync();
        }
    }

    private void ClearActiveIdentity()
    {
        activeSessionId = null;
        activeDeviceId = null;
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        lifecycleCancellation?.Dispose();
        lifecycleLock.Dispose();
        sendLock.Dispose();
    }
}
