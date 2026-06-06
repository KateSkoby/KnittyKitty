using System.Reflection;
using KnittyKitty.Core.Repositories;
using Windows.Storage;

namespace KnittyKitty.Uno.Storage;

public static class ApplicationStorage
{
    private const string DatabaseResourceName = "KnittyKitty.Uno.Data.knittykitty.db";
    private const string DatabaseFileName = "knittykitty.db";

    private static readonly object SyncRoot = new();
    private static bool _isInitialized;

    public static string RootDirectory => Path.Combine(GetLocalRootDirectory(), "KnittyKitty");

    public static string DatabasePath => Path.Combine(RootDirectory, "Data", DatabaseFileName);

    public static string ReceiptsDirectory => Path.Combine(RootDirectory, "Receipts");

    /// Готовит локальное хранилище Uno, базу данных и репозитории приложения.
    public static void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_isInitialized)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            Directory.CreateDirectory(ReceiptsDirectory);
            EnsureDatabase();
            SqliteSeedData.Ensure(DatabasePath);
            _isInitialized = true;
        }
    }

    /// Создаёт рабочую копию SQLite-базы из embedded-ресурса при первом запуске.
    private static void EnsureDatabase()
    {
        if (File.Exists(DatabasePath) && new FileInfo(DatabasePath).Length > 0)
        {
            return;
        }

        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DatabaseResourceName);
        if (resourceStream is not null)
        {
            using var fileStream = File.Create(DatabasePath);
            resourceStream.CopyTo(fileStream);
            return;
        }

        var packagedPath = Path.Combine(AppContext.BaseDirectory, "Data", DatabaseFileName);
        if (File.Exists(packagedPath))
        {
            File.Copy(packagedPath, DatabasePath, overwrite: true);
            return;
        }

        throw new FileNotFoundException("Seed SQLite database was not found.", DatabaseFileName);
    }

    /// Определяет платформенный каталог для локальных данных приложения.
    private static string GetLocalRootDirectory()
    {
        try
        {
            var localFolderPath = ApplicationData.Current.LocalFolder.Path;
            if (!string.IsNullOrWhiteSpace(localFolderPath))
            {
                return localFolderPath;
            }
        }
        catch
        {
            // Some targets expose storage only after startup; fall back to a .NET-local path.
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(localAppData)
            ? AppContext.BaseDirectory
            : localAppData;
    }
}
