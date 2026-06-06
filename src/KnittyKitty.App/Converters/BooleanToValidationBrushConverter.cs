using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace KnittyKitty.App.Converters;

public sealed class BooleanToValidationBrushConverter : IValueConverter
{
    public SolidColorBrush ValidBrush { get; set; } = new(Color.FromArgb(255, 227, 232, 240));

    public SolidColorBrush InvalidBrush { get; set; } = new(Color.FromArgb(255, 196, 43, 28));

    /// Преобразует признак ошибки в кисть подсветки поля ввода.
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? InvalidBrush : ValidBrush;
    }

    /// Запрещает обратное преобразование, так как привязка используется только для вывода.
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Equals(value, InvalidBrush);
    }
}
