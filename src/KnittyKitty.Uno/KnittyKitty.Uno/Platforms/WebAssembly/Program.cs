using Uno.UI.Hosting;

namespace KnittyKitty.Uno;

public class Program
{
    /// Запускает Uno-приложение в браузере через WebAssembly-хост.
    public static async Task Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}