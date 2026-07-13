using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SonicRelay.Windows.Desktop.ViewModels;
using SonicRelay.Windows.Desktop.Views;

namespace SonicRelay.Windows.Desktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The shell renders from the shared presentation projection
            // (DashboardViewModel + PublisherUiState). A live PublisherRuntime is
            // attached in a later slice (tray, auth, reconnect — phase 2 PR 3); until
            // then the window opens on the representative snapshot so the layout and
            // design system can be validated against the Lovable prototype.
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainWindowViewModel.CreatePreview(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
