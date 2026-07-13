using SonicRelay.Windows.Audio;
using SonicRelay.Windows.Desktop.ViewModels;
using SonicRelay.Windows.Presentation;
using SonicRelay.Windows.Signaling;

namespace SonicRelay.Windows.Desktop.Tests;

/// <summary>
/// The shell chooses the sign-in surface vs. the dashboard from the snapshot, so a successful
/// sign-in flips it to the dashboard automatically (#32).
/// </summary>
public sealed class MainWindowViewModelStateTests
{
    [Fact]
    public void Show_login_without_an_authenticated_snapshot()
    {
        Assert.True(MainWindowViewModel.ShouldShowLogin(null));
        Assert.True(MainWindowViewModel.ShouldShowLogin(new PublisherSnapshot { IsAuthenticated = false }));
    }

    [Fact]
    public void Hide_login_once_authenticated()
    {
        Assert.False(MainWindowViewModel.ShouldShowLogin(new PublisherSnapshot { IsAuthenticated = true }));
    }

    [Fact]
    public void Fresh_view_model_opens_on_the_login_surface()
    {
        var vm = new MainWindowViewModel();

        Assert.True(vm.ShowLogin);
    }

    [Fact]
    public void Preview_view_model_opens_on_the_dashboard()
    {
        var vm = MainWindowViewModel.CreatePreview();

        Assert.False(vm.ShowLogin);
        Assert.False(vm.Auth.HasError);
    }
}
