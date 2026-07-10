using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace SonicRelay.Windows.App.Appearance;

/// <summary>
/// A Desktop Acrylic backdrop whose tint opacity is configurable (issue #30). The
/// built-in <see cref="DesktopAcrylicBackdrop"/> does not expose opacity, so this
/// wraps a <see cref="DesktopAcrylicController"/> and drives it from the base
/// <see cref="SystemBackdrop"/>'s theme/activation-aware configuration.
/// </summary>
public sealed partial class TintedAcrylicBackdrop : SystemBackdrop
{
    private readonly float _tintOpacity;
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _configuration;

    public TintedAcrylicBackdrop(double tintOpacity) => _tintOpacity = (float)Math.Clamp(tintOpacity, 0.0, 1.0);

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        _configuration ??= GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
        _controller ??= new DesktopAcrylicController { TintOpacity = _tintOpacity };
        _controller.AddSystemBackdropTarget(connectedTarget);
        _controller.SetSystemBackdropConfiguration(_configuration);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        _controller?.RemoveSystemBackdropTarget(disconnectedTarget);
        _controller?.Dispose();
        _controller = null;
        _configuration = null;
    }
}
