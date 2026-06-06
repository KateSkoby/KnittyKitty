using Uno.UI.Hosting;

namespace KnittyKitty.Uno;

internal class Program
{
    /// Запускает Uno-приложение в desktop-хосте.
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
