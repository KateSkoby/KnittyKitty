using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KnittyKitty.App.Controls;

public sealed partial class KittyLogo : UserControl
{
    /// Создаёт визуальный контрол логотипа и загружает его XAML-разметку.
    public KittyLogo()
    {
        InitializeComponent();

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.png");
        if (File.Exists(logoPath))
        {
            LogoImage.Source = new BitmapImage(new Uri(logoPath));
        }
    }
}
