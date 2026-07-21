using SonicRelay.Windows.Audio;
using SonicRelay.Windows.Core.Configuration;
using SonicRelay.Windows.Core.Storage;

namespace SonicRelay.Windows.Presentation.Tests;

public sealed class PublisherRuntimeTests
{
    private static readonly Uri BackendUrl = new("https://backend.example.test/");

    [Fact]
    public async Task CreateWithoutOverridesUsesTheDefaultWindowsTokenStore()
    {
        await using var runtime = PublisherRuntime.Create(BackendUrl, new FakeAudio());

        Assert.IsType<UserScopedTokenStore>(runtime.TokenStore);
    }

    [Fact]
    public async Task CreateWithATokenStoreOverrideUsesItInstead()
    {
        var tokenStore = new InMemoryFakeTokenStore();

        await using var runtime = PublisherRuntime.Create(BackendUrl, new FakeAudio(), tokenStoreOverride: tokenStore);

        Assert.Same(tokenStore, runtime.TokenStore);
    }

    [Fact]
    public async Task CreateWithAnAudioOutputPreferenceOverrideExposesTheSameInstance()
    {
        var preference = new AudioOutputPreferenceStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "audio-output.json"));

        await using var runtime = PublisherRuntime.Create(BackendUrl, new FakeAudio(), audioOutputPreferenceOverride: preference);

        Assert.Same(preference, runtime.AudioOutput);
    }

    private sealed class FakeAudio : IAudioCaptureService
    {
        public AudioCaptureState State => AudioCaptureState.Stopped;
        public AudioCaptureDiagnostics Diagnostics { get; } = new(AudioCaptureState.Stopped, null, null, AudioLevelSnapshot.Silence, 0, 0);
        public string? PreferredDeviceId => null;
        public event Action<AudioCaptureState>? StateChanged;
        public event Action<AudioFrame>? FrameCaptured;
        public event Action<AudioLevelSnapshot>? LevelChanged;
        public IReadOnlyList<AudioOutputDevice> GetOutputDevices() => [];
        public void SelectOutputDevice(string? deviceId) { }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PauseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ResumeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class InMemoryFakeTokenStore : ITokenStore
    {
        public Task<TokenStorageResult> SaveAsync(TokenSet tokens, CancellationToken cancellationToken = default) => Task.FromResult(TokenStorageResult.Success());
        public Task<TokenStorageResult> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(TokenStorageResult.Success());
        public Task<TokenStorageResult> DeleteAsync(CancellationToken cancellationToken = default) => Task.FromResult(TokenStorageResult.Success());
    }
}
