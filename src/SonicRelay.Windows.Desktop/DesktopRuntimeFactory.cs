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
        var audioOutputPreference = new AudioOutputPreferenceStore();
        var backend = new PipeWireProcessBackend(processRunner, commandPaths, resolver, () => audioOutputPreference.SelectedDeviceId);
        var audioCapture = AudioCaptureService.Create(backend, probe);

        ITokenStore tokenStore = commandPaths.SecretTool is { } secretToolPath
            ? new SecretServiceTokenStore(processRunner, secretToolPath)
            : new InMemoryTokenStore();

        return PublisherRuntime.Create(backendBaseUrl, audioCapture, tokenStore, audioOutputPreference);
    }
}
