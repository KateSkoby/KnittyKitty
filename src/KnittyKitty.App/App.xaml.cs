using Microsoft.UI.Xaml;

namespace KnittyKitty.App;

public partial class App : Application
{
    private Window? _window;

    /// Создаёт объект приложения и подключает XAML-компоненты.
    public App()
    {
        InitializeComponent();
    }

    /// Создаёт и активирует главное окно после запуска приложения.
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
