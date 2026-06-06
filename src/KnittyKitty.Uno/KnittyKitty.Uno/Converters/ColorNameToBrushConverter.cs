using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace KnittyKitty.Uno.Converters;

public sealed class ColorNameToBrushConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<string, string> ColorHexByName = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
    {
        ["Лайм"] = "#C8EB01",
        ["Розовый"] = "#FD7DD4",
        ["Голубой"] = "#37B6F8",
        ["Молочный"] = "#FFF5E7",
        ["Пудровый"] = "#F6B9C9",
        ["Графит"] = "#42434A",
        ["Сиреневый"] = "#C8A8F4",
        ["Мятный"] = "#9DE8D4",
        ["Зеленый"] = "#9DBF6D",
        ["Зелёный"] = "#9DBF6D",
        ["Лавандовый"] = "#BDAEFF"
    };

    /// Преобразует название цвета товара в кисть для цветового индикатора.
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var colorName = value as string ?? string.Empty;
        var hex = ColorHexByName.TryGetValue(colorName, out var mappedHex)
            ? mappedHex
            : "#D9DEE8";

        return new SolidColorBrush(ParseHex(hex));
    }

    /// Запрещает обратное преобразование, так как привязка используется только для вывода.
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    /// Разбирает HEX-строку цвета и создаёт объект Color.
    private static Color ParseHex(string hex)
    {
        var normalized = hex.TrimStart('#');
        var red = byte.Parse(normalized[..2], System.Globalization.NumberStyles.HexNumber);
        var green = byte.Parse(normalized[2..4], System.Globalization.NumberStyles.HexNumber);
        var blue = byte.Parse(normalized[4..6], System.Globalization.NumberStyles.HexNumber);

        return Color.FromArgb(255, red, green, blue);
    }
}
