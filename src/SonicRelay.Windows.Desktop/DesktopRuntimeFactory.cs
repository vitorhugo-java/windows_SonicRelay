using System.Runtime.Versioning;
using SonicRelay.Platform.Linux.Audio;
using SonicRelay.Platform.Linux.Storage;
using SonicRelay.Windows.Audio;
using SonicRelay.Windows.Core.Configuration;
using SonicRelay.Windows.Core.Storage;
using SonicRelay.Windows.Presentation;

namespace SonicRelay.Windows.Desktop;

/// <summary>
/// Platform composition root for the publisher runtime (issue #32): Windows
/// composes WASAPI capture with DPAPI token storage (unchanged); Linux composes
/// the PipeWire adapter with Secret Service token storage (falling back to an
/// in-memory store when Secret Service is unavailable). Any other platform is an
/// explicit unsupported state, not a silent preview.
/// </summary>
public static class DesktopRuntimeFactory
{
    public static PublisherRuntime? Create(Uri backendBaseUrl)
    {
        if (OperatingSystem.IsWindows()) return CreateWindows(backendBaseUrl);
        if (OperatingSystem.IsLinux()) return CreateLinux(backendBaseUrl);
        return null;
    }

    [SupportedOSPlatform("windows")]
    private static PublisherRuntime CreateWindows(Uri backendBaseUrl) =>
        PublisherRuntime.Create(backendBaseUrl, new AudioCaptureService());

    private static PublisherRuntime CreateLinux(Uri backendBaseUrl)
    {
        var commandPaths = new PipeWireCommandLocator().Locate();
        var processRunner = new LinuxProcessRunner();
        var resolver = new PipeWireSinkResolver(processRunner, commandPaths);
        var probe = new PipeWireOutputDeviceProbe(processRunner, commandPaths);
        // On Windows, IAudioCaptureService.SelectOutputDevice() is the live routing
        // switch: it updates AudioCaptureService's own internal selection, which
        // WasapiLoopbackBackend reads on its next start. AudioCaptureService.Create's
        // internal selection has no seam this composition root can reach, so the
        // Linux backend instead reads the shared, persisted AudioOutputPreferenceStore
        // directly. AudioPageViewModel's device picker updates both the enumerator and
        // this store together, so the two stay in sync in practice today — but a
        // future caller that only calls SelectOutputDevice() (bypassing the store)
        // would silently have no effect on Linux capture routing. Keep this in mind
        // before adding a new output-device selection path on either platform.
        var audioOutputPreference = new AudioOutputPreferenceStore();
        var backend = new PipeWireProcessBackend(processRunner, commandPaths, resolver, () => audioOutputPreference.SelectedDeviceId);
        var audioCapture = AudioCaptureService.Create(backend, probe);

        ITokenStore tokenStore = commandPaths.SecretTool is { } secretToolPath
            ? new SecretServiceTokenStore(processRunner, secretToolPath)
            : new InMemoryTokenStore();

        return PublisherRuntime.Create(backendBaseUrl, audioCapture, tokenStore, audioOutputPreference);
    }
}
